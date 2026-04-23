# backend/app/services/planos_service.py
import logging
from typing import List, Dict, Any, Optional
from datetime import datetime, timedelta
from fastapi import HTTPException, Request

from app.database import (
    get_planos_publicos, get_plano_do_cliente, get_plano_trial,
    atribuir_plano_cliente,
    criar_desconto_cliente, get_estatisticas_uso_cliente, get_plano_by_id
)
from app.models_planos import (
    PlanoResponse, PlanoEfetivoResponse, EstatisticasUsoResponse,
    UpgradePlanoRequest, DescontoClienteCreate, DescontoClienteResponse
)

logger = logging.getLogger(__name__)

class PlanosService:
    
    async def listar_planos_publicos(self) -> List[Dict[str, Any]]:
        """
        Busca e retorna a lista de planos públicos, usando cache.
        """
        try:
            return get_planos_publicos()
        except Exception as e:
            logger.error(f"Erro ao listar planos públicos no serviço: {e}")
            raise HTTPException(status_code=500, detail="Não foi possível carregar os planos.")
            
    async def obter_plano_do_cliente(self, cliente_id: str) -> PlanoEfetivoResponse:
        """
        Obtém o plano efetivo de um cliente, incluindo descontos e status de trial.
        """
        if not cliente_id:
            raise HTTPException(status_code=400, detail="ID do cliente não fornecido.")

        try:
            # A função get_plano_do_cliente já faz todo o trabalho pesado
            plano_data = get_plano_do_cliente(cliente_id)
            if not plano_data:
                raise HTTPException(status_code=404, detail="Plano do cliente não encontrado.")

            # O Pydantic fará a validação e conversão para o modelo de resposta
            return PlanoEfetivoResponse(**plano_data)
        except Exception as e:
            logger.error(f"Erro ao obter plano do cliente {cliente_id}: {e}")
            raise HTTPException(status_code=500, detail="Erro ao buscar informações do plano.")

# Instância global do serviço
planos_service = PlanosService()