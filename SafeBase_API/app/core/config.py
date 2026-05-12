from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    api_title: str = "SafeBase API"
    secret_key: str = "replace-this-secret-key"
    algorithm: str = "HS256"
    access_token_expire_minutes: int = 60
    database_url: str = "mssql+pymssql://SafeBaseAPI_app:sfew133Awww@FPC-2571/SafeBaseAPI"
    api_key_header_name: str = "X-API-Key"
    api_key_prefix: str = "ApiKey"
    default_api_key: str = "Hx7z9Q2wR4mP6tY8uVbN3cL1sZ0kF5hE"
    max_ingestion_batch_size: int = 100

    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"

settings = Settings()
