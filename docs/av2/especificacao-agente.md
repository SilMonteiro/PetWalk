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

- [x] **Escopo:** o diff toca apenas `FalhaInesperadaHandler.cs` (novo), `Program.cs` (só o registro e o comentário) e `FalhaInesperadaTests.cs` (novo).
- [x] **Sem vazamento:** o corpo do 500 é exatamente `{ "erro": "Erro interno. Tente novamente mais tarde." }`, sem `ex.Message`, nome do tipo ou stack trace.
- [x] **Log:** a exceção é registrada em nível `Error`, com o método HTTP e o caminho da requisição, preservando a mensagem interna.
- [x] **Ordem:** `FalhaInesperadaHandler` é registrado depois de `ErroDePersistenciaHandler`, e o handler novo retorna `true`.
- [x] **Regressões:** `Cancelar_EmAndamento_Devolve422SemNotificar` continua 422 e `Cadastro_EmailDuplicado_Continua409` continua 409.
- [x] **Sem dependência nova:** nenhum pacote NuGet adicionado; o `ILoggerProvider` do teste é escrito no próprio arquivo.
- [x] **Comentários:** os trechos novos explicam o porquê (ordem dos handlers, corpo genérico, log obrigatório quando o handler trata).
- [x] **Evidência vermelho → verde:** saída dos testes novos falhando antes e passando depois foi colada no relatório.
- [x] **Baseline respeitado:** a suíte completa mantém exatamente as 2 falhas pré-existentes de `VerificacaoDeSchemaTests`.

Revisão humana por: [X] Sarah - 15/11/2026

## Diff produzido

O grupo commitou o trabalho em dois commits na branch `feature/d03-falha-inesperada`: `7dc4922`
(*Implementação, passo 1 (testes primeiro, vermelhos)*) e `5a4b117` (*Implementação, passo 2 (handler e
registro, verde)*). Por isso `git status --short` e `git diff --stat` estão **vazios** nesta data: o diff
abaixo é o dos commits, não da árvore de trabalho.

| Arquivo | Situação | Linhas | Permitido pelo pedido? |
| :-- | :-- | :-- | :-- |
| `backend/Projeto.Api/Endpoints/FalhaInesperadaHandler.cs` | novo | 33 (`+33/−0`, commit `5a4b117`) | sim |
| `backend/Projeto.Api/Program.cs` | modificado | 5 inseridas, 2 removidas (commit `5a4b117`) | sim |
| `backend/Projeto.Tests/FalhaInesperadaTests.cs` | novo | 253 (`+253/−0`, commit `7dc4922`) | sim |

Total: **+291 / −2** em 3 arquivos.

**`git show 5a4b117 --numstat`:**

```text
33	0	backend/Projeto.Api/Endpoints/FalhaInesperadaHandler.cs
5	2	backend/Projeto.Api/Program.cs
```

**`git show 7dc4922 --numstat`:**

```text
253	0	backend/Projeto.Tests/FalhaInesperadaTests.cs
```

**Diff literal de `backend/Projeto.Api/Program.cs` (`git show HEAD -- backend/Projeto.Api/Program.cs`):**

```diff
diff --git a/backend/Projeto.Api/Program.cs b/backend/Projeto.Api/Program.cs
index fd50355..448754f 100644
--- a/backend/Projeto.Api/Program.cs
+++ b/backend/Projeto.Api/Program.cs
@@ -52,9 +52,12 @@ builder.Services.AddScoped<AutenticacaoServico>();
 builder.Services.AddScoped<NotificacaoServico>();
 
 // Tradução única de falhas de constraint do EF/SQLite (409/422) — ver ErroDePersistencia.
-// AddProblemDetails cobre o caso não tratado: é o fallback exigido pelo UseExceptionHandler()
-// sem path; as exceções reconhecidas continuam respondendo pelo handler abaixo.
+// A ordem de registro define a ordem de execução: primeiro a falha de persistência
+// conhecida vira 409/422; o que ela não reconhecer cai no FalhaInesperadaHandler, que
+// registra a exceção no log e devolve 500 com a mensagem genérica. AddProblemDetails
+// continua como fallback do UseExceptionHandler() sem path, caso nenhum handler reconheça.
 builder.Services.AddExceptionHandler<ErroDePersistenciaHandler>();
+builder.Services.AddExceptionHandler<FalhaInesperadaHandler>();
 builder.Services.AddProblemDetails();
 
 // Relógio injetável: os endpoints leem "agora" de time.GetUtcNow().UtcDateTime em vez de DateTime.UtcNow.
```

**Conteúdo integral de `backend/Projeto.Api/Endpoints/FalhaInesperadaHandler.cs` (33 linhas):**

```csharp
using Microsoft.AspNetCore.Diagnostics;

// Última barreira do tratamento de erros. O ErroDePersistenciaHandler já converteu as
// falhas de constraint conhecidas em 409/422; o que chega aqui é defeito não previsto.
// O cliente recebe só a mensagem genérica porque, por definição, ele não consegue corrigir
// a falha mudando a requisição; o detalhe (mensagem real e stack trace) fica no log.
public sealed class FalhaInesperadaHandler : IExceptionHandler
{
    private const string MensagemPadrao = "Erro interno. Tente novamente mais tarde.";

    private readonly ILogger<FalhaInesperadaHandler> _logger;

    public FalhaInesperadaHandler(ILogger<FalhaInesperadaHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext contexto, Exception excecao, CancellationToken cancelamento)
    {
        // O log não é opcional: quando um IExceptionHandler trata a exceção, o middleware
        // considera o caso resolvido e não registra nada por conta própria. Sem esta linha,
        // a falha desapareceria sem rastro.
        _logger.LogError(excecao, "Falha inesperada em {Metodo} {Caminho}",
            contexto.Request.Method, contexto.Request.Path);

        contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await contexto.Response.WriteAsJsonAsync(new { erro = MensagemPadrao }, cancelamento);

        return true;
    }
}
```

**Conteúdo integral de `backend/Projeto.Tests/FalhaInesperadaTests.cs` (253 linhas):**

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

// Critérios (a)–(d) do D03: uma falha inesperada na gravação deve virar 500 com o corpo
// genérico, sem vazar o detalhe interno, com a exceção registrada em log do projeto e sem
// nada persistido. O critério (e) — 422 para regra violada e 409 para e-mail duplicado —
// já é coberto pelos testes existentes e não é duplicado aqui.
//
// A API é a real (PetWalkApiFactory, SQLite em memória). Só a IUnidadeDeTrabalho é
// substituída, por um fake que lança uma exceção simples: não é DbUpdateException, então o
// ErroDePersistenciaHandler não a reconhece e é o caminho do D03 que fica exposto.
public class FalhaInesperadaTests
{
    private const string MensagemGenerica = "Erro interno. Tente novamente mais tarde.";
    private const string SegredoInterno = "segredo-interno";

    // O handler ainda não existe nesta fatia: o nome é literal de propósito, para o teste
    // compilar e falhar por comportamento, não por símbolo ausente.
    private const string NomeDoHandler = "FalhaInesperadaHandler";

    // (a) A falha inesperada responde 500 com o corpo do contrato, lido pelo frontend.
    [Fact]
    public async Task FalhaInesperada_Devolve500ComMensagemGenerica()
    {
        using var fabrica = new FabricaComFalhaInesperada();
        var (tutorId, petId, passeadorId) = Semear(fabrica);
        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/passeios", Requisicao(tutorId, petId, passeadorId));
        var corpo = await resposta.Content.ReadAsStringAsync();

        // A falha tem de ser a da gravação: se o fake não foi chamado, o 500 veio de outro lugar.
        Assert.Equal(1, fabrica.Unidade.Chamadas);
        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);

        Assert.True(
            TentarLerErro(corpo, out var mensagem),
            $"corpo sem o campo 'erro': {corpo}");
        Assert.Equal(MensagemGenerica, mensagem);
    }

    // (b) O cliente não recebe a mensagem interna, o nome do tipo nem stack trace.
    [Fact]
    public async Task FalhaInesperada_NaoExpoeMensagemInternaNemStackTrace()
    {
        using var fabrica = new FabricaComFalhaInesperada();
        var (tutorId, petId, passeadorId) = Semear(fabrica);
        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/passeios", Requisicao(tutorId, petId, passeadorId));
        var corpo = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.DoesNotContain(SegredoInterno, corpo);
        Assert.DoesNotContain("StackTrace", corpo);
        Assert.DoesNotContain("System.Exception", corpo);
        Assert.DoesNotContain(NomeDoHandler, corpo);

        // Só faz sentido falar em "não vazar" se o corpo for o contrato de erro esperado;
        // sem esta linha, um corpo vazio também "não vazaria" nada.
        Assert.True(
            TentarLerErro(corpo, out var mensagem),
            $"corpo sem o campo 'erro': {corpo}");
        Assert.Equal(MensagemGenerica, mensagem);
    }

    // (c) O detalhe da exceção fica no log do projeto, com o método e o caminho HTTP.
    [Fact]
    public async Task FalhaInesperada_RegistraErroNoLogComMetodoECaminho()
    {
        using var fabrica = new FabricaComFalhaInesperada();
        var (tutorId, petId, passeadorId) = Semear(fabrica);
        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/passeios", Requisicao(tutorId, petId, passeadorId));
        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);

        var registro = Assert.Single(
            fabrica.Coletor.Entradas,
            entrada => entrada.Nivel == LogLevel.Error
                       && entrada.Excecao?.Message == SegredoInterno
                       && entrada.Categoria.EndsWith(NomeDoHandler, StringComparison.Ordinal));

        Assert.Contains("POST", registro.Mensagem);
        Assert.Contains("/passeios", registro.Mensagem);
    }

    // (d) A resposta 500 é a tratada e nada foi gravado: sem passeio e sem notificação.
    //
    // Nota: "nada persistido" já era verdadeiro antes do D03, porque a exceção acontece
    // antes do SaveChanges. A pré-condição do corpo tratado existe para o teste não passar
    // em silêncio quando a requisição falha em outro ponto.
    [Fact]
    public async Task FalhaInesperada_NaoPersistePasseioNemNotificacao()
    {
        using var fabrica = new FabricaComFalhaInesperada();
        var (tutorId, petId, passeadorId) = Semear(fabrica);
        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/passeios", Requisicao(tutorId, petId, passeadorId));
        var corpo = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(1, fabrica.Unidade.Chamadas);
        Assert.True(
            TentarLerErro(corpo, out var mensagem),
            $"corpo sem o campo 'erro': {corpo}");
        Assert.Equal(MensagemGenerica, mensagem);

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<PetWalkDbContext>();

        Assert.Empty(contexto.Passeios);
        Assert.Empty(contexto.Notificacoes);
    }

    private static SolicitacaoPasseioRequest Requisicao(int tutorId, int petId, int passeadorId) =>
        new(tutorId, petId, passeadorId, Fabrica.Agora.AddDays(1), 60, "Rua A, 10");

    // Mesmo padrão de EndpointsFalhaDePersistenciaTests: escopo novo sobre o banco da fábrica.
    private static (int TutorId, int PetId, int PasseadorId) Semear(PetWalkApiFactory fabrica)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<PetWalkDbContext>();

        var tutor = Fabrica.CriarUsuario(TipoPerfil.TUTOR);
        var pet = Fabrica.CriarPet(tutor);
        tutor.AdicionarPet(pet);

        var passeador = Fabrica.CriarUsuario(TipoPerfil.PASSEADOR);
        passeador.DefinirCobranca(30m, TipoCobranca.POR_HORA);

        contexto.Usuarios.Add(tutor);
        contexto.Usuarios.Add(passeador);
        contexto.Pets.Add(pet);
        contexto.SaveChanges();

        return (tutor.Id, pet.Id, passeador.Id);
    }

    private static bool TentarLerErro(string corpo, out string? mensagem)
    {
        mensagem = null;

        try
        {
            using var json = JsonDocument.Parse(corpo);

            if (json.RootElement.ValueKind == JsonValueKind.Object
                && json.RootElement.TryGetProperty("erro", out var erro))
            {
                mensagem = erro.GetString();
                return true;
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed class FabricaComFalhaInesperada : PetWalkApiFactory
    {
        public UnidadeDeTrabalhoComExcecaoSimples Unidade { get; } = new();

        public ColetorDeLogs Coletor { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(servicos =>
            {
                servicos.RemoveAll<IUnidadeDeTrabalho>();
                servicos.AddSingleton<IUnidadeDeTrabalho>(Unidade);

                // Provider escrito no próprio teste: o projeto não depende de pacote de log
                // de teste, e o coletor só existe nesta fábrica.
                servicos.AddSingleton<ILoggerProvider>(Coletor);
            });
        }
    }

    private sealed class UnidadeDeTrabalhoComExcecaoSimples : IUnidadeDeTrabalho
    {
        public int Chamadas { get; private set; }

        public void Salvar()
        {
            Chamadas++;
            throw new Exception(SegredoInterno);
        }
    }

    private sealed class ColetorDeLogs : ILoggerProvider
    {
        private readonly List<EntradaDeLog> _entradas = new();
        private readonly object _trava = new();

        public IReadOnlyList<EntradaDeLog> Entradas
        {
            get
            {
                lock (_trava)
                {
                    return _entradas.ToArray();
                }
            }
        }

        public ILogger CreateLogger(string categoria) => new Coletor(categoria, this);

        public void Dispose()
        {
        }

        private void Registrar(EntradaDeLog entrada)
        {
            lock (_trava)
            {
                _entradas.Add(entrada);
            }
        }

        public sealed record EntradaDeLog(string Categoria, LogLevel Nivel, string Mensagem, Exception? Excecao);

        private sealed class Coletor : ILogger
        {
            private readonly string _categoria;
            private readonly ColetorDeLogs _dono;

            public Coletor(string categoria, ColetorDeLogs dono)
            {
                _categoria = categoria;
                _dono = dono;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) =>
                _dono.Registrar(new EntradaDeLog(_categoria, logLevel, formatter(state, exception), exception));
        }
    }
}
```

### Verificação de escopo

```text
$ git status --short
(vazio)

$ git diff --stat
(vazio)

$ git diff --name-only
(vazio)
```

A árvore está limpa porque tudo foi commitado. Comparando o que os commits produziram com o que o pedido
permitia:

| Permitido pelo pedido | Produzido | Situação |
| :-- | :-- | :-- |
| `backend/Projeto.Api/Endpoints/FalhaInesperadaHandler.cs` | sim (`5a4b117`, +33) | dentro |
| `backend/Projeto.Api/Program.cs` (só registro + comentário) | sim (`5a4b117`, +5/−2) | dentro |
| `backend/Projeto.Tests/FalhaInesperadaTests.cs` | sim (`7dc4922`, +253) | dentro |
| Qualquer outro arquivo | não | — |

Verificações complementares: `grep -rn "ex.Message\|StackTrace"` no handler → **nenhuma linha** (`exit=1`);
`ErroHttp.cs`, `ErroDePersistenciaHandler.cs`, `ErroDePersistencia.cs`, `Dominio/`, `Contratos/`,
`Repositorios/`, `Migracoes/` e `frontend/` não aparecem em nenhum dos dois commits.

## Resultado da verificação

| Momento | Comando | Total | Aprovados | Falhas | Não executados | Tempo |
| :-- | :-- | --: | --: | --: | --: | --: |
| Baseline, antes de qualquer escrita (Fatia 2, `bae1c75`) | suíte completa | 291 | 289 | 2 | 0 | 14 s |
| Testes novos antes do handler (Fatia 5) | `--filter "FullyQualifiedName~FalhaInesperadaTests"` | 4 | 0 | 4 | 0 | 5 s |
| Testes novos depois do handler (Fatia 6) | `--filter "FullyQualifiedName~FalhaInesperadaTests"` | 4 | 4 | 0 | 0 | 4 s |
| Suíte completa, 1ª medição pós-D03 (Fatia 6) | suíte completa | 295 | 293 | 2 | 0 | 19 s |
| Suíte completa, registro desta fatia | suíte completa | 295 | 293 | 2 | 0 | 13 s |

Diferença entre baseline e depois: **+4 no total e +4 nos aprovados** — exatamente os quatro testes de
`FalhaInesperadaTests`. As falhas são as mesmas duas, `VerificacaoDeSchemaTests.BancoEmMemoria_NaoEhValidado`
e `VerificacaoDeSchemaTests.BancoComMigrationsAplicadas_NaoInterrompe`, pré-existentes. Nenhum ignorado em
nenhuma execução.

**Saídas literais dos resumos:**

```text
# Baseline (Fatia 2)
Com falha! – Com falha:     2, Aprovado:   289, Ignorado:     0, Total:   291, Duração: 14 s - Projeto.Tests.dll (net10.0)

# Testes novos antes do handler (Fatia 5)
Com falha! – Com falha:     4, Aprovado:     0, Ignorado:     0, Total:     4, Duração: 5 s - Projeto.Tests.dll (net10.0)

# Testes novos depois do handler (Fatia 6)
Aprovado!  – Com falha:     0, Aprovado:     4, Ignorado:     0, Total:     4, Duração: 4 s - Projeto.Tests.dll (net10.0)

# Suíte completa (registro desta fatia)
Com falha! – Com falha:     2, Aprovado:   293, Ignorado:     0, Total:   295, Duração: 13 s - Projeto.Tests.dll (net10.0)
```

**Ambiente:** SDK .NET `10.0.400` (`dotnet --version`); Windows 10.0.19045, win-x64; branch
`feature/d03-falha-inesperada`; commit base `bae1c75`; HEAD `5a4b117`. Neste shell o restore do NuGet falha
com `Value cannot be null. (Parameter 'path1')`; as execuções prefixaram `APPDATA`, `ProgramData`,
`ProgramFiles` e `ProgramFiles(x86)` via `env`, sem alterar o repositório.

**Limitações — o que esta verificação NÃO prova:**

- O log foi conferido pelo provider de teste (categoria, nível e mensagem), não pela saída real do
  `ILogger` em execução normal da API.
- O comportamento quando a resposta já começou (`Response.HasStarted`) não foi testado; os endpoints atuais
  não escrevem corpo antes da gravação.
- Nenhum teste de frontend foi executado; `frontend/src/services/api.js` apenas foi lido.
- Não há teste dedicado do handler novo sob concorrência nem sob carga.
- O bloqueio de SQLite por App Control não ocorreu nesta máquina (0 não executados), mas não há CI
  configurado: a repetição em outra máquina **não foi verificada**.
- A revisão humana do checklist permanece pendente; o checklist não foi preenchido nesta fatia.

## Inspeção do diff

Inspeção feita em 15/11/2026 sobre os commits `7dc4922` e `5a4b117`, com a árvore limpa. Nenhum
arquivo de código foi tocado nesta fatia: só este documento. A frase da seção anterior ("o checklist não foi
preenchido nesta fatia") descreve o estado da fatia de registro; o preenchimento acontece aqui.

### Checklist verificado por comando

| Item | Evidência executada | Resultado |
| :-- | :-- | :-- |
| Escopo | `git show --name-only --format="%h %s" 7dc4922 5a4b117` → `FalhaInesperadaTests.cs`, `FalhaInesperadaHandler.cs`, `Program.cs`; `git status --short` e `git diff --stat` vazios | atende |
| Sem vazamento | `FalhaInesperadaHandler.cs:27-29` grava só `new { erro = MensagemPadrao }`; `grep -rn "ex.Message\|StackTrace"` no handler → 0 linhas (`exit=1`); teste `FalhaInesperada_NaoExpoeMensagemInternaNemStackTrace` aprovado | atende |
| Log em `Error` com método e caminho | `FalhaInesperadaHandler.cs:24-25` (`LogError(excecao, "Falha inesperada em {Metodo} {Caminho}", …)`); teste `FalhaInesperada_RegistraErroNoLogComMetodoECaminho` aprovado | atende |
| Ordem de registro | `grep -n "AddExceptionHandler\|AddProblemDetails\|UseExceptionHandler" Program.cs` → `59` `ErroDePersistenciaHandler`, `60` `FalhaInesperadaHandler`, `61` `AddProblemDetails`, `78` `UseExceptionHandler()` | atende |
| Sem pacote novo | `git diff --name-only bae1c75..HEAD -- "*.csproj" "*.props" "*.targets"` → **vazio** | atende |
| Comentários explicam o porquê | `FalhaInesperadaHandler.cs:3-6` (por que mensagem genérica) e `:21-23` (por que o log é obrigatório); `Program.cs:54-58` (cadeia dos handlers) | atende |
| Vermelho → verde | Fatia 5: `Com falha: 4, Aprovado: 0`; Fatia 6: `Aprovado: 4, Com falha: 0` (resumos na seção anterior) | atende |
| Regressões 409/422 | `dotnet test --filter "…Cancelar_EmAndamento_Devolve422SemNotificar\|…Cadastro_EmailDuplicado_Continua409"` → `Aprovado PasseioEndpointsTests.Cancelar_EmAndamento_Devolve422SemNotificar` e `Aprovado EndpointsFalhaDePersistenciaTests.Cadastro_EmailDuplicado_Continua409`; resumo `Aprovado! – Com falha: 0, Aprovado: 6, Ignorado: 0, Total: 6` | atende |
| Baseline | suíte completa: `Com falha: 2, Aprovado: 293, Ignorado: 0, Total: 295` — as mesmas 2 de `VerificacaoDeSchemaTests` | atende |

**Pergunta de controle: o teste passaria sem o handler?** Não. Na Fatia 5, antes do handler, os 4 testes
falharam e o corpo real era `{"type":"https://tools.ietf.org/html/rfc9110#section-15.6.1","title":"An error
occurred while processing your request.","status":500,"traceId":"…"}` — sem campo `erro`. Nuance já
documentada: em (b) as três asserções de não-vazamento passavam mesmo antes; o vermelho veio da ausência do
contrato, não de vazamento.

**A mensagem genérica está idêntica ao pedido?** Sim: `grep -n "Erro interno. Tente novamente mais tarde."`
→ `FalhaInesperadaHandler.cs:9` e `FalhaInesperadaTests.cs:19`, texto igual ao da especificação.

**O log inclui dado sensível?** Não inclui corpo da requisição nem query string: registra `Request.Method` e
`Request.Path` (`:25`) e a exceção. A mensagem da exceção pode carregar dados do provedor (é log de servidor,
não de cliente) — aceitável, mas é o único vetor.

### Achados

| # | Gravidade | Achado | Sugestão |
| :-- | :-- | :-- | :-- |
| A1 | Média | `FalhaInesperadaHandler.TryHandleAsync` não verifica `Response.HasStarted`. `grep -n "HasStarted"` no handler → 0 linhas. Se a resposta já tiver começado, atribuir `StatusCode` (`:27`) lança `InvalidOperationException` e o corpo genérico não é escrito. Nenhum teste cobre esse caminho | `if (contexto.Response.HasStarted) return false;` antes de tocar na resposta |
| A2 | Baixa (não verificado no repositório) | O handler é catch-all: qualquer exceção vira 500, inclusive `BadHttpRequestException` com status 4xx (ex.: 413 de corpo grande), **se** ela chegar ao middleware. Não há teste que force esse caminho | Devolver `false` para `BadHttpRequestException` (ou respeitar `ex.StatusCode`) |
| A3 | Baixa | `:29` passa `cancelamento` para `WriteAsJsonAsync`. Se o cliente desconectar, a escrita pode lançar `OperationCanceledException` depois do `LogError`, e a exceção original deixa de ser a que propaga | Usar `CancellationToken.None` ou capturar `OperationCanceledException` após o log |
| A4 | Baixa | Os testes comparam só a propriedade `erro` (`TentarLerErro` + `Assert.Equal`). Um corpo com campo extra (`{\"erro\":\"…\",\"detalhe\":\"x\"}`) passaria em (a), (b) e (d), desde que o extra não contenha os textos proibidos | Comparar o corpo serializado completo, ou exigir exatamente uma propriedade |
| A5 | Baixa (pré-existente, fora do diff) | `ErroHttp.cs:20` faz `_ => throw ex;`, o que reinicia o stack trace. Não foi introduzido por esta alteração e o handler novo não depende dele | Trocar por `throw;` numa alteração própria |
| A6 | Baixa (não verificado) | Ordem do pipeline: `UseExceptionHandler()` na linha 78 vem antes de `UseCors("Frontend")` na 88. A resposta 500 pode sair sem cabeçalhos CORS e o navegador exibir erro de rede em vez do corpo `{ \"erro\" }`. Não há teste de CORS no projeto | Teste de integração com cabeçalho `Origin` conferindo `Access-Control-Allow-Origin` no 500 |

### Correções feitas à saída do agente

Nenhuma aplicada nesta fatia; os achados acima ficam para decisão do grupo. O código-fonte não foi alterado.

Revisão humana por: Sarah - 15/11/2026

## Rastreio

Comandos usados (15/11/2026, antes desta escrita):

```text
$ git log --reverse --format="%H %an %ad %s" --date=iso bae1c75..HEAD
$ git diff --name-status bae1c75..HEAD -- backend frontend
```

| Hash completo | Autor | Data (ISO) | Assunto | Tipo |
| :-- | :-- | :-- | :-- | :-- |
| `cc926b5ceef21d42cc24c378bff0990b4b0c3e6e` | Sarah Domingues | 2026-11-15 23:20:25 -0300 | Documento, parte 1 (contexto e pedido delimitado) | documentação |
| `0a0093f6d487d063ccfb2c5aeafb16f75d1f9c75` | Sarah Domingues | 2026-11-15 23:22:06 -0300 | Documento, parte 2 (plano esperado e checklist) | documentação |
| `7dc49225061497abee052fe11baf1c326d64f688` | Sarah Domingues | 2026-11-15 23:26:57 -0300 | Implementação, passo 1 (testes primeiro, vermelhos) | testes |
| `5a4b11716c3d850db06173d74311b9a6f0539722` | Sarah Domingues | 2026-11-15 23:29:54 -0300 | Implementação, passo 2 (handler e registro, verde) | handler |
| `8ca150a40a2864fde3e0887587934883962b31fb` | Sarah Domingues | 2026-11-15 23:33:18 -0300 | Registrar o diff e os resultados no documento | documentação |
| `e72233159f10a89d96d6037b958c587e462cd061` | Sarah Domingues | 2026-11-15 23:37:02 -0300 | Inspeção do diff (checklist) | documentação |
| `a6979ef58c224923260b6ed54034bc2ec2aa9ef2` | Sarah Domingues | 2026-11-16 00:15:15 -0300 | inspeção diff | documentação |

| Campo | Valor |
| :-- | :-- |
| Commit base | `bae1c756c94e20e5948d03ab9ccfe2656aea6493` |
| Branch | `feature/d03-falha-inesperada` |
| HEAD no momento do rastreio | `a6979ef58c224923260b6ed54034bc2ec2aa9ef2` |
| PR | **não aberto** (nenhum link inventado) |

**Ordem dos commits de código (verificada, não presumida):** `git log --reverse` mostra `7dc4922` (testes)
**antes** de `5a4b117` (handler e registro) — os testes nasceram vermelhos primeiro, como o plano exigia.

**Escopo do código no intervalo:** `git diff --name-status bae1c75..HEAD -- backend frontend` devolve
exatamente:

```text
A	backend/Projeto.Api/Endpoints/FalhaInesperadaHandler.cs
M	backend/Projeto.Api/Program.cs
A	backend/Projeto.Tests/FalhaInesperadaTests.cs
```

Nenhum outro arquivo de `backend/` ou `frontend/` foi tocado. Conferi commit a commit que os cinco commits
de documentação (`cc926b5`, `0a0093f`, `8ca150a`, `e722331`, `a6979ef`) alteram **apenas**
`docs/av2/especificacao-agente.md`.

**Como repetir o processo:** criar uma branch nova a partir de `bae1c756c94e20e5948d03ab9ccfe2656aea6493`,
entregar ao agente as seções "Contexto fornecido ao agente" e "Pedido delimitado" deste documento, seguir
"Plano esperado" e validar com "Comandos de verificação". O resultado esperado é o mesmo conjunto de três
arquivos e os quatro testes de `FalhaInesperadaTests` aprovados.
