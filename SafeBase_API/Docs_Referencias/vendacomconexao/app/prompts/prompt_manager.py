"""
Gerenciador central de prompts com renderização e versionamento
"""

import logging
from typing import Dict, Any, Optional
from app.prompts.registry import obter_prompt, obter_metadados, listar_prompts_por_modulo

logger = logging.getLogger(__name__)

class PromptManager:
    def __init__(self):
        self.registry = obter_prompt
        self.metadata = obter_metadados
        
    def render(self, prompt_name: str, **kwargs) -> str:
        """
        Renderiza um prompt com os parâmetros fornecidos
        """
        try:
            # Obter template do registry
            template = self.registry(prompt_name)
            
            # Renderizar com parâmetros
            rendered_prompt = template.format(**kwargs)
            
            logger.debug(f"Prompt '{prompt_name}' renderizado com {len(kwargs)} parâmetros")
            return rendered_prompt
            
        except KeyError as e:
            logger.error(f"Parâmetro faltando para prompt '{prompt_name}': {e}")
            raise ValueError(f"Parâmetro faltando: {e}")
        except Exception as e:
            logger.error(f"Erro ao renderizar prompt '{prompt_name}': {e}")
            raise
    
    def get_available_prompts(self, modulo: str = None) -> Dict[str, str]:
        """
        Lista todos os prompts disponíveis, opcionalmente filtrado por módulo
        """
        return listar_prompts_por_modulo(modulo)
    
    def get_prompt_info(self, prompt_name: str) -> Dict[str, Any]:
        """
        Obtém informações e metadados de um prompt
        """
        try:
            template = self.registry(prompt_name)
            metadata = self.metadata(prompt_name)
            
            return {
                "nome": prompt_name,
                "template": template,
                "metadados": metadata,
                "parametros_necessarios": self._extract_parameters(template)
            }
        except ValueError as e:
            logger.error(f"Erro ao obter info do prompt '{prompt_name}': {e}")
            raise
    
    def _extract_parameters(self, template: str) -> list:
        """
        Extrai parâmetros do template (método simples)
        """
        import re
        params = re.findall(r'\{(\w+)\}', template)
        return list(set(params))  # Remove duplicatas
    
    def validate_parameters(self, prompt_name: str, parameters: Dict[str, Any]) -> bool:
        """
        Valida se todos os parâmetros necessários estão presentes
        """
        try:
            template = self.registry(prompt_name)
            required_params = self._extract_parameters(template)
            
            missing_params = [param for param in required_params if param not in parameters]
            
            if missing_params:
                logger.warning(f"Parâmetros faltando para '{prompt_name}': {missing_params}")
                return False
            
            return True
            
        except Exception as e:
            logger.error(f"❌ Erro na validação de parâmetros: {e}")
            return False

# Instância global para uso em toda a aplicação
prompt_manager = PromptManager()