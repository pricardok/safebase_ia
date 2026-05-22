SafeBase API

Projeto FastAPI mínimo para integrar com o workspace SafeBase.

Requisitos
- Python 3.10+

Instruções rápidas (Windows - PowerShell):

1. Criar e ativar venv:
   python -m venv .venv
   .\.venv\Scripts\Activate.ps1

2. Instalar dependências:
   pip install -r requirements.txt

3. Rodar o servidor localmente:
   uvicorn app.main:app --reload --host 0.0.0.0 --port 8000

4. Rodar com Docker:
   docker build -t safebase_api .
   docker run --rm -p 8000:8000 safebase_api

Endpoints:
- GET / -> Mensagem inicial
- GET /health -> Verifica saúde
- GET /items/{item_id}?q=... -> Exemplo de endpoint
