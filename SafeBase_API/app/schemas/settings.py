from typing import Optional

from pydantic import BaseModel, Field


class UserSettingsResponse(BaseModel):
    default_categoria_codigo: Optional[str] = None
    default_mode: Optional[str] = None
    default_temperature: Optional[float] = None
    default_max_tokens: Optional[int] = None


class UserSettingsUpdate(BaseModel):
    default_categoria_codigo: Optional[str] = Field(None, max_length=128)
    default_mode: Optional[str] = Field(None, max_length=32)
    default_temperature: Optional[float] = None
    default_max_tokens: Optional[int] = None
