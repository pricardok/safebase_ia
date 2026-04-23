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

3. Rodar o servidor:
   uvicorn app.main:app --reload --host 0.0.0.0 --port 8000

Endpoints:
- GET / -> Mensagem inicial
- GET /health -> Verifica saúde
- GET /items/{item_id}?q=... -> Exemplo de endpoint
