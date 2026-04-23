import logging
import time
import re
from typing import List, Optional

from app.database.core import get_db_connection
from app.models import ClassificacaoTipoObjecoes, CategoriaObjecoes
from app.services.utils_service import (
    parsear_respostas_ia,
    remover_tags_nao_esperadas,
    validar_respostas_quebra,
    quebrar_objecao_mock,
)
from app.dependencies import chamar_ia_otimizado
from app.prompts.templates import objecoes_v2

logger = logging.getLogger(__name__)


class ObjecoesServiceV2:
    """Serviço inspirado no ObjecoesService.java, com fluxo mais explícito.

    1. Classifica tipo + clareza da objeção
    2. Classifica categoria do produto
    3. Busca exemplos aprovados na tabela `biblioteca_exemplos`
    4. Monta prompt principal ou fallback e chama IA
    5. Valida estrutura da resposta e cai em mock quando necessário
    """

    async def classificar_objecao(self, objecao: str) -> ClassificacaoTipoObjecoes:
        prompt = objecoes_v2.PROMPT_CLASSIFICA_TIPO_OBJECAO.format(
            objecao_cliente=objecao or ""
        )
        resposta = await chamar_ia_otimizado(prompt)

        # first try to load JSON directly from the response
        try:
            import json, re
            m = re.search(r"\{.*\}", resposta, re.DOTALL)
            if m:
                data = json.loads(m.group())
                return ClassificacaoTipoObjecoes(**data)
        except Exception as exc:
            logger.debug(f"JSON parse attempt failed: {exc} -- text={resposta}")

        # fallback to generic parser (in case model injected headings)
        parsed = parsear_respostas_ia(resposta)
        try:
            return ClassificacaoTipoObjecoes(**parsed)
        except Exception as exc:
            logger.error(f"Falha ao validar classificação de objeção: {exc} -- parsed={parsed}")
            # final fallback to a safe default
            return ClassificacaoTipoObjecoes(tipo_objecao="ADIAMENTO", clareza="VAGA")

    async def categoria_objecao(self, produto_descricao: str) -> CategoriaObjecoes:
        prompt = objecoes_v2.PROMPT_CATEGORIA_OBJECAO.format(
            produto_descricao=produto_descricao or ""
        )
        resposta = await chamar_ia_otimizado(prompt)

        # try extracting JSON directly
        try:
            import json, re
            m = re.search(r"\{.*\}", resposta, re.DOTALL)
            if m:
                data = json.loads(m.group())
                return CategoriaObjecoes(**data)
        except Exception as exc:
            logger.debug(f"JSON parse attempt failed for categoria: {exc} -- text={resposta}")

        parsed = parsear_respostas_ia(resposta)
        try:
            return CategoriaObjecoes(**parsed)
        except Exception as exc:
            logger.error(f"Falha ao validar categoria de produto: {exc} -- parsed={parsed}")
            # fallback to a neutral valid category
            return CategoriaObjecoes(categoria="SERVICO_LOCAL")

    def buscar_exemplos(self,
                        categoria: str,
                        tipo_objecao: str,
                        limite: int = 3) -> List[str]:
        """Retorna lista de respostas de exemplo armazenadas na biblioteca."""
        respostas: List[str] = []
        try:
            with get_db_connection() as conn:
                with conn.cursor() as cur:
                    cur.execute(
                        """
                        SELECT resposta_objecao_exemplo
                        FROM biblioteca_exemplos
                        WHERE tipo = 'objecoes'
                          AND categoria = %s
                          AND tipo_objecao = %s
                        ORDER BY data_criacao DESC
                        LIMIT %s
                        """,
                        (categoria, tipo_objecao, limite),
                    )
                    rows = cur.fetchall()
                    respostas = [r[0] for r in rows] if rows else []
        except Exception as e:
            logger.warning(f"Erro ao buscar exemplos na biblioteca v2: {e}")
        return respostas

    async def gerar(
        self,
        categoria: str,
        tipo_objecao: str,
        produto_descricao: str,
        objecao_cliente: str,
        exemplos_aprovados: Optional[List[str]],
    ) -> dict:
        # if no approved examples, inject the fixed sample from template (Java-like behavior)
        if not exemplos_aprovados:
            logger.debug("Nenhum exemplo aprovado encontrado na biblioteca; inserindo SAMPLE_EXEMPLO padrão")
            exemplos_aprovados = [objecoes_v2.SAMPLE_EXEMPLO]
        else:
            logger.debug(f"Exemplos aprovados encontrados na biblioteca: {exemplos_aprovados}")
        exemplos_str = "\n\n---\n\n".join(exemplos_aprovados)
        prompt = objecoes_v2.PROMPT_PRINCIPAL.format(
            categoria=categoria,
            tipo_objecao=tipo_objecao,
            produto_descricao=produto_descricao,
            objecao_cliente=objecao_cliente,
            exemplos_aprovados=exemplos_str,
        )

        # debug: log prompt preview but avoid huge output; also dump full prompt at debug level
        try:
            preview = prompt if len(prompt) < 1000 else prompt[:1000] + '...'
            logger.debug(f"Gerar prompt v2 (len={len(prompt)}): {preview}")
            logger.debug(f"Gerar prompt v2 completo:\n{prompt}")
        except Exception:
            pass

        async def _call_and_validate(p: str):
            txt = await chamar_ia_otimizado(p, use_cache=False)
            logger.debug(f"Resposta crua IA v2: {repr(txt[:500])} (len={len(txt) if txt else 0})")
            if not txt:
                raise RuntimeError("IA returned empty response")
            parsed = parsear_respostas_ia(txt)
            # import here to avoid free variable issues
            from app.services.utils_service import remover_tags_nao_esperadas
            parsed = remover_tags_nao_esperadas(parsed)
            ok, issues = validar_respostas_quebra(parsed)
            return txt, parsed, ok, issues

        # first attempt
        try:
            raw, respostas_parseadas, valid, issues = await _call_and_validate(prompt)
            logger.debug(f"parsed inicialmente v2: {respostas_parseadas}")
        except Exception as exc:
            logger.error(f"Erro ao chamar IA v2: {exc}")
            raise

        if not valid:
            logger.warning(f"Validação falhou na v2: {issues}, tentando reparar/retry")
            # try heuristic repair
            from app.services.utils_service import reparar_respostas_quebra
            reparado_dict, repaired_keys = reparar_respostas_quebra(respostas_parseadas)
            # debug output of repair result
            logger.debug(f"Repair output: {reparado_dict}")
            logger.debug(f"Repaired keys: {repaired_keys}")
            # also show sentence counts after repair
            for k,v in reparado_dict.items():
                if v:
                    cnt = len([s for s in re.split(r'(?<=[\.?\!])\s+', v) if s.strip()])
                else:
                    cnt = 0
                logger.debug(f"Post-repair sentence count for {k}: {cnt}")
            ok_repair, issues_repair = validar_respostas_quebra(reparado_dict)
            if ok_repair:
                from app.services.utils_service import remover_tags_nao_esperadas
                respostas_parseadas = remover_tags_nao_esperadas(reparado_dict)
                logger.info(f"Reparo heurístico resolveu o formato. Chaves reparadas: {repaired_keys}")
                return respostas_parseadas

            # build retry prompt with explicit format instructions
            retry_prompt = prompt + "\n\nIMPORTANTE: Responda EXATAMENTE no formato abaixo. Use 5 blocos e termine cada bloco com '?':\n" + (
                "[EMPATIA]: ...\n[VALOR]: ...\n[PROVA_SOCIAL]: ...\n[URGENCIA]: ...\n[AUTORIDADE]: ..."
            ) + "\nCada bloco deve ter 4 a 5 frases e terminar com uma pergunta. Não use numeração nem bullets. Se não conseguir, responda exatamente: 'FORMAT_ERROR'"

            raw2, parsed2, ok2, issues2 = await _call_and_validate(retry_prompt)
            if ok2:
                from app.services.utils_service import remover_tags_nao_esperadas
                respostas_parseadas = remover_tags_nao_esperadas(parsed2)
                logger.info("Retry da IA obteve resposta válida")
                return respostas_parseadas
            else:
                logger.error(f"Retry também falhou: {issues2}")
                # for transparency, include raw text in error
                raise RuntimeError(f"Validação falhou após retry: {issues2} / raw: {raw2}")

        return respostas_parseadas


# instância global para importação
objecoes_service_v2 = ObjecoesServiceV2()
