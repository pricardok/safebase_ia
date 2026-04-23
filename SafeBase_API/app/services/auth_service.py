from app.core.security import verify_password, get_password_hash, create_access_token


class AuthService:
    def __init__(self):
        # Mock user store. Em produção, substitua por acesso ao DB.
        self._users = {
            "admin": {
                "username": "admin",
                "email": "admin@safebase.local",
                "full_name": "SafeBase Administrator",
                "hashed_password": get_password_hash("Admin@123"),
                "is_active": True,
            }
        }

    def authenticate(self, username: str, password: str):
        user = self._users.get(username)
        if not user or not verify_password(password, user["hashed_password"]):
            return None
        return user

    def create_access_token(self, subject: str):
        return create_access_token(subject)
