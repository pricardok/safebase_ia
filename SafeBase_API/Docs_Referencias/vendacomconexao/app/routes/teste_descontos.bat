@echo off
setlocal enabledelayedexpansion

set API_URL=http://localhost:8000
set JWT_TOKEN=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJwYXVsb2tfa3Vobl81YzNmIiwic2FsdCI6IjkzNTA5NjBhNTljNzBlMmJkYjliMTc0OWYxOTEyZmYyIiwiZXhwIjoxNzYzNjUyMTM1fQ.-wldtZ0ixpAkcze9s2a8AiT22H7Ab1g8tMLa6Ef6Tko

:: Substitua pelo ID de um cliente existente no seu banco de dados para o teste de aplicação
set CLIENTE_ID_TESTE=f47ac10b-58cc-4372-a567-0e02b2c3d479

echo.
echo --- INICIANDO TESTES DO MODULO DE DESCONTOS ---
echo.

echo Teste 1/5: Criando um novo cupom de desconto...
for /f "tokens=*" %%i in ('curl -s -X POST "%API_URL%/admin/descontos" -H "Authorization: Bearer %JWT_TOKEN%" -H "Content-Type: application/json" -d "{\"codigo\": \"INVERNO30\", \"tipo\": \"PERCENTUAL\", \"valor\": 30, \"max_usos\": 50, \"ativo\": true}"') do (
    set RESPONSE=%%i
)
echo !RESPONSE!
echo.

:: Extrai o ID do cupom criado para usar nos próximos testes
for /f "tokens=2 delims=:, " %%j in ('echo !RESPONSE!') do set CUPOM_ID=%%j

echo.
echo Teste 2/5: Listando todos os descontos...
curl -X GET "%API_URL%/admin/descontos" -H "Authorization: Bearer %JWT_TOKEN%"
echo.
echo.

echo Teste 3/5: Editando o cupom criado (ID: %CUPOM_ID%)...
curl -X PUT "%API_URL%/admin/descontos/%CUPOM_ID%" -H "Authorization: Bearer %JWT_TOKEN%" -H "Content-Type: application/json" -d "{\"codigo\": \"INVERNO35\", \"tipo\": \"PERCENTUAL\", \"valor\": 35, \"max_usos\": 100, \"ativo\": false}"
echo.
echo.

echo Teste 4/5: Aplicando o cupom ao cliente (ID Cliente: %CLIENTE_ID_TESTE%)...
curl -X POST "%API_URL%/admin/clientes/%CLIENTE_ID_TESTE%/descontos" -H "Authorization: Bearer %JWT_TOKEN%" -H "Content-Type: application/json" -d "{\"codigo_cupom\": \"INVERNO35\"}"
echo.
echo.

echo Teste 5/5: Deletando o cupom criado (ID: %CUPOM_ID%)...
curl -X DELETE "%API_URL%/admin/descontos/%CUPOM_ID%" -H "Authorization: Bearer %JWT_TOKEN%"
echo.
echo.

echo --- TESTES FINALIZADOS ---
pause