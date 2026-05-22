# SafeBase API - Contrato para Frontend / Lovable

## Visão geral
Este documento descreve as rotas, autenticação e payloads principais da API `SafeBase_API`, para que o front-end (Lovable) consiga consumir os serviços corretamente.

A API expõe documentação OpenAPI em:
- `GET /openapi.json`
- `GET /docs`
- `GET /redoc`

## Base URL
A API roda em `http://localhost:8000` quando executada localmente ou em `http://<host>:8000` quando em container/Docker.

## Autenticação
A API suporta duas formas de autenticação:

1. JWT Bearer token
   - Header: `Authorization: Bearer <token>`
   - Usado para gerar API Keys em `/auth/api-keys`

2. API Key
   - Header: `X-API-Key: <key>`
   - Usado para acessar endpoints de negócio e IA

### Usuário de login padrão para integração técnica
Atualmente há um usuário mockado para testar a conexão inicial com a API e gerar chaves de API:
- `username`: `admin`
- `password`: `Admin@123`

> Importante: essa conta não representa necessariamente o usuário final do sistema. Ela é usada apenas como credencial técnica para obter o token JWT e criar API Keys.

## Diferença entre integração e UX de usuário final
O documento `prompt lovable.txt` descreve o objetivo de front-end para usuários finais, que deve ser um chat completo no estilo DeepSeek / ChatGPT.

Isso significa:
- o front deve ser pensado para usuários finais do sistema,
- a API precisa fornecer rotas e autenticação claras para essa interface,
- mas o modelo de usuário final e a experiência visual do chat são definidos no prompt de front-end,
- o login API/JWT é apenas o mecanismo de conexão entre front e backend.

## Endpoints

### 1. `GET /health`
- Descrição: Verifica se a API está ativa
- Autenticação: nenhuma
- Resposta:
```json
{
  "status": "ok",
  "service": "SafeBase API"
}
```

### 2. `POST /auth/login`
- Descrição: Autentica o usuário e retorna token JWT
- Autenticação: nenhuma
- Body:
```json
{
  "username": "admin",
  "password": "Admin@123"
}
```
- Resposta:
```json
{
  "access_token": "<jwt-token>",
  "token_type": "bearer"
}
```

### 3. `POST /auth/api-keys`
- Descrição: Cria uma nova API Key para acesso aos endpoints protegidos
- Autenticação: JWT Bearer
- Header:
  - `Authorization: Bearer <token>`
- Body:
```json
{
  "name": "frontend-key",
  "scopes": ["default"]
}
```
- Resposta:
```json
{
  "name": "frontend-key",
  "key": "<generated-api-key>",
  "scopes": ["default"]
}
```

### 4. `POST /ingest/agent-data`
- Descrição: Envia dados de agente para ingestão
- Autenticação: API Key
- Header:
  - `X-API-Key: <key>`
- Body:
```json
{
  "agent_id": "agent-123",
  "timestamp": "2026-05-21T12:00:00Z",
  "payload_type": "event",
  "payload_data": {
    "key1": "value1",
    "count": 5
  },
  "metadata": {
    "source": "frontend",
    "environment": "production"
  }
}
```
- Resposta exemplo:
```json
{
  "status": "success",
  "message": "Data ingested successfully",
  "agent_id": "agent-123",
  "details": {
    "...": "..."
  }
}
```

### 5. `POST /normalize/run`
- Descrição: Executa um ciclo de normalização manualmente
- Autenticação: API Key
- Header:
  - `X-API-Key: <key>`
- Sem body
- Resposta:
```json
{
  "status": "ok",
  "message": "Normalization cycle executed"
}
```

### 6. `POST /ia/query`
- Descrição: Envia uma consulta para o módulo de IA
- Autenticação: API Key
- Header:
  - `X-API-Key: <key>`
- Body:
```json
{
  "query": "Preciso de insights sobre vendas",
  "context": {
    "user": "frontend",
    "session": "abc123"
  },
  "agent_id": "agent-123"
}
```
- Resposta exemplo:
```json
{
  "query": "Preciso de insights sobre vendas",
  "result": {
    "...": "..."
  }
}
```

### 7. `POST /ia/keys`
- Descrição: Registra uma chave de IA para provedores suportados
- Autenticação: API Key
- Header:
  - `X-API-Key: <key>`
- Body:
```json
{
  "provider_name": "openai",
  "api_key": "<chave-externa>",
  "descricao": "Chave OpenAI para IA",
  "ativa": true,
  "prioridade": 1,
  "metadados": {
    "regiao": "us-east"
  }
}
```
- Resposta exemplo:
```json
{
  "id": 1,
  "provider_id": 1,
  "provider_name": "openai",
  "hash_chave": "...",
  "descricao": "Chave OpenAI para IA",
  "ativa": true,
  "prioridade": 1
}
```

### 8. `GET /chat/conversas`
- Descrição: Lista conversas de chat do usuário
- Autenticação: API Key
- Header:
  - `X-API-Key: <key>`
- Resposta exemplo:
```json
{
  "conversas": [
    {
      "id": 1,
      "titulo": "Atendimento",
      "preview": "Última mensagem...",
      "atualizado_em": "2026-05-21T12:00:00"
    }
  ]
}
```

### 9. `POST /chat/conversas`
- Descrição: Cria nova conversa
- Autenticação: API Key
- Header:
  - `X-API-Key: <key>`
- Body:
```json
{
  "titulo": "Suporte cliente"
}
```
- Resposta exemplo:
```json
{
  "id": 2,
  "titulo": "Suporte cliente",
  "criado_em": "2026-05-21T12:00:00",
  "atualizado_em": "2026-05-21T12:00:00",
  "mensagens": []
}
```

### 10. `GET /chat/conversas/{conversation_id}`
- Descrição: Recupera conversa e mensagens
- Autenticação: API Key
- Header:
  - `X-API-Key: <key>`
- Resposta exemplo:
```json
{
  "id": 2,
  "titulo": "Suporte cliente",
  "criado_em": "2026-05-21T12:00:00",
  "atualizado_em": "2026-05-21T12:00:00",
  "mensagens": [
    {
      "id": 1,
      "papel": "user",
      "conteudo": "Olá",
      "criado_em": "2026-05-21T12:00:01"
    }
  ]
}
```

### 11. `POST /chat/conversas/{conversation_id}/mensagens`
- Descrição: Envia mensagem para uma conversa e recebe resposta do assistente
- Autenticação: API Key
- Header:
  - `X-API-Key: <key>`
- Body:
```json
{
  "conteudo": "Olá, preciso de ajuda"
}
```
- Resposta exemplo:
```json
{
  "conversa_id": 2,
  "user_message": {
    "id": 3,
    "papel": "user",
    "conteudo": "Olá, preciso de ajuda",
    "criado_em": "2026-05-21T12:01:00"
  },
  "assistant_message": {
    "id": 4,
    "papel": "assistant",
    "conteudo": "Claro, como posso ajudar?",
    "criado_em": "2026-05-21T12:01:01"
  }
}
```

## Observações para o front
- Use `POST /auth/login` para obter o token JWT inicial.
- Use o JWT para criar API Keys em `/auth/api-keys`.
- Use `X-API-Key` nos endpoints de negócio e IA.
- O front deve tratar erros 401/403 para autenticação e 422 para validações.

## Mensagem pronta para Lovable
O front pode usar este documento como contrato e referência de implementação. Todas as rotas já estão definidas e a API expõe documentação OpenAPI para integração automática.
