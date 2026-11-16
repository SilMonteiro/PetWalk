# Especificação para agente de código — PetWalk (AV2)

Este documento registra, para a Etapa 7 do `docs/plano-av2.md` (item G02), o **contexto** e o **pedido**
entregues a um agente de código para executar uma alteração funcional real no PetWalk. A ordem das seções
segue a ordem real do trabalho: primeiro o que o agente recebeu, depois o que foi pedido — o plano, o diff e
a inspeção vêm depois da execução e serão registrados nas seções seguintes quando existirem. Nesta data
(15/11/2026) apenas contexto e pedido estão escritos; nada de resultado é afirmado aqui.

| Campo | Valor |
| :-- | :-- |
| Ferramenta | DsCode (CLI de agente de código) |
| Modelo | `deepseek-flash` (confirmado em `.dscode/settings.json`) |
| Versão do DsCode | **não verificada** — `.dscode/` não contém arquivo de versão |
| Data | 15/11/2026 |
| Branch de trabalho | `feature/d03-falha-inesperada` |
| Commit base | `bae1c756c94e20e5948d03ab9ccfe2656aea6493` |
| Alteração pedida | D03 — tratamento central de falhas inesperadas |

O estado de partida foi medido antes de qualquer escrita: `dotnet test` na suíte completa deu **291 casos,
289 aprovados, 2 falhas pré-existentes em `VerificacaoDeSchemaTests`, 0 ignorados**; a suíte filtrada em
`EndpointsFalhaDePersistenciaTests` deu 6/6. O agente deve tratar esses números como linha de base, nunca
como "suíte verde".

## Contexto fornecido ao agente

### Stack e versões (verificadas)

| Item | Verificação |
| :-- | :-- |
| .NET 10 | `dotnet --version` → `10.0.400`; `Projeto.Tests.csproj` com `<TargetFramework>net10.0</TargetFramework>` |
| xUnit | `Projeto.Tests.csproj`: `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.4, `Microsoft.NET.Test.Sdk` 17.14.1, `Microsoft.AspNetCore.Mvc.Testing` 10.0.11 |
| SQLite via EF Core | `Program.cs:23-41` (`SqliteConnectionStringBuilder`, `UseSqlite`, `AddDbContext`); schema criado só por migrations (`Program.cs:65-71`) |
| React/Vite | `frontend/package.json`: `react` ^18.2.0, `react-dom` ^18.2.0, `vite` ^6.4.6 |

### Estrutura de pastas relevante (verificada)

`backend/Projeto.Api/` contém `Contratos/`, `Dados/`, `Dominio/`, `Endpoints/`, `Repositorios/`, `Servicos/`;
`backend/Projeto.Tests/` contém **39 arquivos `.cs`** (classes de domínio, endpoints, persistência, HTTP e
concorrência). O agente trabalha apenas em `Endpoints/` (produção) e em `Projeto.Tests/` (teste).

### Convenções reais do projeto (confirmadas por leitura/grep)

| Convenção | Evidência |
| :-- | :-- |
| Setters privados nas entidades | `Dominio/Avaliacao.cs:3-6`; `Dominio/Notificacao.cs:3-8`; `Dominio/Passeio.cs:5-8`; `Dominio/Usuario.cs:11` (`TipoPerfil { get; private set; }`) |
| "Agora" injetado, nunca lido do relógio do sistema | `Program.cs:61` (`AddSingleton(TimeProvider.System)`); `Endpoints/PasseioEndpoints.cs:11,20,27,34,41,48` (`time.GetUtcNow().UtcDateTime`) |
| Mensagens de erro literais, vindas do domínio | `Dominio/Avaliacao.cs:23` ("Nota deve estar entre 1 e 5"); `Dominio/Passeio.cs:96` ("Passeio não pode ser cancelado no estado atual."); `Dominio/Passeio.cs:110` e `Endpoints/ErroDePersistencia.cs:67` ("Passeio já avaliado por este usuário"); `Servicos/AutenticacaoServico.cs:32` e `Endpoints/ErroDePersistencia.cs:60` ("E-mail já cadastrado"); `Endpoints/ErroDePersistencia.cs:83` ("Pet possui passeios e não pode ser excluído") |
| Contrato de erro `{ "erro": "<mensagem>" }` | `Endpoints/ErroHttp.cs:9` (comentário), `:18` (400), `:19` (422), `:24` (404); o frontend lê exatamente esse campo em `frontend/src/services/api.js:12` (`dados.erro || \`Erro ${resposta.status}\``) |
| 400 para `ArgumentException`, 422 para `InvalidOperationException` | `Endpoints/ErroHttp.cs:18-19`; exceções não previstas propagam (`:20`) |
| 500 para o inesperado | `Program.cs:57` (`AddExceptionHandler<ErroDePersistenciaHandler>()`), `:58` (`AddProblemDetails()`), `:75` (`app.UseExceptionHandler()`) |
| O projeto **não** registra log nem escreve "Erro interno" | `grep -rn "ILogger\|LogError\|Erro interno" backend --include=*.cs` → **0 linhas** (`exit=1`) |
| Falha de persistência simulada nos testes | `Projeto.Tests/UnidadeDeTrabalhoQueFalha.cs:10` lança `InvalidOperationException("falha de persistência simulada")` |

### Arquivos que o agente deve ler antes de agir

1. `backend/Projeto.Api/Program.cs`
2. `backend/Projeto.Api/Endpoints/ErroHttp.cs`
3. `backend/Projeto.Api/Endpoints/ErroDePersistenciaHandler.cs`
4. `backend/Projeto.Api/Endpoints/ErroDePersistencia.cs`
5. `backend/Projeto.Tests/PetWalkApiFactory.cs`
6. `backend/Projeto.Tests/EndpointsFalhaDePersistenciaTests.cs`
7. `docs/av2/especificacao.md`, seção **Falhas** (linhas 526-540)

## Pedido delimitado

### Alteração pedida

Implementar o tratamento central de falhas inesperadas (item **D03** do plano): criar um `IExceptionHandler`
chamado `FalhaInesperadaHandler`, registrado **depois** do `ErroDePersistenciaHandler`, que:

1. registra a exceção com `ILogger`, incluindo o método HTTP e o caminho da requisição;
2. responde **500** com o corpo `{ "erro": "Erro interno. Tente novamente mais tarde." }`;
3. **nunca** expõe `ex.Message` nem stack trace ao cliente;
4. retorna `true` (a exceção é considerada tratada).

O detalhe técnico da exceção fica apenas no log. O corpo é o mesmo contrato `{ "erro": ... }` que o frontend
já lê (`frontend/src/services/api.js:12`).

### Motivação rastreável

- `docs/av2/especificacao.md`, seção **Falhas** (linha 528): o contrato separa 400 (entrada inválida), 422
  (regra violada) e 500 (falha excepcional).
- A mesma seção, linha 534, registra que, no caso 500, **"Não é o contrato `{ \"erro\": ... }`"** e que o
  formato do corpo é **"não verificado"**.
- A mesma seção, linha 538, registra que o corpo `{ "erro": "Erro interno. Tente novamente mais tarde." }`
  **"não existe no código"** e que **não há logging explícito** (`ILogger`, `LogError` ou equivalente).
- `docs/plano-av2.md`, Etapa 4 (D03/D04), pede exatamente o handler central que registra a exceção e devolve
  mensagem genérica, e testes para as três situações de falha.

### Arquivos PERMITIDOS

| Arquivo | Situação |
| :-- | :-- |
| `backend/Projeto.Api/Endpoints/FalhaInesperadaHandler.cs` | novo |
| `backend/Projeto.Api/Program.cs` | somente o registro do handler e o comentário correspondente |
| `backend/Projeto.Tests/FalhaInesperadaTests.cs` | novo |

Nenhum outro arquivo pode ser criado ou editado. **Nenhum pacote NuGet novo.**

### Arquivos e comportamentos que NÃO PODEM mudar

- `backend/Projeto.Api/Endpoints/ErroHttp.cs`, `ErroDePersistenciaHandler.cs`, `ErroDePersistencia.cs`.
- Qualquer arquivo de `Dominio/`, `Contratos/`, `Repositorios/`, `Dados/Migracoes/` e `frontend/`.
- Os testes existentes, incluindo os nomes de método.
- Os status já existentes — **400, 401, 403, 404, 409, 422** — e as mensagens literais atuais.
- A ordem de tratamento: `DbUpdateException` de constraint conhecida continua produzindo 409/422 pelo
  `ErroDePersistenciaHandler`; o handler novo só vê o que ele não tratou.

### Critérios de aceitação (Dado / Quando / Então)

| # | Critério |
| :-- | :-- |
| C1 | **Dado** um endpoint cuja persistência lança exceção não mapeada (ex.: `UnidadeDeTrabalhoQueFalha`), **Quando** a requisição é processada, **Então** a resposta tem status **500** e corpo `{ "erro": "Erro interno. Tente novamente mais tarde." }`. |
| C2 | **Dado** uma falha cujo texto interno é `falha de persistência simulada`, **Quando** o cliente recebe a resposta, **Então** o corpo **não** contém `falha de persistência simulada`, **não** contém `StackTrace` e **não** contém o nome do tipo da exceção. |
| C3 | **Dado** a mesma falha, **Quando** ela é tratada, **Então** existe entrada de log de nível **`Error`** da categoria do handler, contendo a mensagem interna da exceção e o método e o caminho HTTP da requisição. |
| C4 | **Dado** o fluxo `POST /passeios` com falha na gravação, **Quando** a resposta 500 é devolvida, **Então** nada foi persistido: a consulta ao banco não encontra o passeio nem a notificação correspondente. |
| C5 | **Dado** uma violação de regra já existente (cancelar passeio `EM_ANDAMENTO`), **Quando** a requisição é processada, **Então** continua **422** com a mensagem da regra; e **dado** um e-mail já cadastrado, **Quando** o cadastro é repetido, **Então** continua **409** — sem regressão dos testes existentes. |
| C6 | **Dado** qualquer exceção tratada pelo handler novo, **Quando** o registro é feito, **Então** o corpo da resposta usa a mensagem genérica fixa, sem interpolar dados da exceção. |

Critérios C1–C4 e C6 são verificados pelo teste novo em `backend/Projeto.Tests/FalhaInesperadaTests.cs`;
C5 é verificado pelos testes já existentes (`PasseioEndpointsTests.Cancelar_EmAndamento_Devolve422SemNotificar`
e `EndpointsFalhaDePersistenciaTests.Cadastro_EmailDuplicado_Continua409`), que devem permanecer aprovados.

### Fora de escopo

- Alterar o frontend ou o formato de erro consumido por ele.
- Substituir o `ProblemDetails` de forma global (`AddProblemDetails` permanece como fallback).
- Autenticação, autorização, CORS, rate limiting.
- CI, migrations, schema, banco de dados.
- Qualquer refatoração de `ErroHttp`/`ErroDePersistencia` ou mudança de mensagens existentes.

## Plano esperado

1. Ler os sete arquivos listados em "Arquivos que o agente deve ler antes de agir" e confirmar que a lacuna
   existe: `grep -rn "ILogger\|LogError\|Erro interno" backend --include=*.cs` não retorna nenhuma linha.
2. Escrever **primeiro** `backend/Projeto.Tests/FalhaInesperadaTests.cs` e ver os testes **falharem pelo
   motivo certo** — corpo e log ausentes —, e não por erro de compilação.
   Detalhe do teste esperado: partir de `PetWalkApiFactory` (`backend/Projeto.Tests/PetWalkApiFactory.cs`,
   SQLite em memória); substituir `IUnidadeDeTrabalho` por um fake que lança `new Exception("segredo-interno")`
   (classe privada aninhada no próprio arquivo de teste — **não** editar `UnidadeDeTrabalhoQueFalha.cs`);
   capturar log com um `ILoggerProvider` simples escrito no teste, sem pacote novo; disparar `POST /passeios`
   com dados semeados (tutor, pet e passeador, no padrão de `EndpointsFalhaDePersistenciaTests.SemearTutorComPet`);
   depois, em novo escopo de serviço, conferir que não há `Passeio` nem `Notificacao` gravados.
3. Criar `backend/Projeto.Api/Endpoints/FalhaInesperadaHandler.cs`.
4. Registrá-lo em `backend/Projeto.Api/Program.cs` **depois** de `AddExceptionHandler<ErroDePersistenciaHandler>()`.
5. Rodar os testes novos até ficarem verdes.
6. Rodar a suíte completa e comparar com o baseline da Fatia 2: **291 casos, 289 aprovados, 2 falhas
   pré-existentes em `VerificacaoDeSchemaTests`, 0 ignorados, 14 s** (branch `feature/d03-falha-inesperada`,
   base `bae1c75`). O esperado é o mesmo par de falhas, nunca "suíte verde".
7. Verificar que `git diff --name-only` e `git status --short` juntos mostram exatamente os três arquivos
   permitidos — os dois arquivos novos aparecem como `??` (não rastreados) e não entram em `git diff`.

## Comandos de verificação

| Comando | Resultado esperado |
| :-- | :-- |
| `dotnet test backend/Projeto.Tests/Projeto.Tests.csproj --filter "FullyQualifiedName~FalhaInesperadaTests"` | todos os testes novos aprovados, 0 falhas, 0 ignorados |
| `dotnet test backend/Projeto.Tests/Projeto.Tests.csproj` | total = 291 + testes novos; aprovados = 289 + testes novos; falhas continuam exatamente 2 (`VerificacaoDeSchemaTests.BancoEmMemoria_NaoEhValidado` e `BancoComMigrationsAplicadas_NaoInterrompe`); 0 ignorados |
| `git diff --name-only` | mostra apenas `backend/Projeto.Api/Program.cs` (os dois arquivos novos são `??` e não aparecem aqui) |
| `git status --short` | exatamente os três arquivos permitidos: `M backend/Projeto.Api/Program.cs`, `?? backend/Projeto.Api/Endpoints/FalhaInesperadaHandler.cs`, `?? backend/Projeto.Tests/FalhaInesperadaTests.cs` |
| `grep -rn "ex.Message\|StackTrace" backend/Projeto.Api/Endpoints/FalhaInesperadaHandler.cs` | nenhuma linha (`exit=1`): o corpo da resposta não usa esses dados |

> Nota de ambiente: neste shell o restore do NuGet falha com `Value cannot be null. (Parameter 'path1')`
> porque `APPDATA`, `ProgramData` e `ProgramFiles(x86)` não são propagados; a execução desta sessão
> prefixou essas variáveis com `env`, sem alterar o repositório.

## Checklist de revisão

- [ ] **Escopo:** o diff toca apenas `FalhaInesperadaHandler.cs` (novo), `Program.cs` (só o registro e o comentário) e `FalhaInesperadaTests.cs` (novo).
- [ ] **Sem vazamento:** o corpo do 500 é exatamente `{ "erro": "Erro interno. Tente novamente mais tarde." }`, sem `ex.Message`, nome do tipo ou stack trace.
- [ ] **Log:** a exceção é registrada em nível `Error`, com o método HTTP e o caminho da requisição, preservando a mensagem interna.
- [ ] **Ordem:** `FalhaInesperadaHandler` é registrado depois de `ErroDePersistenciaHandler`, e o handler novo retorna `true`.
- [ ] **Regressões:** `Cancelar_EmAndamento_Devolve422SemNotificar` continua 422 e `Cadastro_EmailDuplicado_Continua409` continua 409.
- [ ] **Sem dependência nova:** nenhum pacote NuGet adicionado; o `ILoggerProvider` do teste é escrito no próprio arquivo.
- [ ] **Comentários:** os trechos novos explicam o porquê (ordem dos handlers, corpo genérico, log obrigatório quando o handler trata).
- [ ] **Evidência vermelho → verde:** saída dos testes novos falhando antes e passando depois foi colada no relatório.
- [ ] **Baseline respeitado:** a suíte completa mantém exatamente as 2 falhas pré-existentes de `VerificacaoDeSchemaTests`.

Revisão humana por: [ ] pendente — nome e data a preencher pelo grupo
