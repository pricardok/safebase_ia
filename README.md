# SafeBase

Sistema de gerenciamento e observabilidade para bancos MSSQL, com API central e orquestracao de IA para analises e chat.

## Componentes principais
- **API central (FastAPI)**: ingestao de dados, historico de chat, IA e normalizacao.
- **Servico de coleta (Agent Service)**: coleta dados locais e envia para a API.
- **Banco central**: armazena payloads brutos e dados normalizados.

## Estrutura relevante
- `SafeBase_API/`: backend FastAPI e documentacao.
- `SafeBase_InitDB/`: projetos e scripts de banco.
- `SafeBase_Installer/`: instalador e utilitarios Windows.

## Como rodar a API (dev)
1) Ative o venv e configure o `.env` em `SafeBase_API/`.
2) Inicie a API:
```bash
uvicorn app.main:app --reload
```

## Variaveis de ambiente (exemplos)
Veja `SafeBase_API/.env.example`.

Principais:
- `DATABASE_URL`
- `DEFAULT_API_KEY`
- `CRYPTO_MASTER_KEY`
- `NORMALIZATION_ENABLED`
- `NORMALIZATION_INTERVAL_SECONDS`

## Normalizacao de payloads
- Payloads chegam em `agent_payloads` e sao normalizados por tipo.
- O servico roda em background a cada minuto (configuravel via env).

## Documentacao
- Exemplos de chamadas: `SafeBase_API/documentacao/api_curl.md`
- Scripts SQL de normalizacao: `SafeBase_API/documentacao/normalization_*.sql`
