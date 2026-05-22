from datetime import datetime
from typing import Any, Dict, List, Optional

from pydantic import BaseModel, Field


class ChartDataset(BaseModel):
    label: str
    data: List[float]


class ChartPayload(BaseModel):
    type: str = Field(..., description="bar|line|pie")
    title: str
    labels: List[str]
    datasets: List[ChartDataset]


class ChartShareRequest(BaseModel):
    titulo: Optional[str] = None
    categoria_codigo: Optional[str] = None
    chart: ChartPayload
    expires_in_hours: Optional[int] = None


class ChartShareResponse(BaseModel):
    share_url: str
    token: str
    expires_at: Optional[datetime] = None


class ChartShareDetailResponse(BaseModel):
    titulo: Optional[str] = None
    categoria_codigo: Optional[str] = None
    chart: Dict[str, Any]
    expires_at: Optional[datetime] = None
