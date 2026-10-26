# PetWalk

Plataforma Web para intermediação de passeios com pets.

---

## Integrante

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
Para confirmar que está respondendo, acesse `http://localhost:5229/teste`.


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

### Encerrar os servidores

Em cada terminal, pressione `Ctrl+C`.

## Rotas e Telas Disponíveis

- **Login:** [http://localhost:3000/](http://localhost:3000/)
  - *Credenciais de teste (Mock):* **`admin@petwalk.com`** / **`123456`**
- **Home (Página Inicial):** [http://localhost:3000/home](http://localhost:3000/home)
- **Cadastro de Pets:** [http://localhost:3000/registro-pet](http://localhost:3000/registro-pet)


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
│   │   └── Properties
│   └── Projeto.Tests        # Testes xUnit das regras do domínio
├── docs
├── frontend
│   ├── index.html           # Ponto de entrada HTML do Vite
│   ├── package.json         # Dependências do projeto frontend
│   ├── vite.config.js       # Configuração do Vite
│   └── src
│       ├── App.jsx          # Configuração de rotas
│       ├── main.jsx         # Renderização principal do React
│       ├── components
│       │   ├── LoginScreen.jsx
│       │   ├── HomeScreen.jsx
│       │   ├── RegisterScreen.jsx
│       │   └── PetRegistrationScreen.jsx
│       └── styles
│           ├── login.css
│           ├── home.css
│           └── pet-registration.css
├── .gitignore
└── README.md
```
