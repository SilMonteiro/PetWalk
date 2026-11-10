# PetWalk

Plataforma Web para intermediação de passeios com pets.

---

## Integrantes

- GUILHERME INÁCIO DOS SANTOS MOREIRA - Matrícula: 06014499
- DANIEL MORAES DELGADO - Matrícula: 06014751
- RAFAELLA ALVES GUERRA - Matrícula: 06015406
- SARAH LETÍCIA DOMINGUES DOS SANTOS - Matrícula: 06014748
- SILVIA MARIA ALBUQUERQUE MONTEIRO - Matrícula: 06011524

---

## Requisitos

- **Node.js** 18 ou superior e **npm** para o frontend.
- **.NET SDK 10** para a API. O SDK inclui o runtime necessário para executar o projeto.

Confira se as ferramentas estão instaladas:

```bash
node --version
npm --version
dotnet --version
```

## Como Executar

Clone o repositório e abra um terminal na raiz do projeto. O frontend e a API devem ser executados em **terminais separados**.

### 1. Iniciar a API

No primeiro terminal:

```bash
cd backend/Projeto.Api
dotnet restore
dotnet run --launch-profile http
```

A API ficará disponível em `http://localhost:5229`.
Para confirmar que está respondendo, acesse o documento OpenAPI em `http://localhost:5229/openapi/v1.json`
(ou use as requisições de `backend/Projeto.Api/Projeto.Api.http`).

### Executar os testes do backend

```bash
cd backend
dotnet test
```

### 2. Iniciar o frontend

No segundo terminal, a partir da raiz do repositório:

```bash
cd frontend
npm install
npm run dev
```

Abra `http://localhost:3000` no navegador. Mantenha os dois terminais abertos enquanto estiver usando o projeto.

#### Configuração da API no frontend

O frontend lê a URL da API da variável `VITE_API_URL`. O modelo versionado é `frontend/.env.example`:

```bash
cd frontend
cp .env.example .env
```

```env
VITE_API_URL=http://localhost:5229
```

- `frontend/.env` não é versionado (está no `.gitignore`).
- Sem `.env`, `src/services/api.js` usa `http://localhost:5229` como valor padrão.
- A API habilita CORS apenas para `http://localhost:3000`, a porta padrão do Vite.

### Encerrar os servidores

Em cada terminal, pressione `Ctrl+C`.

## Rotas e Telas Disponíveis

Rotas protegidas redirecionam para o login quando não há usuário na sessão.

| Rota | Tela |
| :-- | :-- |
| `/` | Login de usuário real da API |
| `/cadastro` | Cadastro de tutor, passeador ou ambos |
| `/home` | Home com cards conforme o `tipoPerfil` |
| `/pets` | Meus pets (listar e excluir) |
| `/pets/novo` | Cadastro de pet |
| `/pets/:id/editar` | Edição de pet |
| `/passeadores` | Passeadores ativos (tutor) |
| `/passeios/novo?passeadorId=ID` | Solicitação de passeio (tutor) |
| `/passeios` | Meus passeios, ações por papel/status e avaliação do passeador |
| `/notificacoes` | Notificações do usuário logado |

## Estrutura do Projeto

```
PetWalk
├── backend
│   ├── PetWalk.slnx         # Solution (API + testes)
│   ├── Projeto.Api
│   │   ├── Contratos        # DTOs de entrada/saída da API
│   │   ├── Dominio          # Entidades e regras de negócio
│   │   ├── Endpoints        # Rotas HTTP (finas, delegam ao domínio)
│   │   ├── Repositorios     # Armazenamento (atrás de interfaces)
│   │   ├── Servicos         # Autenticação e notificações
│   │   └── Properties
│   └── Projeto.Tests        # Testes xUnit das regras do domínio
├── docs
├── frontend
│   ├── .env.example         # Modelo de VITE_API_URL
│   ├── index.html           # Ponto de entrada HTML do Vite
│   ├── package.json         # Dependências do projeto frontend
│   ├── vite.config.js       # Configuração do Vite
│   └── src
│       ├── App.jsx          # Configuração de rotas e providers
│       ├── main.jsx         # Renderização principal do React
│       ├── components
│       │   ├── AvaliacaoForm.jsx
│       │   ├── Header.jsx
│       │   ├── PasseioCard.jsx
│       │   ├── PetCard.jsx
│       │   └── RotaProtegida.jsx
│       ├── constants
│       │   └── enums.js
│       ├── context
│       │   ├── AuthContext.jsx
│       │   └── NotificacoesContext.jsx
│       ├── pages
│       │   ├── CadastroPage.jsx
│       │   ├── HomePage.jsx
│       │   ├── LoginPage.jsx
│       │   ├── NotificacoesPage.jsx
│       │   ├── PasseadoresPage.jsx
│       │   ├── PasseiosPage.jsx
│       │   ├── PetFormPage.jsx
│       │   ├── PetsPage.jsx
│       │   └── SolicitarPasseioPage.jsx
│       ├── services
│       │   ├── api.js
│       │   ├── notificacaoService.js
│       │   ├── passeadorService.js
│       │   ├── passeioService.js
│       │   ├── petService.js
│       │   └── usuarioService.js
│       ├── utils
│       │   └── passeioAcoes.js
│       └── styles
│           ├── cadastro.css
│           ├── header.css
│           ├── home.css
│           ├── login.css
│           ├── notificacoes.css
│           ├── passeadores.css
│           ├── passeios.css
│           ├── pet-form.css
│           ├── pets.css
│           └── solicitar-passeio.css
├── .gitignore
└── README.md
```
