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

_A preencher na fatia correspondente._

## C03 — Inventário dos arquivos alterados

_A preencher na fatia correspondente._

## C04 — Análise de impacto

_A preencher na fatia correspondente._

## C05 — Propriedades de POO

_A preencher na fatia correspondente._

## C06 — Parecer

_A preencher na fatia correspondente._
