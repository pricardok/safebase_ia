# backend/app/services/history_service.py
"""
Serviço seguro para operações de histórico com validação de ownership
"""

import logging
from typing import Dict, Any, List, Optional
from fastapi import Request, HTTPException, status
from app.database import (
    save_simulation, 
    get_user_simulations,  
    user_has_simulations,
    get_user_simulations_secure,
    validate_simulation_ownership
)
from app.services.database_context import db_context
from datetime import datetime
import json
from functools import lru_cache
import time

# Adicionar o logger
logger = logging.getLogger(__name__)

class HistoryService:
    def __init__(self):
        pass
    
    async def save_simulation_secure(
        self, 
        modulo: str,
        usuario_id: int,
        produto_descricao: str,
        perfil_cliente: Optional[str] = None,
        conversa: Optional[Dict[str, Any]] = None,
        metricas: Optional[Dict[str, Any]] = None,
        feedback_ia: Optional[str] = None,
        is_exemplo: bool = False,
        session_id: Optional[str] = None
    ) -> Optional[int]:
        """Salva simulação apenas se for autenticação JWT com validação reforçada"""
        try:
            # A validação se o histórico deve ser salvo (baseado em JWT vs API Key)
            # agora é responsabilidade de quem chama o serviço, que não passará o `usuario_id` se não for JWT.
            if not usuario_id:
                return None # Não salva se não houver ID de usuário
            # Sanitiza dados sensíveis antes de salvar
            conversa_sanitized = self._sanitize_conversation_data(conversa)
            metricas_sanitized = self._sanitize_metrics_data(metricas)
            feedback_sanitized = self._sanitize_feedback_data(feedback_ia)
            
            simulation_id = save_simulation(
                usuario_id=usuario_id,
                modulo=modulo,
                produto_descricao=produto_descricao,
                perfil_cliente=perfil_cliente,
                conversa=conversa_sanitized,
                metricas=metricas_sanitized,
                feedback_ia=feedback_sanitized,
                is_exemplo=is_exemplo,
                session_id=session_id
            )
            
            logger.info(f"Simulação salva com segurança: {simulation_id} para usuário {usuario_id} - Módulo: {modulo}")
            return simulation_id
            
        except Exception as e:
            logger.error(f"Erro ao salvar simulação segura: {e}")
            return None
    
    async def get_user_simulations_secure(self, user_id: int, limit: int = 50) -> List[Dict[str, Any]]:
        """Obtém simulações apenas do usuário atual com validação de ownership"""
        try:
            simulations = get_user_simulations(user_id, limit)
            
            # Aplica sanitização adicional em todas as simulações
            sanitized_simulations = []
            for sim in simulations:
                sanitized_sim = self._sanitize_simulation_data(sim)
                sanitized_simulations.append(sanitized_sim)
            
            logger.debug(f"Histórico seguro carregado: {len(sanitized_simulations)} simulações para usuário {user_id}")
            return sanitized_simulations
            
        except Exception as e:
            logger.error(f"Erro ao obter simulações seguras: {e}")
            raise HTTPException(
                status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                detail="Erro ao carregar histórico"
            )
    
    async def validate_simulation_ownership(self, request: Request, simulation_id: int) -> bool:
        """Valida se a simulação pertence ao usuário atual de forma segura"""
        try:
            user_id = db_context.get_current_user_id(request)
            
            simulations = get_user_simulations(user_id, limit=1000)
            simulation_ids = [sim["id"] for sim in simulations]
            is_owner = simulation_id in simulation_ids
            
            if not is_owner:
                logger.warning(
                    f"Tentativa de acesso a simulação não pertencente ao usuário: "
                    f"User: {user_id}, Simulation: {simulation_id}"
                )
            
            return is_owner
            
        except Exception as e:
            logger.error(f"❌ Erro ao validar ownership da simulação: {e}")
            return False

    # ========== FUNÇÕES PARA MEMÓRIA DE LONGO PRAZO ==========
    
    @lru_cache(maxsize=128)
    def _get_cached_user_summary(self, user_id: int, modulo: str, cache_key: str) -> str:
        """Cache LRU para resumos de usuário com TTL implícito"""
        return self._calculate_user_summary(user_id, modulo)
    
    async def get_user_summary(self, user_id: int, modulo: str) -> str:
        """
        Retorna resumo estruturado do histórico recente do usuário
        Formato: "Nas últimas X simulações, probabilidade média = Y%. Lacuna: Z."
        """
        try:
            cache_key = f"{user_id}_{modulo}_{int(time.time() // 300)}"  # TTL de 5 minutos
            
            # Usa cache LRU com TTL implícito
            summary = self._get_cached_user_summary(user_id, modulo, cache_key)
            logger.debug(f"Resumo de histórico gerado para usuário {user_id} - Módulo: {modulo}")
            return summary
            
        except Exception as e:
            logger.warning(f"Erro ao gerar resumo do usuário: {e}")
            return ""
    
    def _calculate_user_summary(self, user_id: int, modulo: str) -> str:
        """Calcula resumo do histórico do usuário baseado nas últimas simulações"""
        try:
            simulations = get_user_simulations(user_id, limit=10)
            
            if not simulations:
                return "Sem histórico suficiente. Continue praticando!"
            
            # Filtra por módulo se especificado
            module_simulations = [s for s in simulations if s["modulo"] == modulo] if modulo else simulations
            recent_simulations = module_simulations[:3]  # Últimas 3 simulações
            
            if not recent_simulations:
                return f"Sem histórico no módulo {modulo}. Experimente praticar!"
            
            # Calcula métricas
            total_simulations = len(recent_simulations)
            avg_probability = self._calculate_avg_probability(recent_simulations)
            common_gaps = self._identify_common_gaps(recent_simulations)
            behavioral_trend = self._analyze_behavioral_trend(recent_simulations)
            
            # Constrói resumo
            summary_parts = []
            
            if avg_probability > 0:
                summary_parts.append(f"Probabilidade média de conversão: {avg_probability}%")
            
            if common_gaps:
                summary_parts.append(f"Lacuna: {common_gaps}")
            
            if behavioral_trend:
                summary_parts.append(f"Tendência: {behavioral_trend}")
            
            if not summary_parts:
                summary_parts.append("Continue praticando para gerar insights!")
            
            return ". ".join(summary_parts)
            
        except Exception as e:
            logger.error(f"Erro ao calcular resumo do usuário: {e}")
            return "Histórico em análise..."
    
    def _calculate_avg_probability(self, simulations: List[Dict[str, Any]]) -> int:
        """Calcula probabilidade média de conversão baseada nas métricas"""
        try:
            probabilities = []
            for sim in simulations:
                if sim.get("metricas"):
                    prob = sim["metricas"].get("probabilidade_conversao") or sim["metricas"].get("pontuacao", 0)
                    if isinstance(prob, (int, float)) and prob > 0:
                        probabilities.append(prob)
            
            if probabilities:
                return int(sum(probabilities) / len(probabilities))
            return 0
        except:
            return 0
    
    def _identify_common_gaps(self, simulations: List[Dict[str, Any]]) -> str:
        """Identifica lacunas comuns baseadas no feedback da IA"""
        try:
            gaps = []
            feedback_keywords = {
                "empatia": ["empatia", "empático", "conexão", "escuta"],
                "objeções": ["objeção", "resistência", "dúvida", "preocupação"],
                "valor": ["valor", "benefício", "ROI", "resultado"],
                "clareza": ["clareza", "específico", "detalhe", "exemplo"]
            }
            
            for sim in simulations:
                feedback = sim.get("feedback_ia", "").lower()
                for gap_type, keywords in feedback_keywords.items():
                    if any(keyword in feedback for keyword in keywords):
                        if gap_type not in gaps:
                            gaps.append(gap_type)
            
            if gaps:
                return ", ".join(gaps)
            return "boas práticas consolidadas"
        except:
            return "análise em andamento"
    
    def _analyze_behavioral_trend(self, simulations: List[Dict[str, Any]]) -> str:
        """Analisa tendência comportamental baseada nas classificações"""
        try:
            if len(simulations) < 2:
                return ""
            
            recent_classifications = []
            for sim in simulations:
                if sim.get("metricas"):
                    classificacao = sim["metricas"].get("classificacao")
                    if classificacao:
                        recent_classifications.append(classificacao)
            
            if len(recent_classifications) >= 2:
                latest = recent_classifications[0]
                previous = recent_classifications[1]
                
                if latest != previous:
                    return f"Evolução: {previous} → {latest}"
            
            return "consistência mantida"
        except:
            return ""
    
    async def get_user_behavioral_profile(self, user_id: int) -> Dict[str, Any]:
        """
        Analisa o perfil comportamental do usuário baseado no histórico
        Retorna: {"estilo": "AGRESSIVO|EMPÁTICO|TÉCNICO", "lacunas": ["empatia", "escuta_ativa"]}
        """
        try:
            simulations = get_user_simulations(user_id, limit=20)
            
            if not simulations:
                return {"estilo": "NEUTRO", "lacunas": ["experiência"]}
            
            # Análise de padrões comportamentais
            style_analysis = self._analyze_behavioral_style(simulations)
            gap_analysis = self._analyze_behavioral_gaps(simulations)
            
            return {
                "estilo": style_analysis,
                "lacunas": gap_analysis,
                "total_simulacoes": len(simulations),
                "ultima_atualizacao": datetime.now().isoformat()  
            }
            
        except Exception as e:
            logger.warning(f"❌ Erro ao analisar perfil comportamental: {e}")
            return {"estilo": "NEUTRO", "lacunas": []}

    def _analyze_behavioral_style(self, simulations: List[Dict[str, Any]]) -> str:
        """Analisa o estilo comportamental predominante"""
        try:
            style_scores = {"AGRESSIVO": 0, "EMPÁTICO": 0, "TÉCNICO": 0, "CONSULTIVO": 0}
            
            for sim in simulations:
                if sim.get("metricas"):
                    classificacao = sim["metricas"].get("classificacao", "").upper()
                    tom = sim["metricas"].get("tom", "").upper()
                    
                    # Mapeia classificações para estilos
                    if "AGRESSIVO" in classificacao or "PRESSAO" in tom:
                        style_scores["AGRESSIVO"] += 1
                    elif "EMPÁTICO" in classificacao or "EMPATIA" in tom:
                        style_scores["EMPÁTICO"] += 1
                    elif "TÉCNICO" in classificacao or any(word in tom for word in ["DADO", "TÉCNICO", "ESPECÍFICO"]):
                        style_scores["TÉCNICO"] += 1
                    elif "CONSULTIVO" in classificacao or "CONSULTIVO" in tom:
                        style_scores["CONSULTIVO"] += 1
            
            # Encontra estilo predominante
            predominant_style = max(style_scores.items(), key=lambda x: x[1])
            
            if predominant_style[1] > 0:
                return predominant_style[0]
            return "NEUTRO"
            
        except:
            return "NEUTRO"
    
    def _analyze_behavioral_gaps(self, simulations: List[Dict[str, Any]]) -> List[str]:
        """Identifica lacunas comportamentais recorrentes"""
        try:
            gap_counter = {}
            
            for sim in simulations:
                feedback = sim.get("feedback_ia", "").lower()
                
                # Detecta lacunas baseadas no feedback
                if any(word in feedback for word in ["empatia", "empático", "conexão"]):
                    gap_counter["empatia"] = gap_counter.get("empatia", 0) + 1
                
                if any(word in feedback for word in ["escuta", "ouvir", "perguntar"]):
                    gap_counter["escuta_ativa"] = gap_counter.get("escuta_ativa", 0) + 1
                
                if any(word in feedback for word in ["clareza", "específico", "detalhe"]):
                    gap_counter["clareza"] = gap_counter.get("clareza", 0) + 1
                
                if any(word in feedback for word in ["objeção", "resistência", "dúvida"]):
                    gap_counter["manejo_objecoes"] = gap_counter.get("manejo_objecoes", 0) + 1
            
            # Retorna lacunas que aparecem em pelo menos 30% das simulações
            threshold = len(simulations) * 0.3
            return [gap for gap, count in gap_counter.items() if count >= threshold]
            
        except:
            return []

    def _sanitize_conversation_data(self, conversa: Optional[Dict[str, Any]]) -> Optional[Dict[str, Any]]:
        """Remove dados sensíveis da conversa"""
        if not conversa:
            return conversa
        
        try:
            # Cria uma cópia para não modificar o original
            sanitized = conversa.copy()
            
            # Remove campos potencialmente sensíveis
            sensitive_fields = ['tokens', 'embeddings', 'ip_address', 'user_agent', 'session_id']
            for field in sensitive_fields:
                sanitized.pop(field, None)
            
            # Sanitiza mensagens individuais se existirem
            if 'mensagens' in sanitized and isinstance(sanitized['mensagens'], list):
                for msg in sanitized['mensagens']:
                    if isinstance(msg, dict):
                        msg.pop('ip', None)
                        msg.pop('user_agent', None)
                        msg.pop('location', None)
            
            return sanitized
            
        except Exception as e:
            logger.warning(f"Erro ao sanitizar dados de conversa: {e}")
            return conversa
    
    def _sanitize_metrics_data(self, metricas: Optional[Dict[str, Any]]) -> Optional[Dict[str, Any]]:
        """Remove dados sensíveis das métricas"""
        if not metricas:
            return metricas
        
        try:
            sanitized = metricas.copy()
            
            # Remove campos sensíveis
            sensitive_metrics = ['user_id', 'api_key', 'internal_tokens', 'model_weights']
            for field in sensitive_metrics:
                sanitized.pop(field, None)
            
            return sanitized
            
        except Exception as e:
            logger.warning(f"Erro ao sanitizar métricas: {e}")
            return metricas
    
    def _sanitize_feedback_data(self, feedback: Optional[str]) -> Optional[str]:
        """Sanitiza feedback para remover informações sensíveis"""
        if not feedback:
            return feedback
        
        try:
            # Remove possíveis tokens ou informações sensíveis
            sensitive_patterns = [
                'sk-',  # OpenAI keys
                'AIza', # Google keys  
                'hf_',  # Huggingface keys
                'password',
                'senha',
                'token'
            ]
            
            sanitized = feedback
            for pattern in sensitive_patterns:
                if pattern in sanitized.lower():
                    sanitized = sanitized.replace(pattern, '[REDACTED]')
            
            return sanitized
            
        except Exception as e:
            logger.warning(f"Erro ao sanitizar feedback: {e}")
            return feedback
    
    def _sanitize_simulation_data(self, simulation: Dict[str, Any]) -> Dict[str, Any]:
        """Sanitiza dados completos da simulação"""
        try:
            sanitized = simulation.copy()
            
            # Aplica sanitização em todos os campos
            if 'conversa' in sanitized:
                sanitized['conversa'] = self._sanitize_conversation_data(sanitized['conversa'])
            
            if 'metricas' in sanitized:
                sanitized['metricas'] = self._sanitize_metrics_data(sanitized['metricas'])
            
            if 'feedback_ia' in sanitized:
                sanitized['feedback_ia'] = self._sanitize_feedback_data(sanitized['feedback_ia'])
            
            return sanitized
            
        except Exception as e:
            logger.warning(f"Erro ao sanitizar simulação completa: {e}")
            return simulation

# Instância global
history_service = HistoryService()
