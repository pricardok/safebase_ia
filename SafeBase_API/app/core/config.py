from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    api_title: str = "SafeBase API"
    secret_key: str = "replace-this-secret-key"
    algorithm: str = "HS256"
    access_token_expire_minutes: int = 60
    refresh_token_expire_days: int = 30
    database_url: str = "mssql+pymssql://<usuario>:<senha>@<host>/<database>"
    api_key_header_name: str = "X-API-Key"
    api_key_prefix: str = "ApiKey"
    default_api_key: str = ""
    max_ingestion_batch_size: int = 100
    crypto_master_key: str = "default-master-key-for-dev"
    normalization_enabled: bool = True
    normalization_interval_seconds: int = 60
    normalization_batch_size: int = 100
    ia_key_failure_threshold: int = 3
    ia_debug_logs_enabled: bool = False
    knowledge_md_enabled: bool = True
    knowledge_md_max_items: int = 5
    knowledge_md_max_chars: int = 1500

    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"
 
settings = Settings()
