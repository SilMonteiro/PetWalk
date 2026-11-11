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
- **dotnet-ef 10.0.11** como ferramenta local, restaurada pelo manifesto `.config/dotnet-tools.json` com `dotnet tool restore`.

Confira se as ferramentas estão instaladas:

```bash
node --version
npm --version
dotnet --version
```

## Banco de dados (SQLite)

A API usa SQLite em arquivo, via Entity Framework Core. O banco padrão fica em:

```
backend/Projeto.Api/App_Data/petwalk.db
```

O diretório `App_Data/` é ignorado pelo git — o banco não é versionado.

O schema do banco da API é criado **somente por migrations**. A API não cria nem altera o schema ao iniciar: não há `EnsureCreated` nem `Migrate` no startup. Se o arquivo não existir, estiver com migrations pendentes ou divergente do modelo, a API interrompe a inicialização antes de atender requisições e informa a ação necessária.

### Primeira execução

A partir da raiz do repositório, restaure a ferramenta local:

```bash
dotnet tool restore
```

Depois, na pasta da API, restaure as dependências e crie o banco aplicando as migrations:

```bash
cd backend/Projeto.Api
dotnet restore
dotnet ef database update
```

A migration inicial (`20261111012746_Inicial`) cria as tabelas `Usuarios`, `Pets`, `Passeios`, `Avaliacoes` e `Notificacoes`. Não há seed: o banco começa vazio, e o primeiro usuário pode ser cadastrado pela tela `/cadastro`.

### Recriar o banco

Com a API parada, apague o arquivo `backend/Projeto.Api/App_Data/petwalk.db` (e os arquivos `-wal` e `-shm`, se existirem) e execute novamente:

```bash
cd backend/Projeto.Api
dotnet ef database update
```

Os testes não usam esse arquivo: as fábricas de teste usam SQLite em memória ou um arquivo temporário próprio.

## Como Executar

Clone o repositório e abra um terminal na raiz do projeto. O frontend e a API devem ser executados em **terminais separados**. Na primeira execução, aplique as migrations conforme a seção anterior.

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

### 2. Iniciar o frontend

No segundo terminal, a partir da raiz do repositório:

```bash
cd frontend
npm install
npm run dev
```

Abra `http://localhost:3000` no navegador. Mantenha os dois terminais abertos enquanto estiver usando o projeto.

#### Configuração da API no frontend

O frontend lê a URL da API da variável `VITE_API_URL`. O modelo versionado é `frontend/.env.example`.

A partir da raiz do repositório:

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

### Build do frontend

Após instalar as dependências, a partir da raiz do repositório:

```bash
cd frontend
npm run build
```

Para servir o build gerado em `dist/`:

```bash
npm run serve
```

### Encerrar os servidores

Em cada terminal, pressione `Ctrl+C`.

## Testes

Os testes do backend usam xUnit. A suíte tem **291 casos** (253 `[Fact]` e 38 casos de `[Theory]`) em **33 classes**, cobrindo domínio, endpoints, persistência EF/SQLite, integração HTTP, schema de inicialização e concorrência.

Para executar toda a suíte, a partir da raiz do repositório:

```bash
cd backend
dotnet test
```

Para executar uma classe específica, a partir da raiz do repositório:

```bash
dotnet test backend/Projeto.Tests/Projeto.Tests.csproj --filter "FullyQualifiedName~LoginEndpointsTests"
```

Para compilar o backend, a partir da raiz do repositório:

```bash
dotnet build backend/PetWalk.slnx
```

### Limitação do ambiente Windows

**120 dos 291 casos não foram executados neste ambiente.** Eles dependem do SQLite nativo e falham no carregamento de `SQLitePCLRaw.batteries_v2.dll` com `System.IO.FileLoadException` (`0x800711C7`), por bloqueio do Windows App Control. A falha ocorre na inicialização do teste, antes de qualquer asserção.

Esses 120 casos **não estão aprovados**. Na última execução local registrada: 171 casos aprovados e 120 não executados. A homologação dos casos de persistência, schema e concorrência depende de CI ou de uma máquina sem essa política de App Control.

## Limitações conhecidas

- **Autenticação:** não há token, JWT ou sessão de servidor. `POST /login` apenas valida as credenciais e devolve o perfil do usuário; a API não emite credencial para as requisições seguintes.
- **Autorização:** as rotas da API não exigem autenticação. As regras de perfil, status e posse são validadas no domínio a partir dos IDs informados pelo cliente (`usuarioId` no corpo ou `id` na URL).
- **Proteção de rotas no frontend:** o `AuthContext` guarda o usuário no `localStorage` e o componente `RotaProtegida` redireciona para o login quando não há usuário. Essa proteção é apenas de navegação; não substitui autenticação na API.
- **Rate limiting:** não há limite de tentativas de login.
- **Concorrência:** apenas `Passeio.Status` é configurado como token de concorrência. Alterações concorrentes em `Usuario` e `Pet` seguem last-write-wins.
- **Banco de dados:** SQLite local em arquivo único; não é um banco compartilhado entre máquinas.
- **Testes do frontend:** não há testes automatizados nem script de lint no frontend. A validação disponível é `npm run build` e o uso manual das telas.
- **CI:** não há workflow de integração contínua configurado.

## Rotas e Telas Disponíveis

No frontend, rotas protegidas redirecionam para o login quando não há usuário na sessão.

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
├── .config
│   └── dotnet-tools.json        # Manifesto da ferramenta local dotnet-ef
├── backend
│   ├── PetWalk.slnx             # Solution (API + testes)
│   ├── Projeto.Api
│   │   ├── App_Data             # Banco SQLite local (não versionado)
│   │   ├── Contratos            # DTOs de entrada/saída da API
│   │   ├── Dados
│   │   │   ├── Configuracoes    # Mapeamento EF Core das entidades
│   │   │   ├── Migracoes        # Migrations do EF Core
│   │   │   ├── PetWalkDbContext.cs
│   │   │   └── VerificacaoDeSchema.cs
│   │   ├── Dominio              # Entidades e regras de negócio
│   │   ├── Endpoints            # Rotas HTTP (finas, delegam ao domínio)
│   │   ├── Repositorios         # Interfaces, implementações EF e versões em memória
│   │   ├── Servicos             # Autenticação e notificações
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── Projeto.Api.csproj
│   │   ├── Projeto.Api.http     # Roteiro de requisições manuais
│   │   ├── Program.cs
│   │   └── Properties
│   └── Projeto.Tests            # Testes de domínio, endpoints, persistência, HTTP e concorrência
├── docs
│   ├── plano-av2.md
│   └── proposta.md
├── frontend
│   ├── .env.example             # Modelo de VITE_API_URL
│   ├── index.html               # Ponto de entrada HTML do Vite
│   ├── package.json             # Dependências do projeto frontend
│   ├── package-lock.json        # Lockfile do npm
│   ├── vite.config.js           # Configuração do Vite
│   └── src
│       ├── App.jsx              # Configuração de rotas e providers
│       ├── main.jsx             # Renderização principal do React
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
