# backend/app/services/prompt_service.py
"""
Serviço de negócio para gerenciamento de prompts
"""
from app.services.ia_orchestrator import ia_orchestrator
from app.services.key_manager import key_manager
import logging
from typing import Dict, Any, List
from app.prompts.prompt_manager import prompt_manager
from app.prompts.registry import get_all_prompts, get_prompts_by_module, get_prompt_parameters, get_prompt_template
import json
from fastapi import Request

logger = logging.getLogger(__name__)

class PromptService:
    """
    Serviço para operações com prompts
    """
    
    def __init__(self):
        self.manager = prompt_manager
        self.orchestrator = ia_orchestrator

    # ========== FUNÇÕES PARA CONTEXTO PERSONALIZADO ==========

    async def render_with_user_context(self, request: Request, prompt_name: str, **kwargs) -> str:
        """
        Renderiza prompt com contexto personalizado do usuário (histórico + perfil)
        """
        try:
            from app.services.history_service import history_service
            
            # Obtém contexto do usuário se disponível
            user_context = await self._get_user_context(request, prompt_name, kwargs)
            
            # Prepara placeholders para o template
            enhanced_kwargs = kwargs.copy()
            context_placeholders = self._prepare_context_placeholders(user_context)
            enhanced_kwargs.update(context_placeholders)
            
            # Log para debug
            logger.debug(f"Contexto injetado no prompt '{prompt_name}': "
                        f"historico={bool(user_context.get('historico_usuario'))}, "
                        f"perfil={bool(user_context.get('perfil_comportamental'))}, "
                        f"ajuste={bool(user_context.get('instrucao_ajuste'))}")
            
            # Renderiza prompt normalmente
            return self.manager.render(prompt_name, **enhanced_kwargs)
            
        except Exception as e:
            logger.warning(f"❌ Erro ao renderizar com contexto: {e}")
            # Fallback para renderização normal sem contexto
            return self.manager.render(prompt_name, **kwargs)
            
    async def _get_user_context(self, request: Request, prompt_name: str, parameters: Dict[str, Any]) -> Dict[str, Any]:
        """
        Obtém contexto personalizado do usuário (histórico + perfil comportamental)
        """
        context = {}
        
        if not hasattr(request.state, 'current_user_id'):
            return {} # Retorna contexto vazio se não for um usuário autenticado via JWT

        try:
            from app.services.history_service import history_service
            user_id = request.state.current_user_id

            # Determina módulo baseado no prompt_name
            modulo = self._extract_module_from_prompt(prompt_name)
            
            # 1. Memória de Longo Prazo - Resumo do histórico
            if modulo:
                historico_usuario = await history_service.get_user_summary(user_id, modulo)
                if historico_usuario:
                    context["historico_usuario"] = historico_usuario

            # 1.5 Primeiro nome do usuário (se disponível) - usado para saudações personalizadas
            try:
                from app.database.users import get_user_by_id
                user = get_user_by_id(user_id)
                if user and user.get('full_name'):
                    primeiro = user.get('full_name').strip().split()[0]
                    if primeiro:
                        context['primeiro_nome'] = primeiro
                        context['nome_cliente'] = primeiro
            except Exception as e:
                logger.debug(f"Não foi possível obter primeiro nome do usuário {user_id}: {e}")
            
            # 2. Perfil Comportamental
            perfil_comportamental = await history_service.get_user_behavioral_profile(user_id)
            if perfil_comportamental and perfil_comportamental.get("estilo") != "NEUTRO":
                context["perfil_comportamental"] = perfil_comportamental
                
                # Adiciona instruções específicas baseadas no perfil
                estilo = perfil_comportamental.get("estilo", "NEUTRO")
                if estilo == "AGRESSIVO":
                    context["instrucao_ajuste"] = "O vendedor tende a ser AGRESSIVO. Corrija para ser mais consultivo e empático."
                elif estilo == "TÉCNICO":
                    context["instrucao_ajuste"] = "O vendedor é muito TÉCNICO. Simplifique a linguagem e foque em benefícios emocionais."
                elif estilo == "EMPÁTICO":
                    context["instrucao_ajuste"] = "O vendedor tem boa EMPATIA. Mantenha esta qualidade enquanto fortalece o fechamento."
            
            return context
            
        except Exception as e:
            logger.warning(f"❌ Erro ao obter contexto do usuário: {e}")
            return {}
    
    def _extract_module_from_prompt(self, prompt_name: str) -> str:
        """Extrai módulo do nome do prompt"""
        module_mapping = {
            "simulador": ["simulador"],
            "objecoes": ["objecoes", "predicao_objecoes"],
            "detector": ["detector"],
            "conexao": ["conexao"],
            "scripts": ["scripts"],
            "contexto": ["contexto"],
            "emocional": ["mudanca_emocional"]
        }
        
        for module, prefixes in module_mapping.items():
            if any(prompt_name.startswith(prefix) for prefix in prefixes):
                return module
        
        return ""

    def obter_prompt_simulador_cliente_original(self, produto_descricao: str, perfil_cliente: str, 
                                            mensagem_usuario: str, historico: str = "") -> str:
        """
        Método original mantido para compatibilidade total
        """
        return self.manager.render(
            "simulador_cliente",
            produto_descricao=produto_descricao,
            perfil_cliente=perfil_cliente,
            mensagem_usuario=mensagem_usuario,
            historico=historico,
            historico_usuario_placeholder="",
            perfil_comportamental_placeholder="", 
            instrucao_ajuste_placeholder=""
        )

    def _prepare_context_placeholders(self, user_context: Dict[str, Any]) -> Dict[str, str]:
        """Prepara placeholders formatados para o contexto do usuário"""
        placeholders = {}
        
        # Histórico do usuário
        if user_context.get("historico_usuario"):
            placeholders["historico_usuario_placeholder"] = f"CONTEXTO DO VENDEDOR: {user_context['historico_usuario']}"
        else:
            placeholders["historico_usuario_placeholder"] = ""
        
        # Perfil comportamental  
        if user_context.get("perfil_comportamental"):
            perfil = user_context["perfil_comportamental"]
            estilo = perfil.get('estilo', 'NEUTRO')
            lacunas = ', '.join(perfil.get('lacunas', []))
            placeholders["perfil_comportamental_placeholder"] = f"PERFIL COMPORTAMENTAL: {estilo}\nLACUNAS: {lacunas}"
        else:
            placeholders["perfil_comportamental_placeholder"] = ""
        
        # Instrução de ajuste
        if user_context.get("instrucao_ajuste"):
            placeholders["instrucao_ajuste_placeholder"] = f"INSTRUÇÃO ESPECÍFICA: {user_context['instrucao_ajuste']}"
        else:
            placeholders["instrucao_ajuste_placeholder"] = ""

        # Primeiro nome do cliente
        if user_context.get('primeiro_nome'):
            placeholders['nome_cliente'] = user_context.get('primeiro_nome')
            placeholders['nome_cliente_placeholder'] = f"NOME DO CLIENTE: {user_context.get('primeiro_nome')}"
        else:
            placeholders['nome_cliente'] = ''
            placeholders['nome_cliente_placeholder'] = ''
        
        return placeholders

    # ========== MÉTODOS COM SUPORTE A CONTEXTO ==========
    
    async def obter_prompt_simulador_cliente(self, request: Request, produto_descricao: str, perfil_cliente: str, 
                                           mensagem_usuario: str, historico: str = "") -> str:
        """
        Obtém prompt para simulação de resposta do cliente COM CONTEXTO
        """
        return await self.render_with_user_context(
            request,
            "simulador_cliente",
            produto_descricao=produto_descricao,
            perfil_cliente=perfil_cliente,
            mensagem_usuario=mensagem_usuario,
            historico=historico
        )
    
    async def obter_prompt_detector_analisar(self, request: Request, produto_descricao: str, mensagem: str) -> str:
        """
        Obtém prompt para análise do detector de vendedor chato COM CONTEXTO
        """
        return await self.render_with_user_context(
            request,
            "detector_analisar",
            produto_descricao=produto_descricao,
            mensagem=mensagem
        )
    
    async def obter_prompt_quebrar_objecao(self, request: Request, produto_descricao: str, objecao: str) -> str:
        """
        Obtém prompt para quebra de objeções COM CONTEXTO
        """
        return await self.render_with_user_context(
            request,
            "objecoes_quebrar",
            produto_descricao=produto_descricao,
            objecao=objecao
        )

    async def obter_prompt_whatsapp_conversar(self, request: Request, produto_descricao: str, mensagem_usuario: str, historico: str = "") -> str:
        """
        Obtém prompt para o agente de conversação WhatsApp COM CONTEXTO
        """
        return await self.render_with_user_context(
            request,
            "whatsapp_conversar",
            produto_descricao=produto_descricao,
            mensagem_usuario=mensagem_usuario,
            historico=historico
        )

    async def obter_prompt_responder_pergunta(self, request: Request, produto_descricao: str, pergunta: str, historico: str = "") -> str:
        """
        Obtém prompt para gerar respostas a perguntas de clientes COM CONTEXTO
        """
        return await self.render_with_user_context(
            request,
            "whatsapp_responder_pergunta",
            produto_descricao=produto_descricao,
            pergunta=pergunta,
            historico=historico
        )

    async def obter_prompt_consultor_vendas(self, request: Request, topico: str, contexto: str = "", historico: str = "") -> str:
        """
        Obtém prompt para o consultor de vendas COM CONTEXTO
        """
        return await self.render_with_user_context(
            request,
            "whatsapp_consultor_vendas",
            topico=topico,
            contexto=contexto,
            historico=historico
        )
    
    async def obter_prompt_gerar_perguntas(self, request: Request, produto_descricao: str, contexto_cliente: str) -> str:
        """
        Obtém prompt para geração de perguntas de conexão COM CONTEXTO
        """
        return await self.render_with_user_context(
            request,
            "conexao_gerar_perguntas",
            produto_descricao=produto_descricao,
            contexto_cliente=contexto_cliente
        )
    
    async def obter_prompt_otimizar_script(self, request: Request, produto_descricao: str, script_original: str,
                                         canal: str, objetivo: str) -> str:
        """
        Obtém prompt para otimização de script COM CONTEXTO
        """
        return await self.render_with_user_context(
            request,
            "scripts_otimizar",
            produto_descricao=produto_descricao,
            script_original=script_original,
            canal=canal,
            objetivo=objetivo
        )

    def obter_prompt_otimizar_script_legacy(self, produto_descricao: str, script_original: str,
                                          canal: str, objetivo: str) -> str:
        """
        Método original mantido para compatibilidade (API Keys).
        """
        return self.manager.render(
            "scripts_otimizar",
            produto_descricao=produto_descricao,
            script_original=script_original,
            canal=canal,
            objetivo=objetivo
        )

    async def obter_prompt_simulador_feedback(self, request: Request, produto_descricao: str, mensagem_usuario: str) -> str:
        """
        Obtém prompt para feedback da mensagem do vendedor COM CONTEXTO
        """
        return await self.render_with_user_context(
            request,
            "simulador_feedback",
            produto_descricao=produto_descricao,
            mensagem_usuario=mensagem_usuario
        )

    # ========== MÉTODOS EXISTENTES (MANTIDOS PARA COMPATIBILIDADE) ==========
    
    def obter_prompt_feedback(self, produto_descricao: str, mensagem_usuario: str) -> str:
        """Método original mantido para compatibilidade"""
        return self.manager.render(
            "simulador_feedback",
            produto_descricao=produto_descricao,
            mensagem_usuario=mensagem_usuario
        )
    
    def obter_prompt_feedback_detalhado(self, produto_descricao: str, mensagem_usuario: str) -> str:
        """Método original mantido para compatibilidade"""
        return self.manager.render(
            "simulador_feedback_detalhado",
            produto_descricao=produto_descricao,
            mensagem_usuario=mensagem_usuario
        )
    
    def obter_prompt_analisar_resposta_old(self, produto_descricao: str, pergunta: str, 
                                     resposta_vendedor: str) -> str:
        """Método original mantido para compatibilidade"""
        return self.manager.render(
            "conexao_analisar_resposta",
            produto_descricao=produto_descricao,
            pergunta=pergunta,
            resposta_vendedor=resposta_vendedor
        )

    async def obter_prompt_analisar_resposta(self, request: Request, produto_descricao: str, pergunta: str, 
                                           resposta_vendedor: str) -> str:
        """
        Obtém prompt para análise de resposta COM CONTEXTO
        """
        return await self.render_with_user_context(
            request,
            "conexao_analisar_resposta",
            produto_descricao=produto_descricao,
            pergunta=pergunta,
            resposta_vendedor=resposta_vendedor
        )    

    async def obter_prompt_simular_dialogo(self, request: Request, produto_descricao: str, cenario: str, 
                                         abordagem: str) -> str:
        """
        Obtém prompt para simulação de diálogo COM CONTEXTO
        """
        return await self.render_with_user_context(
            request,
            "conexao_simular_dialogo",
            produto_descricao=produto_descricao,
            cenario=cenario,
            abordagem=abordagem
        )

    async def obter_prompt_roteiro_abordagem(self, request: Request, produto_descricao: str, tipo_pessoa: str, nome_cliente: str = None) -> str:
        """
        Obtém prompt para gerar um roteiro de abordagem COM CONTEXTO
        """
        params = {
            "produto_descricao": produto_descricao,
            "tipo_pessoa": tipo_pessoa,
            "nome_cliente": nome_cliente or ""
        }
        return await self.render_with_user_context(
            request,
            "conexao_roteiro_abordagem",
            **params
        )
    
    def obter_prompt_gerar_variacoes_(self, produto_descricao: str, script_base: str,
                                canal: str, numero_variacoes: int) -> str:
        """Método original mantido para compatibilidade"""
        return self.manager.render(
            "scripts_gerar_variacoes",
            produto_descricao=produto_descricao,
            script_base=script_base,
            canal=canal,
            numero_variacoes=numero_variacoes
        )

    async def obter_prompt_gerar_variacoes(self, request: Request, produto_descricao: str, script_base: str,
                                         canal: str, numero_variacoes: int) -> str:
        """
        Obtém prompt para geração de variações de script COM CONTEXTO
        """
        return await self.render_with_user_context(
            request,
            "scripts_gerar_variacoes",
            produto_descricao=produto_descricao,
            script_base=script_base,
            canal=canal,
            numero_variacoes=numero_variacoes
        )        

    def obter_prompt_gerar_variacoes_legacy(self, produto_descricao: str, script_base: str,
                                          canal: str, numero_variacoes: int) -> str:
        """Método original mantido para compatibilidade (API Keys)"""
        return self.manager.render(
            "scripts_gerar_variacoes",
            produto_descricao=produto_descricao,
            script_base=script_base,
            canal=canal,
            numero_variacoes=numero_variacoes
        )

    def obter_prompt_analisar_resposta_legacy(self, produto_descricao: str, pergunta: str, 
                                            resposta_vendedor: str) -> str:
        """Método original mantido para compatibilidade (API Keys)"""
        return self.manager.render(
            "conexao_analisar_resposta",
            produto_descricao=produto_descricao,
            pergunta=pergunta,
            resposta_vendedor=resposta_vendedor
        )

    def obter_prompt_simular_dialogo_legacy(self, produto_descricao: str, cenario: str, 
                                          abordagem: str) -> str:
        """Método original mantido para compatibilidade (API Keys)"""
        return self.manager.render(
            "conexao_simular_dialogo",
            produto_descricao=produto_descricao,
            cenario=cenario,
            abordagem=abordagem
        )

    def obter_prompt_analisar_eficacia_legacy(self, produto_descricao: str, script: str,
                                            canal: str) -> str:
        """Método original mantido para compatibilidade (API Keys)"""
        return self.manager.render(
            "scripts_analisar_eficacia",
            produto_descricao=produto_descricao,
            script=script,
            canal=canal
        )

    def obter_prompt_analisar_eficacia_legacy(self, produto_descricao: str, script: str,
                                            canal: str) -> str:
        """Método original mantido para compatibilidade (API Keys)"""
        return self.manager.render(
            "scripts_analisar_eficacia",
            produto_descricao=produto_descricao,
            script=script,
            canal=canal,
            # CORREÇÃO: Adiciona placeholders vazios para compatibilidade
            historico_usuario_placeholder="",
            perfil_comportamental_placeholder="",
            instrucao_ajuste_placeholder=""
        )

    def obter_prompt_analise_conversao(self, produto_descricao: str, conversa_completa: List[Dict[str, str]], perfil_cliente: str) -> str:
        """Método original mantido para compatibilidade"""
        # Formatar conversa para texto
        conversa_texto = ""
        for i, msg in enumerate(conversa_completa):
            speaker = "VENDEDOR" if msg.get("tipo") == "usuario" else "CLIENTE"
            conversa_texto += f"{i+1}. {speaker}: {msg.get('texto', '')}\n"
        
        return self.manager.render(
            "analise_conversao",
            produto_descricao=produto_descricao,
            conversa_completa=conversa_texto,
            perfil_cliente=perfil_cliente
        )
    
    def obter_prompt_metricas_tempo_real(self, produto_descricao: str, mensagem_vendedor: str, resposta_cliente: str, perfil_cliente: str) -> str:
        """Método original mantido para compatibilidade"""
        return self.manager.render(
            "metricas_tempo_real",
            produto_descricao=produto_descricao,
            mensagem_vendedor=mensagem_vendedor,
            resposta_cliente=resposta_cliente,
            perfil_cliente=perfil_cliente
        )

    async def obter_prompt_analisar_eficacia(self, request: Request, produto_descricao: str, script: str,
                                           canal: str) -> str:
        """
        Obtém prompt para análise de eficácia de script COM CONTEXTO
        """
        return await self.render_with_user_context(
            request,
            "scripts_analisar_eficacia",
            produto_descricao=produto_descricao,
            script=script,
            canal=canal
        )

    # ========== MÉTODOS PARA ENDPOINTS ==========

    def obter_prompt(self, nome: str) -> str:
        """Obtém o template de um prompt pelo nome - PARA ENDPOINTS"""
        template = get_prompt_template(nome)
        if not template:
            raise ValueError(f"Prompt '{nome}' não encontrado")
        return template
        
    def renderizar_prompt(self, nome: str, **kwargs) -> str:
        """Renderiza um prompt com os parâmetros fornecidos - PARA ENDPOINTS"""
        template = self.obter_prompt(nome)
        
        try:
            # Renderização simples com format
            return template.format(**kwargs)
        except KeyError as e:
            missing_param = str(e).strip("'")
            raise ValueError(f"Parâmetro faltante: {missing_param}")
        except Exception as e:
            raise ValueError(f"Erro ao renderizar prompt: {str(e)}")

    def listar_todos_prompts(self, modulo: str = None) -> Dict[str, Any]:
        """Lista todos os prompts com metadados - PARA ENDPOINTS"""
        if modulo:
            prompts = get_prompts_by_module(modulo)
        else:
            prompts = get_all_prompts()
        
        return {
            "prompts": prompts,
            "total": len(prompts),
            "modulo": modulo if modulo else "todos"
        }

    def obter_metadados_prompt(self, prompt_name: str) -> Dict[str, Any]:
        """Obtém metadados completos de um prompt - PARA ENDPOINTS"""
        all_prompts = get_all_prompts()
        prompt_info = all_prompts.get(prompt_name)
        
        if not prompt_info:
            raise ValueError(f"Prompt '{prompt_name}' não encontrado")
            
        return {
            "nome": prompt_name,
            "modulo": prompt_info.get("modulo", "geral"),
            "descricao": prompt_info.get("descricao", ""),
            "versao": prompt_info.get("versao", "1.0"),
            "tags": prompt_info.get("tags", []),
            "parametros": prompt_info.get("parametros", []),
            "template_preview": get_prompt_template(prompt_name)[:200] + "..." if get_prompt_template(prompt_name) else ""
        }

    # ========== MÉTODOS PARA ENDPOINTS CONTEXTO ==========

    async def obter_prompt_contexto_alinhamento(self, request: Request, produto_descricao: str, conversa: List[Dict[str, str]], 
                                              historico_contexto: List[str]) -> str:
        """
        Obtém prompt para análise de contexto e alinhamento COM CONTEXTO
        """
        # Formatar conversa
        conversa_texto = ""
        for i, msg in enumerate(conversa):
            role = "VENDEDOR" if msg.get("role") == "vendedor" else "CLIENTE"
            conversa_texto += f"{i+1}. {role}: {msg.get('message', '')}\n"

        historico_texto = "\n".join([f"- {ctx}" for ctx in historico_contexto]) if historico_contexto else "Nenhum contexto histórico registrado."

        return await self.render_with_user_context(
            request,
            "contexto_alinhamento",
            produto_descricao=produto_descricao,
            conversa=conversa_texto,
            historico_contexto=historico_texto
        )

    def obter_prompt_contexto_alinhamento_legacy(self, produto_descricao: str, conversa: List[Dict[str, str]], 
                                               historico_contexto: List[str]) -> str:
        """Método original mantido para compatibilidade (API Keys)"""
        # Formatar conversa
        conversa_texto = ""
        for i, msg in enumerate(conversa):
            role = "VENDEDOR" if msg.get("role") == "vendedor" else "CLIENTE"
            conversa_texto += f"{i+1}. {role}: {msg.get('message', '')}\n"

        # Formatar histórico de contexto
        historico_texto = "\n".join([f"- {ctx}" for ctx in historico_contexto]) if historico_contexto else "Nenhum contexto histórico registrado."

        return self.manager.render(
            "contexto_alinhamento",
            produto_descricao=produto_descricao,
            conversa=conversa_texto,
            historico_contexto=historico_texto
        )

    # ========== MÉTODOS PARA ENDPOINTS PREDICACOES ==========

    async def obter_prompt_predicao_objecoes(self, request: Request, produto_descricao: str, conversa: List[Dict[str, str]], 
                                           nicho: str, perfil_cliente: str) -> str:
        """
        Obtém prompt para predição de objeções COM CONTEXTO
        """
        # Formatar conversa
        conversa_texto = ""
        for i, msg in enumerate(conversa):
            role = "VENDEDOR" if msg.get("role") == "vendedor" else "CLIENTE"
            conversa_texto += f"{i+1}. {role}: {msg.get('message', '')}\n"

        return await self.render_with_user_context(
            request,
            "predicao_objecoes",
            produto_descricao=produto_descricao,
            conversa=conversa_texto,
            nicho=nicho,
            perfil_cliente=perfil_cliente
        )

    def obter_prompt_predicao_objecoes_legacy(self, produto_descricao: str, conversa: List[Dict[str, str]], 
                                            nicho: str, perfil_cliente: str) -> str:
        """Método original mantido para compatibilidade (API Keys)"""
        # Formatar conversa
        conversa_texto = ""
        for i, msg in enumerate(conversa):
            role = "VENDEDOR" if msg.get("role") == "vendedor" else "CLIENTE"
            conversa_texto += f"{i+1}. {role}: {msg.get('message', '')}\n"

        return self.manager.render(
            "predicao_objecoes",
            produto_descricao=produto_descricao,
            conversa=conversa_texto,
            nicho=nicho,
            perfil_cliente=perfil_cliente
        )

    # ========== MÉTODOS PARA ENDPOINTS EMOCIONAL ==========

    async def obter_prompt_mudanca_emocional(self, request: Request, produto_descricao: str, conversa: List[Dict[str, str]], 
                                           metricas_base: Dict[str, Any]) -> str:
        """
        Obtém prompt para análise de mudança emocional COM CONTEXTO
        """
        # Formatar conversa
        conversa_texto = ""
        for i, msg in enumerate(conversa):
            if "role" in msg:
                role = "VENDEDOR" if msg.get("role") == "vendedor" else "CLIENTE"
                message = msg.get("message", "")
            elif "tipo" in msg:
                role = "VENDEDOR" if msg.get("tipo") == "vendedor" else "CLIENTE"
                message = msg.get("texto", msg.get("mensagem", ""))
            else:
                role = "CLIENTE"
                message = str(msg)
            conversa_texto += f"{i+1}. {role}: {message}\n"

        # Formatar métricas base
        metricas_texto = json.dumps(metricas_base, indent=2, ensure_ascii=False) if metricas_base else "Nenhuma métrica base fornecida."

        return await self.render_with_user_context(
            request,
            "mudanca_emocional",
            produto_descricao=produto_descricao,
            conversa=conversa_texto,
            metricas_base=metricas_texto
        )

    def obter_prompt_mudanca_emocional_legacy(self, produto_descricao: str, conversa: List[Dict[str, str]], 
                                            metricas_base: Dict[str, Any]) -> str:
        """Método original mantido para compatibilidade (API Keys)"""
        # Formatar conversa - CORREÇÃO: usar 'role' ou 'tipo' conforme padrão
        conversa_texto = ""
        for i, msg in enumerate(conversa):
            # Suporte a múltiplos formatos de mensagem
            if "role" in msg:
                role = "VENDEDOR" if msg.get("role") == "vendedor" else "CLIENTE"
                message = msg.get("message", "")
            elif "tipo" in msg:
                role = "VENDEDOR" if msg.get("tipo") == "vendedor" else "CLIENTE"
                message = msg.get("texto", msg.get("mensagem", ""))
            else:
                # Fallback: assume que é cliente se não especificado
                role = "CLIENTE"
                message = str(msg)
            
            conversa_texto += f"{i+1}. {role}: {message}\n"

        # Formatar métricas base
        metricas_texto = json.dumps(metricas_base, indent=2, ensure_ascii=False) if metricas_base else "Nenhuma métrica base fornecida."

        return self.manager.render(
            "mudanca_emocional",
            produto_descricao=produto_descricao,
            conversa=conversa_texto,
            metricas_base=metricas_texto
        )

    def obter_prompt_mudanca_emocional_original(self, produto_descricao: str, conversa: List[Dict[str, str]], 
                                              metricas_base: Dict[str, Any]) -> str:
        """Método original mantido para compatibilidade"""
        # Formatar conversa - CORREÇÃO: usar 'role' ou 'tipo' conforme padrão
        conversa_texto = ""
        for i, msg in enumerate(conversa):
            # Suporte a múltiplos formatos de mensagem
            if "role" in msg:
                role = "VENDEDOR" if msg.get("role") == "vendedor" else "CLIENTE"
                message = msg.get("message", "")
            elif "tipo" in msg:
                role = "VENDEDOR" if msg.get("tipo") == "vendedor" else "CLIENTE"
                message = msg.get("texto", msg.get("mensagem", ""))
            else:
                # Fallback: assume que é cliente se não especificado
                role = "CLIENTE"
                message = str(msg)
            
            conversa_texto += f"{i+1}. {role}: {message}\n"

        # Formatar métricas base
        metricas_texto = json.dumps(metricas_base, indent=2, ensure_ascii=False) if metricas_base else "Nenhuma métrica base fornecida."

        return self.manager.render(
            "mudanca_emocional",
            produto_descricao=produto_descricao,
            conversa=conversa_texto,
            metricas_base=metricas_texto
        )

    async def _call_llm_provider(self, prompt: str, provider_name: str, **kwargs) -> str:
        """Método original mantido para compatibilidade"""
        # Implementação existente mantida
        max_retries = 3
        current_try = 0
        
        while current_try < max_retries:
            try:
                # Verifica se deve usar mock
                if self.orchestrator.should_use_mock():
                    logger.info("Usando modo MOCK (fallback automático)")
                    return await self._get_mock_response(prompt, provider_name, **kwargs)
                
                # Obtém próxima chave para o provedor
                key_result = self.orchestrator.rotate_key(provider_name)
                if not key_result:
                    logger.warning(f"Nenhuma chave ativa para {provider_name}, tentando próximo provedor")
                    break
                
                provedor_id, chave_hash = key_result
                
                # Chama o provedor específico (mantém sua lógica existente)
                if provider_name == 'gemini':
                    response = await self._call_gemini(prompt, chave_hash, **kwargs)
                elif provider_name == 'openai':
                    response = await self._call_openai(prompt, chave_hash, **kwargs)
                elif provider_name == 'azure':
                    response = await self._call_azure(prompt, chave_hash, **kwargs)
                elif provider_name == 'mistral':
                    response = await self._call_mistral(prompt, chave_hash, **kwargs)
                elif provider_name == 'huggingface':
                    response = await self._call_huggingface(prompt, chave_hash, **kwargs)
                else:
                    raise ValueError(f"Provedor não suportado: {provider_name}")
                
                # Marca sucesso
                self.orchestrator.mark_key_success(provedor_id)
                return response
                
            except Exception as e:
                current_try += 1
                logger.error(f"Tentativa {current_try} falhou para {provider_name}: {e}")
                
                # Marca falha se tivermos o ID da chave
                if 'provedor_id' in locals() and 'chave_hash' in locals():
                    self.orchestrator.mark_key_failure(provedor_id, str(e))
                
                if current_try >= max_retries:
                    logger.error(f"Todas tentativas falharam para {provider_name}")
                    break
        
        # Se chegou aqui, todas tentativas falharam - usa mock
        logger.warning(f"Fallback para MOCK após {max_retries} tentativas")
        return await self._get_mock_response(prompt, provider_name, **kwargs)

    async def _get_mock_response(self, prompt: str, provider_name: str, **kwargs) -> str:
        """Mock response para compatibilidade"""
        return "Resposta mock para manter compatibilidade"

# Instância global do serviço
prompt_service = PromptService()
