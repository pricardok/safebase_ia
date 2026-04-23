from dataclasses import dataclass, field
from datetime import datetime
from typing import Dict, List, Optional

from app.core.security import generate_api_key


@dataclass
class ApiKeyRecord:
    name: str
    key: str
    scopes: List[str]
    is_active: bool = True
    created_at: datetime = field(default_factory=datetime.utcnow)


class ApiKeyService:
    def __init__(self):
        self._keys: Dict[str, ApiKeyRecord] = {}

    def create_api_key(self, name: str, scopes: Optional[List[str]] = None) -> Dict[str, any]:
        key = generate_api_key()
        record = ApiKeyRecord(name=name, key=key, scopes=scopes or ["default"])
        self._keys[key] = record
        return {"name": name, "key": key, "scopes": record.scopes}

    def validate_key(self, key: str) -> Optional[ApiKeyRecord]:
        record = self._keys.get(key)
        if record and record.is_active:
            return record
        return None

    def get_key_info(self, key: str) -> Optional[ApiKeyRecord]:
        return self._keys.get(key)


api_key_service = ApiKeyService()
