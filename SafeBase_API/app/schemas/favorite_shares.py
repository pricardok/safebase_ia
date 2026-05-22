from datetime import datetime
from typing import Any, Dict, Optional

from pydantic import BaseModel


class ChartFavoriteShareRequest(BaseModel):
    expires_in_hours: Optional[int] = None


class ChartFavoriteShareResponse(BaseModel):
    share_url: str
    token: str
    expires_at: Optional[datetime] = None


class ChartFavoriteShareDetailResponse(BaseModel):
    titulo: Optional[str] = None
    categoria_codigo: Optional[str] = None
    chart: Dict[str, Any]
    expires_at: Optional[datetime] = None
