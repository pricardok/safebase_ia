# Projeto SafeBase API - Documentação

## Objetivo do projeto

Criar a API central do SafeBase com orquestração de IA e ingestão de dados de agentes, usando uma arquitetura moderna e segura.

A API deve ser:
- modular e segmentada em camadas lógicas
- segura com autenticação JWT para usuários
- segura com API Keys para serviços e integrações
- capaz de receber dados de agentes e persistir no banco SQL Server
- documentada para facilitar desenvolvimento, testes e operação

## Estrutura atual do projeto

```
SafeBase_API/
  .env
  .env.example
  README.md
  requirements.txt
  app/
    __init__.py
    main.py
    core/
      config.py
      security.py
      dependencies.py
    db/
      base.py
      session.py
      models.py
    middleware/
      auth_middleware.py
    routers/
      __init__.py
      auth.py
      health.py
      agents.py
      ia.py
    schemas/
      agent.py
      ia.py
      token.py
      user.py
    services/
      auth_service.py
      apikey_service.py
      ia_service.py
  documentacao/
    Projeto_SafeBase_API.md
```

## O que já está implementado

- backend FastAPI em `app/main.py`
- autenticação híbrida com JWT e API Key
- endpoint de ingestão `POST /ingest/agent-data`
- persistência em SQL Server via `pymssql`
- documentação de projeto no `documentacao/`
- exemplo de configuração em `.env.example`

## Endpoints atuais

### Autenticação
- `POST /auth/login` — obtém token JWT
- `POST /auth/api-keys` — cria nova API Key (requer JWT)

### Agentes
- `POST /ingest/agent-data` — recebe payload do agente e salva no banco
- `GET /agents/{agent_id}/status` — verifica o status do agente (dummy)

### IA
- `POST /ia/query` — consulta de IA básica

### Monitoramento
- `GET /health` — verifica se a API está ativa

## Configuração do banco de dados

Já estamos usando `pymssql` para conexão direta com SQL Server, sem ODBC intermediário.

Exemplo no `.env`:

```dotenv
DATABASE_URL=mssql+pymssql://SafeBaseAPI_app:sfew133Awww@FPC-2571/SafeBaseAPI
```

### Observação
- A conexão direta funciona melhor para o seu ambiente Windows
- Não é necessário instalar driver ODBC para este projeto
- Basta manter `DATABASE_URL` apontando para o SQL Server com `mssql+pymssql://`

## Variáveis de ambiente atuais

```dotenv
SECRET_KEY=...
ALGORITHM=HS256
ACCESS_TOKEN_EXPIRE_MINUTES=60
DATABASE_URL=mssql+pymssql://user:password@server/database
API_KEY_HEADER_NAME=X-API-Key
API_KEY_PREFIX=ApiKey
DEFAULT_API_KEY=...
MAX_INGESTION_BATCH_SIZE=100
```

- `MAX_INGESTION_BATCH_SIZE`: limite de registros por lote de ingestão. Se não definido, o valor padrão é `100`.
- para testes com volumes maiores, o valor pode ser aumentado para `1000` ou mais, desde que o sistema suporte.

## Fluxo de autenticação

### JWT
1. O cliente chama `POST /auth/login`
2. A API retorna `access_token`
3. Chamadas protegidas usam:
   - `Authorization: Bearer <token>`

### API Key
1. Gera-se uma chave com `POST /auth/api-keys`
2. Usa-se no header:
   - `X-API-Key: <key>`
3. A API valida a chave no middleware

## Persistência de dados

- Agentes são salvos em `agents`
- Payloads de agente são salvos em `agent_payloads`
- O campo `metadata_json` foi adicionado ao banco para armazenar metadados

## Como rodar a API

```powershell
cd C:\Projetos\dev\safebase\SafeBase_API
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\.venv\Scripts\python.exe -m uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

## Teste rápido de ingestão

```bash
curl -X POST "http://127.0.0.1:8000/ingest/agent-data" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: Hx7z9Q2wR4mP6tY8uVbN3cL1sZ0kF5hE" \
  -d '{"agent_id":"agent-001","timestamp":"2026-04-23T12:00:00Z","payload_type":"test_payload","payload_data":{"message":"Teste de envio com API Key","value":123},"metadata":{"source":"curl-test"}}'
```

## Atualizações recentes

- A API foi migrada para `app/` em vez de `backend-api/`
- O SQL Server agora usa `pymssql` direto
- O `agent_payloads` recebeu a coluna `metadata_json`
- O endpoint de ingestão já salva dados no banco

## Integração SafeBase_InitDB

A próxima etapa é fazer as instâncias SafeBase enviarem dados para esta API usando o SQL CLR function `SafeBase_InitDB\CoreTask\Functions\fncResolveHttpRequest.cs`.

### Plano de integração

1. Utilizar `fncResolveHttpRequest` para chamar o endpoint:
   - `POST /ingest/agent-data`
   - `Content-Type: application/json`
   - `X-API-Key: <DEFAULT_API_KEY>`

2. O payload deve seguir o contrato atual do backend:
   - `agent_id`
   - `timestamp`
   - `payload_type`
   - `payload_data` (JSON)
   - `metadata` (JSON)

3. O backend deve persistir todos os dados recebidos no banco, sem aplicar lógica de IA imediata.
   - cada payload pode representar um processo diferente de monitoramento
   - `payload_type` permite identificar a origem e segmento dos dados
   - a API grava em `agent_payloads` e mantém o registro do `agent` em `agents`

4. O conteúdo do payload pode ser genérico, e a análise futura pode ser feita a partir dos dados já salvos.
   - por enquanto, o agente de IA não processa nada ao receber os dados
   - a API é responsável apenas por armazenar a informação

5. Criar um job/executável no ambiente SQL local que:
   - lê dados do SafeBase local
   - monta JSON de payload
   - chama `fncResolveHttpRequest`
   - trata falhas e faz retry/log local

### Envio em blocos
- defina `MAX_INGESTION_BATCH_SIZE` no `.env` conforme o volume de dados
- se o payload tiver muitos itens, envie em blocos menores e não em um JSON único
- para `payload_data.logins` use chunking por `100`, `500` ou `1000` registros conforme o limite
- `fncResolveHttpRequest` suporta enviar qualquer JSON, mas o backend já valida o tamanho de lote

### Observações

- `fncResolveHttpRequest.cs` já suporta cabeçalhos customizados e corpo de requisição
- o importante é enviar JSON e o header `X-API-Key`
- mantendo um único endpoint de ingestão, podemos receber qualquer tipo de dados e classificá-los por `payload_type`
- se necessário mais adiante, podemos criar rotas específicas para cada tipo de processo, mas por agora um endpoint genérico é suficiente

## Próximos passos recomendados

1. adicionar endpoints de leitura e filtros
2. persistir usuários e chaves de API no banco
3. implementar migrações formais de esquema
4. adicionar testes automatizados
5. expandir a orquestração de IA com provedores reais
