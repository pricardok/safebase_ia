from datetime import datetime
from typing import Any, Dict, Optional

from pydantic import BaseModel

from app.schemas.charts import ChartPayload


class ChartFavoriteCreate(BaseModel):
    titulo: Optional[str] = None
    categoria_codigo: Optional[str] = None
    chart: ChartPayload


class ChartFavoriteUpdate(BaseModel):
    titulo: Optional[str] = None


class ChartFavoriteResponse(BaseModel):
    id: int
    titulo: Optional[str] = None
    categoria_codigo: Optional[str] = None
    chart: Dict[str, Any]
    criado_em: datetime
