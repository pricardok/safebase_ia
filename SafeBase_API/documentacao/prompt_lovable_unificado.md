# SafeBase Frontend (Lovable) — Contrato + UI/UX Unificado

Este documento unifica o **contrato da API** e o **briefing de UI/UX** para o Lovable gerar o front-end completo.

---

## 1) Visão geral
- API: `SafeBase_API`
- Documentação OpenAPI:
  - `GET /openapi.json`
  - `GET /docs`
  - `GET /redoc`
- Base URL (local): `http://localhost:8000`

---

## 2) Autenticação e sessão
A autenticação é feita **via JWT** (login). Não existe mais usuário fixo no código.

### 2.0 Variáveis de ambiente do front
Criar arquivo `.env` na raiz do front:
```
VITE_API_URL=http://localhost:8000
VITE_DEFAULT_AGENT_ID=chat
```

### 2.1 Fluxo
1. `POST /auth/login` → retorna `access_token`
2. Front salva o token (session/local storage)
3. Todas as rotas protegidas usam:
   - `Authorization: Bearer <token>`

### 2.2 Expiração de sessão
- Controlada por `ACCESS_TOKEN_EXPIRE_MINUTES` no `.env`
- Ex: `ACCESS_TOKEN_EXPIRE_MINUTES=60`

### 2.3 Roles e permissões
O JWT retorna `roles` e `permissions`.

### 2.3.1 Categorias liberadas
O usuário também possui **categorias liberadas** (ex.: `dba`, `seguros`, `bi`).
Essas categorias controlam quais dados o chat pode acessar.

**Regras do front:**
- Se `roles` contém `admin` → exibir **menu de administração**
- Caso contrário → mostrar apenas o **chat** (estilo DeepSeek)

### 2.4 Endpoint para perfil logado
`GET /auth/me`
- Retorna dados do usuário + roles + permissions
- Usado pelo front para montar menu e permissões

---

## 3) Endpoints principais

### 3.1 `POST /auth/login`
**Body**
```json
{
  "username": "admin (ou email)",
  "password": "<SENHA>"
}
```
**Resposta**
```json
{
  "access_token": "<jwt-token>",
  "token_type": "bearer",
  "roles": ["admin"],
  "permissions": ["rbac.read", "rbac.manage", "users.read", "users.manage"]
}
```

### 3.2 `GET /auth/me`
**Header**
```
Authorization: Bearer <token>
```
**Resposta**
```json
{
  "id": 1,
  "username": "paulo.kuhn",
  "email": "paulo.kuhn@facta.com.br",
  "full_name": "Paulo Kuhn",
  "is_active": true,
  "roles": ["admin"],
  "permissions": ["rbac.manage", "rbac.read", "users.manage", "users.read"],
  "categorias": ["dba", "seguros"]
}
```

### 3.3 `POST /ia/query`
**Header**
```
Authorization: Bearer <token>
ou
X-API-Key: <key>
```
**Body**
```json
{
  "query": "Quais os top waits nas ultimas 24h?",
  "context": {"origin": "chat"},
  "agent_id": "chat",
  "categoria_codigo": "dba"
}
```

### 3.4 Chat
**Criar conversa**
```
POST /chat/conversas
Authorization: Bearer <token>
ou
X-API-Key: <key>
```
Body:
```json
{ "titulo": "Analise de performance", "categoria_codigo": "dba" }
```

**Enviar mensagem**
```
POST /chat/conversas/{id}/mensagens
Authorization: Bearer <token>
ou
X-API-Key: <key>
```
Body:
```json
{ "conteudo": "Analise o ambiente" }
```

**Excluir conversa (somente do próprio usuário)**
```
DELETE /chat/conversas/{id}
Authorization: Bearer <token>
ou
X-API-Key: <key>
```

---

## 4) UI/UX — Layout estilo DeepSeek

### Estrutura Geral
- Layout dividido em duas colunas: sidebar esquerda (histórico/conversas) e área principal do chat
- Sidebar com largura de 260px, fundo escuro (#0d0f12) e borda direita sutil
- Área principal com fundo (#1a1c21) e conteúdo centralizado

### Sidebar Esquerda
- Botão "Novo Chat" no topo com ícone de "+" e fundo azul (#3b82f6)
- Lista de conversas históricas com título, preview da mensagem e data
- Cada conversa tem ícone de chat, hover com fundo mais claro
- Área inferior com nome do usuário, avatar e opções (configurações, tema, logout)

### Área Principal do Chat
- Topo: título da conversa atual e opções (compartilhar, código, mais)
- Container de mensagens rolável (viewport entre cabeçalho e input)
- Mensagens do usuário: alinhadas à direita, fundo (#2a2e35), cantos arredondados
- Mensagens do assistente: alinhadas à esquerda, fundo (#25282e), sem avatar visual
- Suporte a markdown básico (negrito, itálico, blocos de código com highlight)
- Blocos de código com fundo preto (#0a0a0a), barra de título e botão de copiar

### Input de Mensagem
- Área fixa na parte inferior, fundo (#1e2128)
- Campo de texto multi-linha com altura dinâmica (min 40px, max 200px)
- Placeholder: "Mande uma mensagem..."
- Botão de enviar azul (#3b82f6) ao lado direito
- Ícone de anexo (opcional) e sugestões rápidas acima do input

### Estilos e Temas
- Dark theme como padrão
- Bordas arredondadas (12px cards, 20px mensagens)
- Sombra suave nos elementos flutuantes
- Animações suaves (hover, foco)
- Scrollbar customizada (fina e escura)
- Font-family: system-ui, -apple-system, sans-serif

### Componentes Funcionais (JS)
- Simular envio de mensagens com atraso de resposta
- Criar nova conversa ao clicar em "Novo Chat"
- Alternar entre conversas do histórico (simulação)
- Botão de copiar código funcionando com Clipboard API
- Responsividade: em mobile, sidebar oculta com botão hambúrguer

### Stack Técnica
- HTML5 semântico
- TailwindCSS via CDN
- JavaScript vanilla
- Ícones: Lucide ou FontAwesome (CDN)
- Syntax highlight: highlight.js (opcional)

---

## 5) Regras do menu (admin vs usuário comum)
- Se o usuário logado possui role `admin`, o front deve exibir **menu de administração**
- Usuários sem `admin` devem ver apenas a UI de chat
- A área de administração deve permitir **atribuir categorias** aos usuários (ex.: `dba`, `bi`, `seguros`)

---

## 6) Observações finais
- Sempre usar JWT para autenticação no front
- Tratar erros 401/403/422 no front
- O layout deve ser fiel ao estilo minimalista e funcional do DeepSeek
