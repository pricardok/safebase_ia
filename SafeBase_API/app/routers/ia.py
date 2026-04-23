from fastapi import APIRouter, Depends
from app.core.dependencies import require_auth
from app.schemas.ia import IAQueryRequest, IAQueryResponse
from app.services.ia_service import IAService

router = APIRouter()

ia_service = IAService()

@router.post("/ia/query", response_model=IAQueryResponse)
async def query_ia(payload: IAQueryRequest, auth=Depends(require_auth)):
    result = ia_service.query_insights(payload.dict())
    return {"query": payload.query, "result": result}
