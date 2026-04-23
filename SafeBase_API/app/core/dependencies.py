from fastapi import Depends, HTTPException, Request, status


def require_auth(request: Request):
    auth = getattr(request.state, "auth", None)
    if not auth or auth.get("type") == "anonymous":
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Authentication required",
            headers={"WWW-Authenticate": "Bearer"},
        )
    return auth


def require_jwt(request: Request):
    auth = require_auth(request)
    if auth.get("type") != "jwt":
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="JWT token required",
        )
    return auth


def require_api_key(request: Request):
    auth = require_auth(request)
    if auth.get("type") != "api_key":
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="API Key required",
        )
    return auth
