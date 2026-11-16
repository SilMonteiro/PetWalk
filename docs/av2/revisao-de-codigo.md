# Revisão de código — PetWalk (AV2)

Este documento revisa uma alteração funcional real do histórico do PetWalk, escolhida para a Etapa 6 do
`docs/plano-av2.md` (itens C01–C06). O objetivo é que um revisor consiga decidir entre **aceitar**,
**corrigir** ou **rejeitar** a alteração sem abrir nada além do que está citado aqui. Cada item é
preenchido em uma fatia própria; neste momento apenas **C01** está preenchido.

## Alteração revisada

| Campo | Valor |
| :-- | :-- |
| Hash completo | `b22819d1fb86e44ab21ce19ac3ac3f4389ca95e6` |
| Hash curto | `b22819d` |
| Autor | danielmdelgado `<danielmdelgado11@gmail.com>` |
| Data (ISO) | `2026-11-06 20:08:04 -0300` |
| Assunto | AdicionarPet exige perfil de tutor |
| Commit pai | `8871eef` ("Tirar DateTime.UtcNow do domínio") |

Ambiente desta revisão: SDK .NET `10.0.400` (`dotnet --version`), 15/11/2026. A alteração **não** é a mesma
do registro de depuração (`docs/av2/registro-de-depuracao.md`), como o plano exige.

## C01 — Identificação da alteração

**Comando executado e saída literal:**

```bash
git show --stat --format="%H%n%an <%ae>%n%ad%n%s%n%b" --date=iso b22819d
```

```text
b22819d1fb86e44ab21ce19ac3ac3f4389ca95e6
danielmdelgado <danielmdelgado11@gmail.com>
2026-11-06 20:08:04 -0300
AdicionarPet exige perfil de tutor


 backend/Projeto.Api/Dominio/Usuario.cs |  6 ++++++
 backend/Projeto.Tests/UsuarioTests.cs  | 23 +++++++++++++++++++++++
 2 files changed, 29 insertions(+)
```

**Trecho do diff que implementa a mudança.** Arquivo: `backend/Projeto.Api/Dominio/Usuario.cs` — é o
**único** arquivo de produção do commit, e o trecho abaixo é a totalidade da mudança de produção. Foi
escolhido porque é exatamente nele que a regra passa a ser aplicada: a guarda nova fica no início de
`AdicionarPet`, antes das validações que já existiam.

```diff
diff --git a/backend/Projeto.Api/Dominio/Usuario.cs b/backend/Projeto.Api/Dominio/Usuario.cs
index 793110b..1db676e 100644
--- a/backend/Projeto.Api/Dominio/Usuario.cs
+++ b/backend/Projeto.Api/Dominio/Usuario.cs
@@ -49,6 +49,12 @@ public class Usuario
 
     public void AdicionarPet(Pet pet)
     {
+        // REG-002: só quem tem perfil de tutor pode ter pets no próprio perfil.
+        if (!EhTutor)
+        {
+            throw new InvalidOperationException("Usuário não possui perfil de tutor");
+        }
+
         if (pet is null)
         {
             throw new ArgumentNullException(nameof(pet));
```

**O que a alteração faz (fato observável no diff).** `Usuario.AdicionarPet` passa a começar com uma guarda:
se `!EhTutor`, lança `InvalidOperationException` com a mensagem literal
`"Usuário não possui perfil de tutor"`. A guarda é inserida **antes** da verificação `pet is null` que já
existia, e nenhuma linha é removida — o arquivo tem `+6/−0`, sem mudança de assinatura, de visibilidade ou
de estado. O restante do método (a validação de posse do pet, a checagem de duplicidade e o `_pets.Add`)
permanece como estava.

**Problema que a alteração resolve, segundo a mensagem do commit.** A mensagem é "AdicionarPet exige perfil
de tutor". Antes do commit, `AdicionarPet` validava apenas `pet is null`, a posse (`pet.Tutor != this`) e a
duplicidade; um usuário com perfil `PASSEADOR` podia vincular pets ao próprio perfil. A alteração fecha esse
caminho colocando a restrição de perfil dentro do agregado `Usuario`, e não no chamador.

**Sobre omissões.** Nenhum trecho do diff de produção foi omitido: `git show b22819d -- backend/Projeto.Api`
produz exatamente o bloco acima (uma única hunk, 6 linhas adicionadas). O outro arquivo do commit é de teste
(`backend/Projeto.Tests/UsuarioTests.cs`, `+23/−0`) e seu diff **não** é reproduzido aqui por não implementar
a mudança; o commit completo, com os dois diffs, está em `git show b22819d`
(ou `git show b22819d -- backend/Projeto.Tests` apenas para o teste).

## C02 — Ligação com o requisito

**Regra ligada — REG-002.** A `docs/av2/especificacao.md` **não tem seção dedicada à REG-002**: as cinco
regras fixadas pela AV2 são REG-001, REG-003, REG-004, REG-005 e REG-008. O enunciado da regra está na
proposta (`docs/proposta.md:48`), literal:

> "Somente um Usuário com tipoPerfil TUTOR ou AMBOS pode solicitar um Passeio."

E a especificação a menciona em três pontos, também literalmente:

- `especificacao.md:106` (pré-condição do FLX-002): "Tutor existente, com perfil TUTOR ou AMBOS (REG-002)."
- `especificacao.md:118` (ordem de validação): "A API chama `Usuario.SolicitarPasseio`, que valida nesta
  ordem: perfil de tutor (REG-002), posse do pet (REG-005), perfil e status do passeador (REG-009) e data
  futura (REG-008). Toda falha de regra é `InvalidOperationException` e a API responde **422** com a
  mensagem da regra."
- `especificacao.md:163` (tabela critério → teste): `UsuarioSemPerfilDeTutor_NaoPodeSolicitar` —
  `REG-002: "Usuário não possui perfil de tutor"`.

**Fluxo relacionado.** Na especificação, apenas o **FLX-002** (solicitar passeio). Na proposta, a REG-002
também aparece no **FLX-007** (`proposta.md:79`, "Tutor cadastra, consulta, edita e exclui seus pets"), fluxo
que a especificação da AV2 **não** fixa — o escopo dela é FLX-002, FLX-004 e FLX-005.

**Divergência registrada.** O commit altera `Usuario.AdicionarPet`, mas a especificação só trata a REG-002
no caminho de *solicitação* (`Usuario.SolicitarPasseio`). Nenhum critério numerado da especificação é
satisfeito por este diff: o commit **estende** a mesma regra para outra operação do agregado, que a
especificação não documenta.

**Critério de aceitação → onde o código implementa.** Linhas conferidas em
`git show b22819d:backend/Projeto.Api/Dominio/Usuario.cs`:

| Critério | Condição (Dado / Quando / Então) | Implementação no commit | Mensagem literal | Status HTTP |
| :-- | :-- | :-- | :-- | :-- |
| C1 — sucesso (reconstruído do enunciado) | Dado um usuário TUTOR ou AMBOS / Quando adiciona um pet que lhe pertence / Então o pet passa a estar em `_pets` | `backend/Projeto.Api/Dominio/Usuario.cs:Usuario.AdicionarPet` — `_pets.Add(pet)` na linha 71 | — | — |
| C2 — violação (reconstruído do enunciado) | Dado um usuário só PASSEADOR / Quando chama `AdicionarPet` / Então recebe `InvalidOperationException` e `Pets` continua vazio | `backend/Projeto.Api/Dominio/Usuario.cs:Usuario.AdicionarPet` — guarda nas linhas 53–56 | `"Usuário não possui perfil de tutor"` | — |
| C3 — pré-condição do FLX-002 (`especificacao.md:106`) | Dado um usuário sem perfil TUTOR ou AMBOS / Quando solicita passeio / Então a operação falha com a mensagem da REG-002 | `backend/Projeto.Api/Dominio/Usuario.cs:Usuario.SolicitarPasseio` — guarda nas linhas 91–94 | `"Usuário não possui perfil de tutor"` | 422 (só no `HEAD`; o commit não tem rota) |

**C1 — já era verdadeiro antes; o commit preservou.** Em `b22819d^` não existia guarda de perfil: um
usuário TUTOR ou AMBOS já chegava a `_pets.Add(pet)`. O commit não alterou esse caminho; acrescentou o teste
`AdicionarPet_ComPerfilAmbos_AdicionaOPet`, que passa a provar o comportamento.

**C2 — é o commit que torna verdadeiro.** Em `b22819d^`, o método começava direto em `if (pet is null)`; um
PASSEADOR cujo `pet.Tutor` fosse ele mesmo conseguia adicionar o pet (e o construtor de `Pet` não valida o
perfil do tutor — `git show b22819d:backend/Projeto.Api/Dominio/Pet.cs` só tem a guarda de `tutor` nulo).
Depois do commit, a guarda `if (!EhTutor)` interrompe antes, e `AdicionarPet_ComPerfilDePasseador_LancaErro`
assere a mensagem e `Assert.Empty(passeador.Pets)`.

**C3 — já era verdadeiro antes; o commit preservou.** Em `b22819d^`, `SolicitarPasseio` (linha 83) já trazia
a guarda nas linhas 85–87, com a mesma mensagem; o commit apenas deslocou o bloco em +6 linhas (agora 89 e
91–94), por causa da guarda inserida acima.

**Status HTTP.** O commit não tem rota que alcance `AdicionarPet`: em `b22819d` o `Program.cs` mapeia apenas
`/teste`, `/cadastro` e `/login`, e o `UsuarioEndpoints` do commit só declara `/cadastro` e `/login`. Logo, o
commit não produz status HTTP. O **422** aparece no `HEAD`, onde
`backend/Projeto.Api/Endpoints/ErroHttp.cs:19` mapeia `InvalidOperationException` para
`422 UnprocessableEntity`.

### Lacunas

- **A especificação não tem seção REG-002**, então C1 e C2 foram reconstruídos do enunciado da proposta e
  não copiados de critérios numerados da especificação.
- **A especificação não documenta `AdicionarPet` nem o FLX-007**; a ligação mais direta do commit com um
  fluxo está na proposta (FLX-007), fora do escopo fixado da AV2.
- **Parte do commit sem critério correspondente:** a ordem das validações mudou — a guarda de perfil passou
  a vir antes de `pet is null`. Em `b22819d^`, um PASSEADOR que passasse `null` recebia
  `ArgumentNullException`; depois do commit recebe `InvalidOperationException` da REG-002. A especificação
  não trata desse caso, e nenhum teste do commit o cobre.
- **Nenhum critério da especificação é tornado verdadeiro por este diff**: a implementação que ela descreve
  (`SolicitarPasseio`) é anterior ao commit; o diff estende a regra para `AdicionarPet`.
- **O perfil AMBOS não aparece em nenhum critério da especificação** — só no enunciado da proposta
  ("TUTOR ou AMBOS"); o teste `AdicionarPet_ComPerfilAmbos_AdicionaOPet` cobre esse caso no commit.

## C03 — Inventário dos arquivos alterados

Inventário conferido contra `git show --stat --name-status b22819d` e `git show --numstat b22819d` — a
saída lista **exatamente 2 arquivos**, e a tabela abaixo tem as mesmas 2 linhas:

```text
M	backend/Projeto.Api/Dominio/Usuario.cs
M	backend/Projeto.Tests/UsuarioTests.cs

6	0	backend/Projeto.Api/Dominio/Usuario.cs
23	0	backend/Projeto.Tests/UsuarioTests.cs
```

| Arquivo | Tipo | +linhas/−linhas | Finalidade da mudança | Escopo | Justificativa |
| :-- | :-- | :-- | :-- | :-- | :-- |
| `backend/Projeto.Api/Dominio/Usuario.cs` | M | +6/−0 | Inserir a guarda de perfil no início de `Usuario.AdicionarPet` (`if (!EhTutor)` → `InvalidOperationException("Usuário não possui perfil de tutor")`) | **Dentro** | É a própria regra do título ("AdicionarPet exige perfil de tutor") e a única mudança de produção do commit; nenhuma linha removida, nenhuma alteração de assinatura ou de estado |
| `backend/Projeto.Tests/UsuarioTests.cs` | M | +23/−0 | Acrescentar dois testes no início da classe: `AdicionarPet_ComPerfilDePasseador_LancaErro` (assere a mensagem e `Assert.Empty(passeador.Pets)`) e `AdicionarPet_ComPerfilAmbos_AdicionaOPet` (assere `Assert.Same`/`Assert.Single`) | **Dentro** | Cada teste cobre um lado da mesma guarda (violação e sucesso); o diff é composto apenas pelos dois blocos `[Fact]`, sem alterar os testes que já existiam |

Nenhum arquivo mistura mudança de escopo com refatoração, renomeação ou formatação: li o diff completo dos
dois arquivos (`git show b22819d -- backend/Projeto.Api/Dominio/Usuario.cs` e
`git show b22819d -- backend/Projeto.Tests/UsuarioTests.cs`), e as 29 linhas adicionadas são exclusivamente a
guarda (6) e os dois testes (23), sem nenhuma linha removida.

### Mudanças fora do escopo

**Nenhuma mudança fora do escopo identificada.** A leitura dos dois diffs sustenta a afirmação: o arquivo de
produção tem uma única hunk (a guarda, dentro de `AdicionarPet`) e o arquivo de teste tem uma única hunk
(dois testes novos), sem tocar `RemoverPet`, `SolicitarPasseio` ou qualquer outro método da classe.

A única alteração de comportamento que vai além do caso previsto é **efeito da posição da guarda**, dentro
das mesmas 6 linhas: ela foi inserida antes de `if (pet is null)`, então um usuário sem perfil de tutor que
passe `null` passa a receber `InvalidOperationException` em vez de `ArgumentNullException`. Isso já está
registrado como lacuna em **C02** (não há critério nem teste para esse caso); não é uma mudança de outro
arquivo nem de outro propósito.

**Resumo:** 2 arquivos no total — **1 de produção** (`Usuario.cs`), **1 de teste** (`UsuarioTests.cs`),
**0 de documentação/configuração**. Somatório: `+29/−0`.

## C04 — Análise de impacto

**Símbolos alterados.** O diff não cria, remove nem renomeia símbolo algum: altera o **comportamento** de um
método público que já existia, `Usuario.AdicionarPet(Pet pet)`
(`backend/Projeto.Api/Dominio/Usuario.cs`, linha 50 no commit). Não há mudança de assinatura, de parâmetro,
de construtor, de propriedade nem de DTO. A guarda nova consome `Usuario.EhTutor` (propriedade preexistente,
linha 20 no commit), que **não** foi alterada.

**Comandos usados** (executados nesta sessão, no estado do commit e no `HEAD`):

```bash
git grep -n "AdicionarPet" b22819d -- backend frontend
git grep -n "AdicionarPet" -- backend frontend
git grep -n "RegistrarPet" b22819d -- backend frontend
git grep -n "RegistrarPet" -- backend frontend
```

No commit, `git grep "AdicionarPet" b22819d -- backend frontend` retorna 1 referência de produção
(`UsuarioRepositorioEmMemoria.cs:29`) e 10 de teste; no `HEAD`, retorna 2 de produção
(`UsuarioRepositorioEmMemoria.cs:29` e `Ef/UsuarioRepositorio.cs:49`) e o restante em testes. Nenhuma
referência no `frontend` em nenhum dos dois estados (`AdicionarPet` é símbolo C#, não existe em JS).

### Impacto direto

| Símbolo | Quem chama/referencia | Tipo | Alterado pelo commit? |
| :-- | :-- | :-- | :-- |
| `Usuario.AdicionarPet` | `UsuarioRepositorioEmMemoria.RegistrarPet` — `backend/Projeto.Api/Repositorios/UsuarioRepositorioEmMemoria.cs:29` (no commit) | produção | **não** |
| `Usuario.AdicionarPet` | `Fabrica.CriarPasseioSolicitadoPara` — `backend/Projeto.Tests/Fabrica.cs:43` | teste | **não** |
| `Usuario.AdicionarPet` | `SolicitacaoDePasseioTests` — linhas 17, 61, 75 e 89 | teste | **não** |
| `Usuario.AdicionarPet` | `PasseioValorTests:68` e `ProtecaoDeEstadoTests:116` | teste | **não** |
| `Usuario.AdicionarPet` | `UsuarioTests` — linhas 12 e 24 (métodos novos) e 34 (teste que já existia) | teste | **sim** (2 métodos) |
| `IUsuarioRepositorio.RegistrarPet` | declarado em `backend/Projeto.Api/Repositorios/IUsuarioRepositorio.cs:13`, implementado em `UsuarioRepositorioEmMemoria.cs:27` (no commit) | produção | **não** |

### Impacto transitivo

**No commit (b22819d):** a cadeia é `Usuario.AdicionarPet` ← `UsuarioRepositorioEmMemoria.RegistrarPet`
(linhas 27–29) ← **ninguém em produção**. O `git grep "RegistrarPet" b22819d -- backend frontend` mostra
apenas a interface (linha 13), a implementação (27) e dois usos em teste (`UsuarioRepositorioTests.cs:63–64`);
não há endpoint nem tela. E o commit só expõe três rotas: `Program.cs` mapeia `/teste` e
`MapUsuarioEndpoints()`, e `UsuarioEndpoints.cs` declara apenas `/cadastro` (linha 5) e `/login` (linha 25).
**A cadeia para no método de repositório — não existe rota HTTP nem tela que alcance a mudança.**

**No HEAD (para comparação):** a cadeia está completa —
`Usuario.AdicionarPet` (`Usuario.cs:58`) ← `RegistrarPet` (`Ef/UsuarioRepositorio.cs:49` e
`UsuarioRepositorioEmMemoria.cs:29`) ← `PetEndpoints.cs:50`, dentro do `MapPost("/usuarios/{id:int}/pets")`
(linha 35) ← `frontend/src/services/petService.js:12` (`POST /usuarios/${usuarioId}/pets`) ←
`frontend/src/pages/PetFormPage.jsx:6` (importa `criarPet`). Aqui a cadeia termina em endpoint e tela.

### Contratos afetados

| Contrato | Mudou? | Evidência |
| :-- | :-- | :-- |
| Assinatura `public void AdicionarPet(Pet pet)` | **Não** | O diff (`git show b22819d -- backend/Projeto.Api/Dominio/Usuario.cs`) só adiciona linhas **dentro** do corpo; a linha da assinatura aparece como contexto, não como `+`/`−` |
| Interface `IUsuarioRepositorio` / `RegistrarPet` | **Não** | Arquivo não aparece no `--name-status` do commit |
| DTOs de `Contratos/` | **Não** | No commit a pasta só tem `LoginRequest.cs` e `UsuarioRequest.cs` (`git ls-tree -r --name-only b22819d -- backend/Projeto.Api/Contratos`); nenhum DTO de pet existe e nenhum arquivo dela foi tocado |
| Mensagem literal `"Usuário não possui perfil de tutor"` | **Não** (o texto é o mesmo) | O diff adiciona a mesma string que `Usuario.SolicitarPasseio` já lançava (linhas 85–87 em `b22819d^`); o que muda é o caminho que passa a emiti-la |
| Status HTTP | **Não** no commit | Não há rota que alcance `AdicionarPet` em `b22819d`; no `HEAD` o mesmo `InvalidOperationException` vira **422** (`backend/Projeto.Api/Endpoints/ErroHttp.cs:19`) |
| Corpo `{ "erro": ... }` | **Não** | O commit não toca `ErroHttp` nem `Program.cs`; o formato é do `HEAD` |
| Chamadas do frontend | **Não** | No commit, `frontend/src/components/PetRegistrationScreen.jsx` não faz nenhuma requisição (`grep -n "fetch\|localhost\|/pets"` no arquivo do commit não retorna nada); no `HEAD` existe `petService.js` chamando a rota |

**Nenhum contrato público foi alterado pelo commit.** O único contrato que muda de fato é comportamental: uma
chamada que antes retornava normalmente agora pode lançar `InvalidOperationException`.

### Testes impactados

| Teste (classe.método) | Situação no commit | Exercita |
| :-- | :-- | :-- |
| `UsuarioTests.AdicionarPet_ComPerfilDePasseador_LancaErro` | **criado** | `AdicionarPet` com PASSEADOR → exceção + lista vazia |
| `UsuarioTests.AdicionarPet_ComPerfilAmbos_AdicionaOPet` | **criado** | `AdicionarPet` com AMBOS → pet adicionado |
| `UsuarioRepositorioTests.RegistrarPet_VinculaAoTutor_AtribuiIdEPermiteObterPet` (linha 56) | intacto | `RegistrarPet` → `AdicionarPet` |
| `SolicitacaoDePasseioTests` (linhas 17, 61, 75, 89) e `Fabrica.CriarPasseioSolicitadoPara` (43) | intacto | monta cenário chamando `AdicionarPet` |
| `PasseioValorTests` (68) e `ProtecaoDeEstadoTests` (116) | intacto | idem |

Nenhum teste existente foi modificado ou removido (`--numstat` do arquivo de teste: `+23/−0`). Os testes
intactos montam o tutor com `Fabrica.CriarUsuario(TipoPerfil.TUTOR)` (`Fabrica.cs` no commit), portanto não
passam pelo novo caminho de exceção. **Não executei a suíte nesta fatia**; a confirmação fica na subseção
abaixo.

### Diferenças entre o commit e o HEAD

| Aspecto | No commit `b22819d` | No `HEAD` |
| :-- | :-- | :-- |
| Corpo de `AdicionarPet` | guarda de perfil + `pet is null` + posse + duplicidade | **idêntico** (comparei os dois trechos com `diff`; saída vazia) |
| Linha de `AdicionarPet` | 50 | 58 |
| Chamadores de produção de `RegistrarPet` | 1 (só `UsuarioRepositorioEmMemoria`) | 2 (inclui `Ef/UsuarioRepositorio.cs:49`) |
| Rotas de pet | nenhuma | 5 em `PetEndpoints.cs` (GET, GET por id, POST, PUT, DELETE) |
| DTOs de pet | nenhum | `PetRequest.cs` e `PetResponse.cs` |
| Frontend | tela de cadastro de pet sem chamada de API | `petService.js` + `PetFormPage.jsx` chamando `POST /usuarios/{id}/pets` |

### Verificação com dotnet test (antes × depois)

**Ambiente.** Windows 10 (build 10.0.19045), shell Git Bash/MSYS, SDK .NET `10.0.400`. Execução em
15/11/2026, ~21:56–21:57. Dois *worktrees* criados **fora** do repositório e usados apenas para esta
verificação:

- `C:\Users\SARAH\Documents\coding\POO\PETWALK\PetWalk-revisao-antes` — commit pai `8871eef` ("antes")
- `C:\Users\SARAH\Documents\coding\POO\PETWALK\PetWalk-revisao-depois` — commit revisado `b22819d` ("depois")

Projeto de testes nos dois estados: `backend/Projeto.Tests/Projeto.Tests.csproj` (o caminho não mudou entre
os commits; conferido com `git ls-tree -r --name-only <hash> | grep "\.csproj$"`). Em cada worktree rodei
`dotnet restore` e depois dois `dotnet test`: a suíte completa e a focada nas classes levantadas em C04-d.

Nota de ambiente: o processo do shell não herda `APPDATA`/`ProgramFiles`, e sem elas o NuGet falha com
`error : Value cannot be null. (Parameter 'path1')` antes de compilar. Os comandos foram executados com essas
variáveis restauradas apenas no processo (mesma adaptação registrada em
`docs/av2/registro-de-depuracao.md`, seção E02). Nenhum arquivo de configuração foi alterado.

**Tabela antes × depois**

| Execução | Comando | Total | Aprovados | Falhas | Não executados | Tempo |
| :-- | :-- | --: | --: | --: | --: | --: |
| Antes — suíte completa | `dotnet test backend/Projeto.Tests/Projeto.Tests.csproj --logger "console;verbosity=normal"` | 118 | 118 | 0 | 0 | 7,9523 s |
| Antes — focada | `dotnet test backend/Projeto.Tests/Projeto.Tests.csproj --filter "FullyQualifiedName~UsuarioTests|FullyQualifiedName~UsuarioRepositorioTests" --logger "console;verbosity=normal"` | 17 | 17 | 0 | 0 | 1,4680 s |
| Depois — suíte completa | `dotnet test backend/Projeto.Tests/Projeto.Tests.csproj --logger "console;verbosity=normal"` | 120 | 120 | 0 | 0 | 6,1337 s |
| Depois — focada | `dotnet test backend/Projeto.Tests/Projeto.Tests.csproj --filter "FullyQualifiedName~UsuarioTests|FullyQualifiedName~UsuarioRepositorioTests" --logger "console;verbosity=normal"` | 19 | 19 | 0 | 0 | 1,6823 s |

**Saída literal dos resumos:**

```text
# ANTES (8871eef) — suíte completa
Execução de Teste Bem-sucedida.
Total de testes: 118
     Aprovados: 118
Tempo total: 7,9523 Segundos

# ANTES (8871eef) — focada
Execução de Teste Bem-sucedida.
Total de testes: 17
     Aprovados: 17
Tempo total: 1,4680 Segundos

# DEPOIS (b22819d) — suíte completa
Execução de Teste Bem-sucedida.
Total de testes: 120
     Aprovados: 120
Tempo total: 6,1337 Segundos

# DEPOIS (b22819d) — focada
Execução de Teste Bem-sucedida.
Total de testes: 19
     Aprovados: 19
Tempo total: 1,6823 Segundos
```

**Fatos (diferenças nos números e nas saídas).**

- O total sobe de **118 para 120** (+2) e a suíte focada de **17 para 19** (+2) — exatamente os dois métodos
  criados pelo commit (`AdicionarPet_ComPerfilDePasseador_LancaErro` e `AdicionarPet_ComPerfilAmbos_AdicionaOPet`).
- **Nenhuma falha** em nenhuma das quatro execuções, antes ou depois; nenhum teste mudou de vermelho para
  verde ou de verde para vermelho.
- **Nenhum teste não executado** (`Não executados: 0`): a limitação do App Control citada no README
  (`0x800711C7`, `FileLoadException`) **não se manifestou** neste ambiente para essas suítes — elas são de
  domínio/repositório em memória e não carregam o SQLite nativo. O `restore` e o `build` do commit pai
  **não falharam**.
- Os tempos caem de 7,9523 s para 6,1337 s (completa) e sobem de 1,4680 s para 1,6823 s (focada); variação
  de ambiente, sem relação com o diff.

**Leitura.**

- O "antes" passar **118/118** é o dado mais importante: a suíte anterior não detectava a ausência da regra.
  O commit **não corrige** algo que a suíte acusava — ele adiciona a **primeira cobertura** da regra em
  `AdicionarPet`, e é por isso que o total de testes aumenta exatamente na mesma medida que a suíte focada.
- Nenhum teste existente regrediu, o que é coerente com C04-d: os cenários pré-existentes montam o tutor com
  `TipoPerfil.TUTOR` e não passam pelo novo caminho de exceção. O risco de a guarda quebrar uso legítimo fica
  baixo, mas isso é **ausência de evidência contrária**, não prova de que não exista uso quebrado fora da
  suíte.
- O ganho de cobertura é local: nenhuma rota HTTP foi exercitada (no commit não há rota de pet), então o
  caminho "requisição → 422" continua sem verificação neste commit.

**Limitações (o que esta verificação NÃO prova).**

- As duas suítes são as **da época do commit** (14 arquivos de teste em `backend/Projeto.Tests`), **não** os
  291 casos de hoje; os números não são comparáveis ao `HEAD` nem ao README.
- **Não** rodei os dois testes novos contra o código do pai: isso exigiria alterar o worktree "antes", o que
  não estava autorizado. Portanto "o commit torna o critério C2 verdadeiro" está sustentado pelo diff e pela
  leitura do código, não por um teste vermelho no pai.
- Nada de frontend foi executado; nenhum endpoint foi chamado; o formato de erro HTTP não foi verificado.
- Os tempos incluem apenas a execução dos testes (o `dotnet test` compila antes, em etapa separada).

**Limpeza:** os dois worktrees foram removidos ao final — `git worktree remove` no "antes" e, no "depois",
`git worktree remove` desregistrou a árvore mas não apagou a pasta (o shell ainda a mantinha aberta);
`git worktree prune` + `rmdir` da pasta vazia concluíram a remoção. `git worktree list` mostra **apenas** o
repositório principal.

## C05 — Propriedades de POO

_A preencher na fatia correspondente._

## C06 — Parecer

_A preencher na fatia correspondente._
