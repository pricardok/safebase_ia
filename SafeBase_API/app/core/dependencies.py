from fastapi import Depends, HTTPException, Request, status
from typing import Iterable, Set


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


def _normalize_set(values: Iterable[str]) -> Set[str]:
    return {value.strip().lower() for value in values if value}


def require_roles(required_roles: Iterable[str]):
    required_set = _normalize_set(required_roles)

    def _checker(request: Request):
        auth = require_auth(request)
        user_roles = _normalize_set(auth.get("roles", []))
        if not required_set.issubset(user_roles):
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN,
                detail="Missing required roles",
            )
        return auth

    return _checker


def require_permissions(required_permissions: Iterable[str]):
    required_set = _normalize_set(required_permissions)

    def _checker(request: Request):
        auth = require_auth(request)
        user_permissions = _normalize_set(auth.get("permissions", []))
        if not required_set.issubset(user_permissions):
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN,
                detail="Missing required permissions",
            )
        return auth

    return _checker
