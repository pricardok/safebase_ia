# VendaComConexao

Uma API FastAPI para gerenciamento de vendas, simulações de conversas, integração com WhatsApp via WAHA, e orquestração de IA para otimização de vendas e tratamento de objeções.

## Descrição

Este projeto é uma aplicação backend desenvolvida em Python usando FastAPI, que oferece funcionalidades como:
- Simulação de conversas de vendas
- Análise de objeções e predições
- Integração com WhatsApp para comunicação automatizada
- Sistema de prompts personalizáveis
- Autenticação e autorização baseada em perfis de usuário
- Auditoria de ações
- Suporte a múltiplos provedores de IA (Google Generative AI, OpenAI, Azure OpenAI)

## Pré-requisitos

- **Python**: 3.9 ou superior (recomendado 3.11+)
- **Banco de dados**: PostgreSQL 12+
- **Sistema**: 
  - Local: Windows (desenvolvimento)
  - Produção: Linux (Railway/Vercel)
- **Dependências**: Ver seção de instalação

## Instalação

1. **Clone o repositório**:
   ```bash
   git clone https://github.com/pricardok/vendacomconexao.git
   cd vendacomconexao
   ```

2. **Crie um ambiente virtual**:
   ```bash
   # Windows
   python -m venv venv
   venv\Scripts\activate

   # Linux/Mac
   python -m venv venv
   source venv/bin/activate
   ```

3. **Instale as dependências**:
   ```bash
   pip install -r requirements.txt
   ```

4. **Configure o banco de dados**:
   - Instale e configure PostgreSQL
   - Configure as migrações do banco conforme necessário

## Configuração

Crie um arquivo `.env` na raiz do projeto com as seguintes variáveis de ambiente:

```env
# Banco de dados
DATABASE_URL=postgresql://user:password@localhost:5432/dbname

# Autenticação e Segurança
SECRET_KEY=your-secret-key-here
ALGORITHM=HS256
ACCESS_TOKEN_EXPIRE_MINUTES=30

# Integração WAHA (WhatsApp)
WAHA_API_KEY=your-waha-api-key
WAHA_BASE_URL=https://your-waha-instance.com
WAHA_WEBHOOK_SECRET=your-webhook-secret
WAHA_GATEWAY_NAME=waha
WAHA_SIGNATURE_HEADER=X-Waha-Signature
WAHA_LID_RESOLVE_URL=https://waha01.vendacomconexao.com
WEBHOOK_SYSTEM_USERNAME=webhook_bot
WAHA_SESSION=default
WAHA_SEND_URL=https://your-waha-send-url.com
WAHA_DISABLE_DEDUPE=false
WAHA_ALLOW_UNKNOWN_REPLY=false
WAHA_OUTBOUND_ALWAYS_SEND=false

# Provedores de IA
GOOGLE_API_KEY=your-google-api-key
OPENAI_API_KEY=your-openai-api-key
AZURE_OPENAI_API_KEY=your-azure-openai-key
AZURE_OPENAI_ENDPOINT=your-azure-endpoint
AZURE_OPENAI_DEPLOYMENT=your-deployment

# Configurações da Aplicação
AMBIENTE_ENV=development
API_LOGGING_ENABLED=false
```

**Nota**: Ajuste os valores conforme seu ambiente. Em produção, use variáveis de ambiente seguras.

## Rodando Localmente

1. **Ative o ambiente virtual** (se não estiver ativo).

2. **Execute a aplicação**:
   ```bash
   uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
   ```

3. **Acesse a documentação**:
   - Swagger UI: http://localhost:8000/docs
   - ReDoc: http://localhost:8000/redoc

## Rotas Principais 🚀
Abaixo está um resumo conciso das rotas mais relevantes organizadas por módulo. Use a documentação Swagger para ver parâmetros e exemplos completos.

- **Autenticação (auth)** 🔐
  - `POST /auth/login` — Obter token JWT (autenticação por e-mail/senha).
  - `GET /auth/me` — Informações do usuário autenticado.

- **Conexão (conexao)** 🔗
  - `POST /conexao/gerar-perguntas` — Gera perguntas abertas para criar conexão com o cliente.
  - `POST /conexao/analisar-resposta` — Analisa a resposta do vendedor e retorna pontuação e sugestões.
  - `POST /conexao/simular-dialogo` — Simula um diálogo vendedor↔cliente com análise breve.
  - `POST /conexao/roteiro-abordagem` — Gera um roteiro de abordagem em texto e JSON estruturado (novidade).

- **Simulador (simulador)** 🤖
  - `POST /simulador/responder` — Simula resposta de cliente com base em message/context.
  - `POST /simulador/feedback` — Gera feedback sobre mensagem de venda.

- **Quebra de Objeções (objecoes)** 🛡️
  - `POST /objecoes/quebrar` — Gera respostas para objeções do cliente em diferentes abordagens.

- **Detector de Vendedor (detector)** 🔍
  - `POST /detector/analisar` — Analisa tom e características de mensagens (vendedor chato etc.).

- **Scripts (scripts)** ✍️
  - `POST /scripts/otimizar` — Otimiza um script de vendas para canal/objetivo.
  - `POST /scripts/analisar-eficacia` — Avalia eficácia do script original.
  - `POST /scripts/gerar-variacoes` — Gera variações do script base.

- **Análises Cognitivas (analise)** 🧠
  - `POST /analise/conversa` — Análises de conversas completas.
  - `POST /analise/probabilidade-conversao` — Estima probabilidade de conversão.
  - `POST /analise/contexto-alinhamento` — Verifica alinhamento contextual da conversa.
  - `POST /analise/predicao-objecoes` — Prevê possíveis objeções.
  - `POST /analise/mudanca-emocional` — Detecta mudanças emocionais na conversa.

- **Histórico (historico)** 📚
  - `POST /historico/simulacao` — Salva simulações (usado por outros módulos).
  - `GET /historico/simulacoes` — Lista simulações do usuário.
  - `GET /historico/sessoes` — Agrupa interações por session_id.
  - `GET /historico/perfil-comportamental` — Retorna o perfil comportamental do usuário.

- **Prompts (prompts)** 📝
  - `GET /prompts/{prompt_name}` — Informação e parâmetros de um prompt.
  - `POST /prompts/{prompt_name}/render` — Renderiza o template do prompt com parâmetros.

- **Administração (admin)** ⚙️
  - Permite gerenciamento de usuários, perfis, módulos, planos, IA (provedores e chaves), logs e auditoria.

> Observação: cada rota tem controle de acesso por perfil (ver tabela `perfil_modulos`). Se precisar que um recurso seja um módulo separado (ex.: `roteiro_abordagem`), podemos criar a entrada em `modulos` e adicionar permissões em `perfil_modulos`.

---

(Para detalhes de payloads e exemplos, use a Swagger UI: `http://localhost:8000/docs`.)

## Testes

Execute os testes com pytest:

```bash
pytest
```

Ou para testes específicos (se disponíveis localmente):
```bash
pytest path/to/test_file.py
```

## Deploy

### Railway (Produção Linux)

1. Conecte seu repositório ao Railway.
2. Configure as variáveis de ambiente no painel do Railway.
3. O deploy é automático via `railway.json`.

### Vercel (Opcional para funções serverless)

1. Configure o Vercel para Python.
2. Use `vercel.json` para configurações.
3. Deploy via CLI ou painel.

**Nota**: Em produção Linux, certifique-se de que todas as dependências sejam compatíveis e que o banco de dados esteja acessível.

### Configurações de Produção

- **URL da API**: https://core.vendacomconexao.com/
- **Banco de Dados**: PostgreSQL hospedado no IP `3.23.64.169` (porta 5432)
- **Variável DATABASE_URL**: Configure com o IP de produção para acesso remoto seguro

## Estrutura do Projeto

```
.
├── app/
│   ├── main.py              # Ponto de entrada da aplicação
│   ├── config.py            # Configurações globais
│   ├── database.py          # Conexões e funções do banco
│   ├── models.py            # Modelos Pydantic
│   ├── auth.py              # Autenticação JWT
│   ├── routes/              # Endpoints da API
│   ├── services/            # Lógica de negócio
│   ├── prompts/             # Sistema de prompts
│   └── utils/               # Utilitários
├── requirements.txt         # Dependências Python
├── .gitignore               # Arquivos ignorados
├── railway.json             # Configuração Railway
├── vercel.json              # Configuração Vercel
└── README.md                # Este arquivo
```

## Análise de Requirements.txt

O `requirements.txt` atual contém versões que podem estar desatualizadas. Recomenda-se atualizar para as versões mais recentes para segurança e compatibilidade:

- `fastapi==0.104.1` → `fastapi>=0.110.0`
- `pydantic==2.5.0` → `pydantic>=2.7.0`
- `openai==1.6.1` → `openai>=1.40.0`
- `google-generativeai==0.3.0` → `google-generativeai>=0.8.0`
- `uvicorn==0.29.0` → `uvicorn>=0.30.0`
- Outras bibliotecas também podem ter updates disponíveis.

**Atenção**: Antes de atualizar, teste a compatibilidade, especialmente com `protobuf==3.20.3` que foi adicionado para compatibilidade com Python 3.13.

Para atualizar:
```bash
pip install --upgrade -r requirements.txt
# Ou edite manualmente e reinstale
```

## Notas sobre Windows (Local) vs Linux (Produção)

- **Caminhos**: Use barras invertidas (`\`) no Windows e barras (`/`) no Linux para caminhos.
- **Scripts**: Scripts PowerShell (`.ps1`) funcionam no Windows; use Bash (`.sh`) no Linux.
- **Dependências**: Algumas bibliotecas podem ter diferenças de instalação (ex.: `psycopg2-binary` vs `psycopg2`).
- **Variáveis de ambiente**: Certifique-se de que as vars sejam definidas corretamente em ambos os SOs.
- **Banco de dados**: PostgreSQL deve estar rodando localmente no Windows; em produção, use serviços como Railway DB.

## Contribuição

1. Fork o projeto.
2. Crie uma branch para sua feature (`git checkout -b feature/nova-feature`).
3. Commit suas mudanças (`git commit -am 'Adiciona nova feature'`).
4. Push para a branch (`git push origin feature/nova-feature`).
5. Abra um Pull Request.

## Licença

Este projeto é privado. Consulte o proprietário para permissões.

## Suporte

Para dúvidas ou problemas, abra uma issue no repositório ou contate o desenvolvedor.