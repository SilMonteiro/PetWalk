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

_A preencher na fatia correspondente._

## C05 — Propriedades de POO

_A preencher na fatia correspondente._

## C06 — Parecer

_A preencher na fatia correspondente._
