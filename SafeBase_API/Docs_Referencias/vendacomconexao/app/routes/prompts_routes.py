from fastapi import APIRouter, HTTPException
from typing import Optional
import logging

from app.prompts.registry import get_all_prompts, get_prompts_by_module, get_prompt_template
from app.services.prompt_service import prompt_service

logger = logging.getLogger(__name__)
router = APIRouter(
    prefix="/prompts",
    tags=["Prompts"]
)

@router.get("")
async def listar_prompts(modulo: Optional[str] = None):
    """Lista todos os prompts disponíveis ou filtra por módulo"""
    try:
        if modulo:
            prompts = get_prompts_by_module(modulo)
        else:
            prompts = get_all_prompts()
        return {"prompts": prompts, "total": len(prompts)}
    except Exception as e:
        logger.error(f"Erro ao listar prompts: {e}")
        raise HTTPException(status_code=500, detail="Erro ao carregar prompts")

@router.get("/{prompt_name}")
async def obter_prompt_info(prompt_name: str):
    """Obtém informações detalhadas de um prompt específico"""
    try:
        all_prompts = get_all_prompts()
        prompt_info = all_prompts.get(prompt_name)
        template_content = get_prompt_template(prompt_name)
        if not prompt_info:
            raise HTTPException(status_code=404, detail="Prompt não encontrado")
        return {
            "nome": prompt_name,
            "modulo": prompt_info.get("modulo", "geral"),
            "descricao": prompt_info.get("descricao", ""),
            "template": template_content,
            "parametros": prompt_info.get("parametros", [])
        }
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Erro ao obter prompt {prompt_name}: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao obter prompt")

@router.post("/{prompt_name}/render")
async def renderizar_prompt(prompt_name: str, request: dict):
    """Renderiza um prompt com os parâmetros fornecidos (desenvolvimento)"""
    try:
        parametros = request.get("parametros", {})
        prompt_renderizado = prompt_service.renderizar_prompt(prompt_name, **parametros)
        return {"prompt_renderizado": prompt_renderizado}
    except Exception as e:
        logger.error(f"Erro ao renderizar prompt {prompt_name}: {e}")
        raise HTTPException(status_code=400, detail=f"Erro ao renderizar prompt: {e}")