# SafeBase API - exemplos de uso via curl

Base URL: http://localhost:8000
Header de autenticacao: X-API-Key

## IA

### POST /ia/query
```bash
curl -X POST http://localhost:8000/ia/query -H "Content-Type: application/json" -H "X-API-Key: <SUA_API_KEY>" -d "{\"query\":\"Quais os top waits nas ultimas 24h?\",\"context\":{\"origin\":\"chat\"},\"agent_id\":\"chat\"}"
```

### POST /ia/keys
```bash
curl -X POST http://localhost:8000/ia/keys -H "Content-Type: application/json" -H "X-API-Key: <SUA_API_KEY>" -d "{\"provider_name\":\"openai\",\"api_key\":\"<OPENAI_API_KEY>\",\"descricao\":\"chave openai principal\",\"ativa\":true,\"metadados\":{\"origem\":\"manual\"}}"
```

### Inserir chave via script local
```bash
python util/insert_ia_key.py --provider openai --api-key "<OPENAI_API_KEY>" --descricao "OpenAI chave 01" --meta origem=manual
```

## Chat

### GET /chat/conversas
```bash
curl -X GET http://localhost:8000/chat/conversas -H "X-API-Key: <SUA_API_KEY>"
```

### POST /chat/conversas
```bash
curl -X POST http://localhost:8000/chat/conversas -H "Content-Type: application/json" -H "X-API-Key: <SUA_API_KEY>" -d "{\"titulo\":\"Analise de performance\"}"
```
curl -X POST http://localhost:8000/chat/conversas/1/mensagens -H "Content-Type: application/json" -H "X-API-Key: <SUA_API_KEY>" -d "{\"conteudo\":\"Analise o ambiente\"}"

### GET /chat/conversas/{id}
```bash
curl -X GET http://localhost:8000/chat/conversas/1 -H "X-API-Key: <SUA_API_KEY>"
```

### POST /chat/conversas/{id}/mensagens
```bash
curl -X POST http://localhost:8000/chat/conversas/1/mensagens -H "Content-Type: application/json" -H "X-API-Key: <SUA_API_KEY>" -d "{\"conteudo\":\"Porque tivemos tantas falhas de job ontem?\"}"
```

### POST /normalize/run
```bash
curl -X POST http://localhost:8000/normalize/run -H "X-API-Key: <SUA_API_KEY>"
```

for /f "tokens=2 delims=:," %i in ('curl -s -X POST http://localhost:8000/auth/login -H "Content-Type: application/json" -d "{\"username\":\"paulo.kuhn\",\"password\":\"!@awssaaqq\"}"') do for /f "delims=" %j in ("%i") do curl -s -X GET http://localhost:8000/auth/me -H "Authorization: Bearer %j"

# login
curl -X POST http://localhost:8000/auth/login -H "Content-Type: application/json" -d "{\"username\":\"paulo.kuhn\",\"password\":\"!@awssaaqq\"}"

# Auth
curl -s -X GET http://localhost:8000/auth/me -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJwYXVsby5rdWhuIiwiZXhwIjoxNzc5MzkxMDkwLCJzY29wZSI6ImFjY2Vzc190b2tlbiIsInJvbGVzIjpbImFkbWluIl0sInBlcm1pc3Npb25zIjpbInJiYWMubWFuYWdlIiwicmJhYy5yZWFkIiwidXNlcnMubWFuYWdlIiwidXNlcnMucmVhZCJdfQ.rA5kpi7CoaNWjBCQ3nEB3Gk9lEuNLlEhN5Y9aTjfAgc"

curl -X POST http://localhost:8000/chat/conversas/1/mensagens -H "Content-Type: application/json" -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJwYXVsby5rdWhuIiwiZXhwIjoxNzc5MzkxMDkwLCJzY29wZSI6ImFjY2Vzc190b2tlbiIsInJvbGVzIjpbImFkbWluIl0sInBlcm1pc3Npb25zIjpbInJiYWMubWFuYWdlIiwicmJhYy5yZWFkIiwidXNlcnMubWFuYWdlIiwidXNlcnMucmVhZCJdfQ.rA5kpi7CoaNWjBCQ3nEB3Gk9lEuNLlEhN5Y9aTjfAgc"" -d "{\"conteudo\":\"Analise o ambiente\"}"
