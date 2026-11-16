# Registro de depuração — PetWalk (AV2)

Este documento registra uma sessão de depuração sobre o cálculo da **média de avaliação** do PetWalk:
o defeito, as condições de reprodução, o esperado × o observado, as hipóteses, a técnica de depuração
usada, a causa raiz com a correção e a comparação antes/depois. O objetivo é que qualquer pessoa com o
ambiente da seção **Ambiente** consiga repetir o defeito seguindo os passos de **E02** e ver a correção
de **E05** eliminá-lo.

## Método

**Opção B do plano (defeito deliberado)**, em uma branch dedicada: `debug/media-avaliacao`.

Motivo: o plano partia da premissa de que o histórico git do defeito real poderia não estar disponível, e
a Opção B demonstra o processo sem depender dele.

Ressalva honesta: a verificação do passo 3 da preparação mostrou que o commit `f57b1d1` ("Corrigir a
média de avaliação") **existe** no repositório (`git cat-file -t f57b1d1` → `commit`). A premissa que
justificava a Opção B não se confirmou. A Opção B foi mantida porque o objetivo é demonstrar o processo de
depuração, e não ler uma correção pronta: o commit **não** é usado como evidência, não sofreu checkout e
não foi exibido (`git show`) nesta etapa.

## Ambiente

| Item | Valor |
| :-- | :-- |
| SDK .NET | `10.0.400` (`dotnet --version`) |
| Commit base | `32e5997a0bb16546f92501bfd74f88545b628a44` (`git rev-parse HEAD`) |
| Assunto do commit base | `32e5997 Verificação cruzada e atualização de docs/plano-av2.md` |
| Branch de origem | `main` (`git rev-parse main` → mesmo hash do commit base) |
| Branch de trabalho | `debug/media-avaliacao`, criada a partir do commit base |
| Estado da árvore no início | limpa (`git status` → `nothing to commit, working tree clean`) |
| Data | 15/11/2026, ~21:35 |

## Onde a média é calculada

Existem **dois** pontos de cálculo, independentes:

1. **Domínio, em memória** — `Usuario.RegistrarAvaliacaoRecebida` recalcula a média com `Average` sobre a
   coleção de avaliações que veio carregada na requisição.
2. **Persistência, por SQL** — `UnidadeDeTrabalho.Salvar` recalcula com `AVG(Nota)` sobre a tabela
   `Avaliacoes` e sobrescreve `Usuarios.MediaAvaliacao` na mesma transação do `INSERT`; depois sincroniza
   o objeto em memória.

| Arquivo | Linhas | Papel no cálculo |
| :-- | --: | :-- |
| `backend/Projeto.Api/Dominio/Usuario.cs` | 16 | `MediaAvaliacao` (getter público, setter privado) |
| `backend/Projeto.Api/Dominio/Usuario.cs` | 162–166 | `RegistrarAvaliacaoRecebida(Avaliacao)` — ponto 1 |
| `backend/Projeto.Api/Dominio/Usuario.cs` | 165 | `MediaAvaliacao = _avaliacoesRecebidas.Average(a => a.Nota)` |
| `backend/Projeto.Api/Dominio/Usuario.cs` | 170 | `AtualizarMediaAvaliacao(double)` — reflexo do valor persistido |
| `backend/Projeto.Api/Dominio/Passeio.cs` | 104–119 | `Passeio.Avaliar(avaliador, nota, comentario, agora)` |
| `backend/Projeto.Api/Dominio/Passeio.cs` | 114–116 | cria a `Avaliacao` e chama `RegistrarAvaliacaoRecebida` |
| `backend/Projeto.Api/Repositorios/UnidadeDeTrabalho.cs` | 23–27 | coleta os avaliados com `Avaliacao` em estado `Added` |
| `backend/Projeto.Api/Repositorios/UnidadeDeTrabalho.cs` | 38–40 | abre a transação e executa o `SaveChanges()` |
| `backend/Projeto.Api/Repositorios/UnidadeDeTrabalho.cs` | 46–49 | `SELECT COALESCE(AVG(Nota), 0.0) ... FROM Avaliacoes WHERE AvaliadoId = ...` — ponto 2 |
| `backend/Projeto.Api/Repositorios/UnidadeDeTrabalho.cs` | 51–52 | `UPDATE Usuarios SET MediaAvaliacao = ... WHERE Id = ...` |
| `backend/Projeto.Api/Repositorios/UnidadeDeTrabalho.cs` | 56 | `avaliado.AtualizarMediaAvaliacao(media)` — resposta devolve a média persistida |

**Qual ponto cada teste exercita**

| Teste | Linha do teste | Caminho exercitado |
| :-- | --: | :-- |
| `MediaAvaliacaoTests.MediaConsideraAvaliacoesDePasseiosDiferentesDoMesmoPasseador` | 6 (asserção em 19: `3d`) | **Só o ponto 1** — domínio puro, sem `UnidadeDeTrabalho` |
| `MediaAvaliacaoTests.PasseadorSemAvaliacoes_TemMediaZero` | 23 (asserção em 27: `0d`) | **Só o ponto 1** — estado inicial da coleção |
| `MediaAvaliacaoConcorrenciaTests.MediaPersistida_NaoDependeDoHistoricoCarregado` | 79 (asserções em 100, 106, 111: `4d`) | **Ponto 2** — SQLite em memória + `Salvar()`; a coleção chega vazia |
| `MediaAvaliacaoConcorrenciaTests.AvaliacoesIntercaladasEmPasseiosDiferentes_NaoSobrescrevemAMedia` | 116 (asserções em 129, 130, 140, 144: `4d`) | **Ponto 2** — dois contextos concorrentes sobre a mesma base |

Consequência para a depuração: um defeito introduzido **apenas** no ponto 1 falha em
`MediaAvaliacaoTests` (domínio puro), mas **não** altera o valor persistido nem a resposta das rotas,
porque `UnidadeDeTrabalho` sobrescreve a média em memória com o `AVG` da tabela. Para que o defeito seja
observável de ponta a ponta, ele precisa atingir o valor persistido — o ponto 2 ou os dois pontos.

## E01 — Defeito e evidência

**Defeito:** `Usuario.RegistrarAvaliacaoRecebida` deixou de calcular a média acumulada e passou a atribuir
a **última nota recebida** a `MediaAvaliacao`. A avaliação continua sendo adicionada à coleção
(`_avaliacoesRecebidas.Add(avaliacao)`), mas o valor derivado passa a refletir apenas a avaliação mais
recente, contrariando a REG-001 ("a média considera todas as avaliações recebidas").

**Diff do defeito** (`git diff`, 15/11/2026 ~21:42, SDK `10.0.400`):

```diff
diff --git a/backend/Projeto.Api/Dominio/Usuario.cs b/backend/Projeto.Api/Dominio/Usuario.cs
index c1a0d47..8603a19 100644
--- a/backend/Projeto.Api/Dominio/Usuario.cs
+++ b/backend/Projeto.Api/Dominio/Usuario.cs
@@ -162,7 +162,7 @@ public class Usuario
     internal void RegistrarAvaliacaoRecebida(Avaliacao avaliacao)
     {
         _avaliacoesRecebidas.Add(avaliacao);
-        MediaAvaliacao = _avaliacoesRecebidas.Count == 0 ? 0d : _avaliacoesRecebidas.Average(a => a.Nota);
+        MediaAvaliacao = avaliacao.Nota; // DEFEITO DELIBERADO (debug/media-avaliacao) — remover na correção
     }
 
     // Sincroniza a média derivada com o valor recalculado no banco. Não valida nem cria
```

**Evidência do comportamento incorreto** — saída completa de
`dotnet test backend/Projeto.Tests/Projeto.Tests.csproj --filter "FullyQualifiedName~MediaAvaliacaoTests"`,
executada em 15/11/2026 ~21:43 (hora local) com SDK `10.0.400`:

```text
  Determinando os projetos a serem restaurados...
  Todos os projetos estão atualizados para restauração.
  Projeto.Api -> C:\Users\SARAH\Documents\coding\POO\PETWALK\PetWalk\backend\Projeto.Api\bin\Debug\net10.0\Projeto.Api.dll
  Projeto.Tests -> C:\Users\SARAH\Documents\coding\POO\PETWALK\PetWalk\backend\Projeto.Tests\bin\Debug\net10.0\Projeto.Tests.dll
Execução de teste para C:\Users\SARAH\Documents\coding\POO\PETWALK\PetWalk\backend\Projeto.Tests\bin\Debug\net10.0\Projeto.Tests.dll (.NETCoreApp,Version=v10.0)
1 arquivos de teste no total corresponderam ao padrão especificado.
[xUnit.net 00:00:03.80]     MediaAvaliacaoTests.MediaConsideraAvaliacoesDePasseiosDiferentesDoMesmoPasseador [FAIL]
  Com falha MediaAvaliacaoTests.MediaConsideraAvaliacoesDePasseiosDiferentesDoMesmoPasseador [206 ms]
  Mensagem de erro:
   Assert.Equal() Failure: Values differ
Expected: 3
Actual:   1
  Rastreamento de pilha:
     at MediaAvaliacaoTests.MediaConsideraAvaliacoesDePasseiosDiferentesDoMesmoPasseador() in C:\Users\SARAH\Documents\coding\POO\PETWALK\PetWalk\backend\Projeto.Tests\MediaAvaliacaoTests.cs:line 19
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Com falha! – Com falha:     1, Aprovado:     1, Ignorado:     0, Total:     2, Duração: 242 ms - Projeto.Tests.dll (net10.0)
```

O segundo teste do filtro, `MediaAvaliacaoTests.PasseadorSemAvaliacoes_TemMediaZero`, **passa** com o
defeito: sem nenhuma avaliação recebida, `MediaAvaliacao` permanece `0d` (valor padrão). O defeito só
aparece quando existe mais de uma avaliação.

## E02 — Reprodução, esperado e observado

**Dados do cenário** (montados em memória pela `Fabrica`, sem banco de dados): um passeador e **dois
passeios finalizados** com ele, cada um com um tutor próprio. O primeiro passeio é avaliado com nota
**5** e o segundo com nota **1** — média correta `(5 + 1) / 2 = 3,0`.

**Passos de reprodução**

1. `git switch debug/media-avaliacao` — a branch parte do commit `32e5997` (o mesmo de `main`).
2. Aplicar o defeito de E01 em `backend/Projeto.Api/Dominio/Usuario.cs`: apenas a linha 165 muda
   (`MediaAvaliacao = avaliacao.Nota;` + o comentário `// DEFEITO DELIBERADO (debug/media-avaliacao) — remover na correção`).
   `git diff` deve mostrar **uma** linha alterada.
3. Rodar o teste de domínio:
   `dotnet test backend/Projeto.Tests/Projeto.Tests.csproj --filter "FullyQualifiedName~MediaAvaliacaoTests"`
4. Ler a falha em `MediaAvaliacaoTests.cs:19`.
5. Desfazer a linha (voltar para `_avaliacoesRecebidas.Count == 0 ? 0d : _avaliacoesRecebidas.Average(a => a.Nota)`)
   e repetir o comando: os dois testes passam. É a prova de que o defeito é a causa.

**Pré-requisito de ambiente (deste ciclo):** o processo que executa os testes aqui foi iniciado sem
`APPDATA`, `ProgramFiles` e `ProgramFiles(x86)`. Sem elas, o NuGet falha **antes** do teste com
`error : Value cannot be null. (Parameter 'path1')` (reproduzível em `dotnet nuget list source`), e o
`--no-restore` falha ao ler `project.assets.json` com o mesmo erro. A execução acima usou as variáveis
restauradas somente no processo:

```bash
env 'APPDATA=C:\Users\SARAH\AppData\Roaming' 'ProgramFiles=C:\Program Files' \
    'ProgramFiles(x86)=C:\Program Files (x86)' 'CommonProgramFiles=C:\Program Files\Common Files' \
    'ALLUSERSPROFILE=C:\ProgramData' \
    dotnet test backend/Projeto.Tests/Projeto.Tests.csproj --filter "FullyQualifiedName~MediaAvaliacaoTests"
```

Isso é configuração de ambiente, não código: nenhum arquivo de projeto, `NuGet.Config` ou `Directory.Build.*`
foi alterado (não existe `Directory.Build.*` no repositório e o `NuGet.Config` do usuário só declara `nuget.org`).

**Comportamento esperado:** `MediaAvaliacao == 3,0` depois das notas 5 e 1.

**Comportamento observado:** `MediaAvaliacao == 1` — a última nota sobrescreveu a média. Saída real:
`Expected: 3` / `Actual: 1`, resumo `Com falha: 1, Aprovado: 1, Ignorado: 0, Total: 2`.

> **Por leitura do código, não executado:** o defeito no domínio fica **mascarado na rota da API**.
> `UnidadeDeTrabalho.Salvar` recalcula a média por SQL (`SELECT COALESCE(AVG(Nota), 0.0) FROM Avaliacoes`)
> e sobrescreve o objeto em memória com `AtualizarMediaAvaliacao(media)` na mesma transação do `INSERT`;
> uma requisição `POST /passeios/{id}/avaliacoes` continuaria devolvendo a média correta. Por isso a
> evidência adotada é o teste de domínio `MediaAvaliacaoTests` (que não passa por `UnidadeDeTrabalho`),
> e não uma requisição HTTP. Nenhuma requisição HTTP foi executada nesta etapa.

## E03 — Hipóteses e experimentos

<!-- a preencher na Fatia N -->

## E04 — Técnica de depuração

<!-- a preencher na Fatia N -->

## E05 — Causa raiz e correção

<!-- a preencher na Fatia N -->

## E06 — Antes/depois e comparação de técnicas

<!-- a preencher na Fatia N -->
