# PetWalk

Plataforma Web para intermediação de passeios com pets.

---

## 📋 Requisitos para execução do projeto

- **Node.js** >= 18 (versão LTS recomendada).
- **npm** (instalado junto com o Node.js).

---

## 🚀 Como Executar o Projeto

No terminal, estando na raiz do projeto:

```bash
# 1. Entrar na pasta do frontend
cd frontend

# 2. Instalar as dependências
npm install

# 3. Iniciar o servidor de desenvolvimento
npm run dev
```

> **Nota para usuários Windows/PowerShell:** Se houver bloqueio de script pelo PowerShell, utilize `npm.cmd run dev` ou execute `Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned`.

O terminal exibirá a URL local do servidor (por padrão: `http://localhost:3000/`).

---

## 🔗 Rotas e Telas Disponíveis

- **Login:** [http://localhost:3000/](http://localhost:3000/)
  - *Credenciais de teste (Mock):* **`admin@petwalk.com`** / **`123456`**
- **Home (Página Inicial):** [http://localhost:3000/home](http://localhost:3000/home)
- **Cadastro de Pets:** [http://localhost:3000/registro-pet](http://localhost:3000/registro-pet)

---

## 🛠️ Tecnologias Utilizadas

- **React** (v18)
- **Vite**
- **React Router DOM** (v6)
- **CSS3**

---

## 📁 Estrutura do Projeto

```
PetWalk
├── backend
│   └── Projeto.Api
│       └── Dominio/         # Modelos de domínio em C#
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
│       │   └── PetRegistrationScreen.jsx
│       └── styles
│           ├── login.css
│           ├── home.css
│           └── pet-registration.css
├── .gitignore
└── README.md
```
