# backend/app/services/playbook_service.py
import logging
from typing import Dict, Any, Optional, List

logger = logging.getLogger(__name__)

class PlaybookService:
    def __init__(self):
        # Biblioteca de Playbooks Estratégicos
        self.playbooks = {
            "CONTORNAR_OBJECAO_PRECO": {
                "nome": "Playbook: Contornar Objeção de Preço",
                "passos": [
                    {
                        "acao": "Use a abordagem Empática do Módulo de Objeções.",
                        "sugestao": "Valide a preocupação do cliente antes de justificar o preço. Ex: 'Entendo perfeitamente sua preocupação com o orçamento...'"
                    },
                    {
                        "acao": "Reforce o valor com um Script de Prova Social.",
                        "sugestao": "Conecte o preço a resultados reais. Ex: 'Clientes como a Empresa X, que tinham uma preocupação similar, viram um ROI de 300% em 6 meses.'"
                    },
                    {
                        "acao": "Conduza para uma Pergunta de Conexão focada em ROI.",
                        "sugestao": "Mude o foco do custo para o benefício. Ex: 'Se o custo não fosse um problema, qual seria o maior benefício que essa solução traria para sua equipe hoje?'"
                    }
                ]
            },
            "RECUPERAR_RAPPORT": {
                "nome": "Playbook: Recuperação de Rapport",
                "passos": [
                    {
                        "acao": "Use uma abordagem Empática para validar o sentimento.",
                        "sugestao": "Mostre que você ouviu. Ex: 'Percebi que talvez o que eu disse não soou da melhor forma. Peço desculpas. Sua preocupação com [ponto levantado pelo cliente] é muito válida.'"
                    },
                    {
                        "acao": "Faça uma Pergunta de Conexão aberta para realinhar.",
                        "sugestao": "Volte a focar no cliente. Ex: 'Para garantir que estamos na mesma página, qual é o desafio mais crítico que você espera resolver neste momento?'"
                    }
                ]
            }
        }

    def recomendar_playbook(self, analise_copiloto: Dict[str, Any]) -> Optional[Dict[str, Any]]:
        """
        Analisa os resultados do Co-piloto e recomenda o melhor Playbook.
        """
        try:
            predicao_objecoes = analise_copiloto.get("predicao_objecoes", {})
            mudanca_emocional = analise_copiloto.get("mudanca_emocional", {})

            # REGRA 1: Se a predição de objeção de preço for alta, recomenda o playbook de preço.
            if predicao_objecoes and predicao_objecoes.get("objecoes_provaveis"):
                for objecao in predicao_objecoes["objecoes_provaveis"]:
                    if objecao.get("tipo", "").upper() == "PREÇO" and objecao.get("probabilidade", 0) > 0.6:
                        logger.debug("Playbook 'CONTORNAR_OBJECAO_PRECO' recomendado.")
                        return self.playbooks["CONTORNAR_OBJECAO_PRECO"]

            # REGRA 2: Se houver uma mudança emocional negativa, recomenda recuperação de rapport.
            if mudanca_emocional and mudanca_emocional.get("direcao_mudanca") == "POSITIVO_PARA_NEGATIVO":
                logger.debug("Playbook 'RECUPERAR_RAPPORT' recomendado.")
                return self.playbooks["RECUPERAR_RAPPORT"]

        except Exception as e:
            logger.error(f"Erro ao recomendar playbook: {e}")
        
        return None

# Instância global do serviço
playbook_service = PlaybookService()