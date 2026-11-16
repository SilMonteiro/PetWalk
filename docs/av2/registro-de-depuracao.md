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

**Diff do defeito** (`git diff`, 15/11/2026 ~21:39, SDK `10.0.400`):

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

Os três experimentos rodaram com o defeito de E01 aplicado, no mesmo ambiente de E02 (SDK `10.0.400`,
15/11/2026 ~21:49, mesmo prefixo de variáveis de ambiente), em um arquivo **temporário**
`backend/Projeto.Tests/DepuracaoTemporariaTests.cs` (removido na Fatia 5):

```bash
dotnet test backend/Projeto.Tests/Projeto.Tests.csproj \
  --filter "FullyQualifiedName~DepuracaoTemporariaTests" --logger "console;verbosity=detailed"
```

O projeto de testes enxerga os membros `internal` do domínio (`InternalsVisibleTo("Projeto.Tests")` em
`Projeto.Api.csproj`), então `passeador.AvaliacoesRecebidas` e `MediaAvaliacao` foram lidos diretamente.
Todos os passeios foram criados por `Fabrica.CriarPasseioSolicitadoPara` + `Aceitar`/`Iniciar`/`Finalizar`
e avaliados pelo tutor, com `Fabrica.Agora` como relógio.

### H1 — "`RegistrarAvaliacaoRecebida` usa só a última nota"

- **Esperado se verdadeira:** registrando 5, 1 e 3 em sequência, a média após cada passo seria 5, 1 e 3
  (a nota recém-registrada), com a contagem crescendo 1, 2, 3.
- **Esperado se falsa:** a média acompanharia o acumulado — 5, depois `(5+1)/2 = 3`, depois
  `(5+1+3)/3 = 3`.
- **Experimento:** um passeador, três passeios finalizados, um `Avaliar` por passeio; imprime contagem e
  média (`G17`, cultura invariante) após cada avaliação.

Saída real:

```text
 H1 nota=5 | avaliacoes=1 | media=5
 H1 nota=1 | avaliacoes=2 | media=1
 H1 nota=3 | avaliacoes=3 | media=3
```

**Veredito: confirmada.** A sequência observada (5, 1, 3) é exatamente a ordem das notas registradas, e
não o acumulado (5, 3, 3). A coleção cresce corretamente (1, 2, 3), mas o valor exposto ignora as
avaliações anteriores.

### H2 — "A coleção `_avaliacoesRecebidas` não acumula entre passeios do mesmo passeador"

- **Esperado se verdadeira:** `AvaliacoesRecebidas.Count` ficaria estagnado (0 ou 1) e a lista mostraria
  só a avaliação mais recente.
- **Esperado se falsa:** a contagem cresceria a cada avaliação e as notas anteriores continuariam na lista.
- **Experimento:** três passeios finalizados do mesmo passeador, todos avaliados com nota 5; imprime
  contagem, notas acumuladas e média.

Saída real:

```text
 H2 passo=1 | AvaliacoesRecebidas.Count=1 | notas=[5] | media=5
 H2 passo=2 | AvaliacoesRecebidas.Count=2 | notas=[5, 5] | media=5
 H2 passo=3 | AvaliacoesRecebidas.Count=3 | notas=[5, 5, 5] | media=5
```

**Veredito: descartada.** A coleção acumula (1 → 2 → 3) e retém as notas. O estado armazenado está
correto; o defeito está no valor derivado (`MediaAvaliacao`), não no armazenamento.

### H3 — "O problema é arredondamento ou tipo de dado"

- **Esperado se verdadeira:** com notas 5, 5 e 4 (média exata `14/3 = 4,666…`), o observado seria uma
  aproximação do cálculo — `4,67` (duas casas) ou `4,666…` — e a diferença para a média exata seria
  explicável por arredondamento/truncamento de `double`.
- **Esperado se falsa:** o observado seria exatamente a última nota (4), com diferença de `−2/3 ≈ −0,6667`
  para a média correta — grande demais para ser arredondamento.
- **Experimento:** notas 5, 5 e 4; imprime média exata, observada e diferença (`G17`).

Saída real:

```text
 H3 notas=[5, 5, 4] | media exata (14/3)=4.666666666666667 | observado=4
 H3 diferenca para a media exata=-0.66666666666666696
```

**Veredito: descartada.** O observado é `4` — a última nota —, não uma aproximação de `4,666…`. O resíduo
na 16ª casa (`…66696` em vez de `…667`) é aritmética de `double`, não arredondamento do domínio: `5`, `1`,
`3` e `4` são representados exatamente, e um truncamento do cálculo correto teria dado `4` só neste
cenário. O experimento de H1 elimina essa leitura: com notas 5 e 1 a média truncada seria `3`, e o
observado foi `1`.

**Conclusão de E03:** de três hipóteses, só H1 sobreviveu; H2 e H3 foram descartadas pelas saídas acima.
O comportamento é determinístico: `MediaAvaliacao` passa a valer a nota da avaliação recém-registrada, e o
histórico permanece intacto.

## E04 — Técnica de depuração

Foram preparadas duas técnicas. A **Técnica 1 (log temporário) foi executada**; a **Técnica 2 (breakpoint)**
está roteirizada para execução no IDE e **não foi executada** nesta automação.

### Técnica 1 — Log temporário (executada)

**Adaptação do plano:** o plano sugeria `ILogger` no ponto de cálculo. O domínio não recebe `ILogger` e
injetá-lo exigiria alterar construtores de `Usuario`/`Passeio` e o caminho de criação de entidades — mudança
fora do escopo desta depuração. A técnica foi adaptada: o log temporário é emitido **do teste**, via
`ITestOutputHelper`, no mesmo cenário de E02, sem tocar em produção.

**Experimento:** teste `T1_LogTemporario_RegistrarAvaliacaoRecebida` (arquivo temporário
`backend/Projeto.Tests/DepuracaoTemporariaTests.cs`, removido na Fatia 5): um passeador, dois passeios
finalizados, notas **5** e **1**; após cada `Avaliar`, imprime a nota registrada, a contagem de avaliações
recebidas e o valor de `MediaAvaliacao`.

**Comando** (15/11/2026 ~21:52, SDK `10.0.400`, mesmo prefixo de ambiente de E02):

```bash
dotnet test backend/Projeto.Tests/Projeto.Tests.csproj \
  --filter "FullyQualifiedName~T1_LogTemporario" --logger "console;verbosity=detailed"
```

**Saída real:**

```text
  Aprovado DepuracaoTemporariaTests.T1_LogTemporario_RegistrarAvaliacaoRecebida [29 ms]
  Mensagens de saída padrão:
 LOG passo=1 | avaliacao.Nota=5 | AvaliacoesRecebidas.Count=1 | media=5
 LOG passo=2 | avaliacao.Nota=1 | AvaliacoesRecebidas.Count=2 | media=1
 LOG notas acumuladas=[5, 1] | media esperada=(5+1)/2=3


Execução de Teste Bem-sucedida.
Total de testes: 1
     Aprovados: 1
```

**O que o log revelou:** na segunda parada a coleção já continha **duas** avaliações (`Count=2`,
`notas acumuladas=[5, 1]`), e ainda assim `media=1` — exatamente a nota recém-registrada. Ou seja, o valor
derivado não é função da coleção naquele momento: o acúmulo funciona e o que falha é a **atribuição**
dentro de `RegistrarAvaliacaoRecebida`. O log também mostra que o valor esperado seria `3` para as mesmas
duas notas, o que isola a divergência no cálculo, não nos dados.

### Técnica 2 — Breakpoint (roteiro para o usuário; não executada)

`docs/av2/img/` foi criada para receber o print. Roteiro no IDE:

1. Abrir `backend/Projeto.Api/Dominio/Usuario.cs`.
2. Localizar `RegistrarAvaliacaoRecebida` (linha 162) e marcar **breakpoint na linha 165** — a que atribui
   `MediaAvaliacao` (com o defeito aplicado: `MediaAvaliacao = avaliacao.Nota;`).
3. Abrir `backend/Projeto.Tests/MediaAvaliacaoTests.cs` e **depurar** o teste
   `MediaConsideraAvaliacoesDePasseiosDiferentesDoMesmoPasseador` (Debug Test / F5), não apenas executá-lo.
4. **Parada 1** (avaliação de nota 5): inspecionar `avaliacao.Nota`, `_avaliacoesRecebidas.Count` e
   `MediaAvaliacao` (Watch ou DataTip). Pela leitura do código, espera-se `5`, `1` e `5` — **não observado**.
5. Continuar a execução (F5) até a **parada 2** (avaliação de nota 1) e inspecionar os mesmos três valores.
   Pela leitura do código, espera-se `1`, `2` e `1` — **não observado**.
6. Comparar as duas paradas: verificar se `MediaAvaliacao` acompanha `avaliacao.Nota` ou
   `_avaliacoesRecebidas.Count`/a coleção.
7. Salvar o print do depurador (janela de Locals/Watch nas duas paradas) em
   `docs/av2/img/breakpoint-media.png`.

[EVIDÊNCIA DO DEPURADOR PENDENTE — a ser colada pelo usuário]

> Ao salvar o print em `docs/av2/img/breakpoint-media.png`, substituir o marcador acima por
> `![Breakpoint em Usuario.RegistrarAvaliacaoRecebida](img/breakpoint-media.png)` seguido da descrição do
> que foi observado nas duas paradas. Enquanto o print não existir, esta técnica permanece **não
> executada** e nada é afirmado sobre o que ela revelou.

### O que cada técnica revelou sobre a causa

| Técnica | Situação | O que revelou |
| :-- | :-- | :-- |
| Log temporário (T1) | **Executada** | Com a coleção já em `[5, 1]` (`Count=2`), `MediaAvaliacao` valeu `1`: o valor deriva da avaliação recém-registrada, não da coleção. O acúmulo está correto; a atribuição é que está errada. |
| Breakpoint | **Não executada** | Nada — aguardando o print em `docs/av2/img/breakpoint-media.png`. |

## E05 — Causa raiz e correção

> **Declaração de honestidade:** o defeito deste registro foi **deliberado**, introduzido por uma única
> linha em `debug/media-avaliacao` para demonstrar o processo de depuração (Opção B). Ele nunca existiu no
> histórico do projeto e não foi um bug encontrado em produção.

**Trecho defeituoso (antes da correção):**

```csharp
    // REG-001: a média considera todas as avaliações recebidas pelo usuário, não apenas as de um passeio.
    internal void RegistrarAvaliacaoRecebida(Avaliacao avaliacao)
    {
        _avaliacoesRecebidas.Add(avaliacao);
        MediaAvaliacao = avaliacao.Nota; // DEFEITO DELIBERADO (debug/media-avaliacao) — remover na correção
    }
```

**Causa raiz.** A média agregada é um valor **derivado de um conjunto**: precisa da média de *todas* as
avaliações recebidas. O código passou a derivá-la de um elemento só — a avaliação que acabou de chegar —
substituindo o agregado por um valor pontual a cada chamada. Por isso o resultado final é sempre a última
nota, independentemente do histórico: o conjunto é atualizado corretamente (`_avaliacoesRecebidas.Add`),
mas nunca é consultado. Não é arredondamento, não é tipo de dado e não é perda de dados (H2 e H3
descartadas em E03): é a regra de cálculo aplicada ao objeto errado.

**Localização.** `backend/Projeto.Api/Dominio/Usuario.cs`, método `Usuario.RegistrarAvaliacaoRecebida`
(linha 162), na atribuição de `MediaAvaliacao` — **linha 165**. O método é chamado por
`Passeio.Avaliar` (`Dominio/Passeio.cs:116`), que é o único caminho que registra avaliações no domínio.
A causa descreve *o que* está errado (agregado trocado por valor pontual); a localização aponta *onde* o
erro se materializa (uma linha, em um método). Corrigir o método não alteraria nada se a causa fosse outra,
e a linha sozinha não explica o sintoma sem a noção de agregado — as duas informações são necessárias.

**Menor correção coerente aplicada:** restaurar o cálculo sobre todas as avaliações recebidas e remover o
comentário do defeito. Nenhuma outra linha do projeto foi alterada.

```diff
diff --git a/backend/Projeto.Api/Dominio/Usuario.cs b/backend/Projeto.Api/Dominio/Usuario.cs
index 8603a19..c1a0d47 100644
--- a/backend/Projeto.Api/Dominio/Usuario.cs
+++ b/backend/Projeto.Api/Dominio/Usuario.cs
@@ -162,7 +162,7 @@ public class Usuario
     internal void RegistrarAvaliacaoRecebida(Avaliacao avaliacao)
     {
         _avaliacoesRecebidas.Add(avaliacao);
-        MediaAvaliacao = avaliacao.Nota; // DEFEITO DELIBERADO (debug/media-avaliacao) — remover na correção
+        MediaAvaliacao = _avaliacoesRecebidas.Count == 0 ? 0d : _avaliacoesRecebidas.Average(a => a.Nota);
     }
 
     // Sincroniza a média derivada com o valor recalculado no banco. Não valida nem cria
```

**Confirmação de retorno ao estado íntegro** — `git diff 32e5997a0bb16546f92501bfd74f88545b628a44 -- backend/Projeto.Api`
(base da Fatia 1), executado em 15/11/2026 ~21:55: **saída vazia** (nenhuma diferença em `backend/Projeto.Api`).

**Teste verde real** — `dotnet test backend/Projeto.Tests/Projeto.Tests.csproj --filter "FullyQualifiedName~MediaAvaliacaoTests"`,
15/11/2026 ~21:55, SDK `10.0.400`:

```text
  Projeto.Api -> C:\Users\SARAH\Documents\coding\POO\PETWALK\PetWalk\backend\Projeto.Api\bin\Debug\net10.0\Projeto.Api.dll
  Projeto.Tests -> C:\Users\SARAH\Documents\coding\POO\PETWALK\PetWalk\backend\Projeto.Tests\bin\Debug\net10.0\Projeto.Tests.dll
Execução de teste para C:\Users\SARAH\Documents\coding\POO\PETWALK\PetWalk\backend\Projeto.Tests\bin\Debug\net10.0\Projeto.Tests.dll (.NETCoreApp,Version=v10.0)
1 arquivos de teste no total corresponderam ao padrão especificado.

Aprovado!  – Com falha:     0, Aprovado:     2, Ignorado:     0, Total:     2, Duração: 49 ms - Projeto.Tests.dll (net10.0)
```

O mesmo teste que falhava com `Expected: 3 / Actual: 1` (E01) agora passa. A execução foi repetida **depois**
de apagar `DepuracaoTemporariaTests.cs` (56 ms, 2 aprovados), garantindo que o verde não dependia do arquivo
temporário.

**Limpeza:** `backend/Projeto.Tests/DepuracaoTemporariaTests.cs` foi apagado; nenhum arquivo `.cs` com esse
nome restou em `backend/`.

## E06 — Antes/depois e comparação de técnicas

### Antes/depois

Todos os valores abaixo vêm das saídas reais registradas em E01 (teste vermelho), E04 (log temporário) e
E05 (teste verde). Nada foi estimado.

| Cenário | Esperado | Observado ANTES (defeito) | Observado DEPOIS (correção) | Teste | Antes → depois |
| :-- | --: | --: | --: | :-- | :-- |
| Dois passeios finalizados do mesmo passeador, notas **5** e **1** | `3` | `1` | `3` | `MediaAvaliacaoTests.MediaConsideraAvaliacoesDePasseiosDiferentesDoMesmoPasseador` | vermelho (`Expected: 3` / `Actual: 1`) → verde |
| Passeador **sem avaliações** | `0` | `0` | `0` | `MediaAvaliacaoTests.PasseadorSemAvaliacoes_TemMediaZero` | verde → verde |

O log de E04 mostra o mesmo antes pelo lado do estado interno: no passo 2, com `notas acumuladas=[5, 1]` e
`Count=2`, `media=1`. Depois da correção o resumo foi `Com falha: 0, Aprovado: 2` (E05).

### Não regressão — suíte completa

Executada a partir de `backend/` (`cd backend && dotnet test`), SDK `10.0.400`, 15/11/2026 ~21:58:

```text
  Projeto.Api -> C:\Users\SARAH\Documents\coding\POO\PETWALK\PetWalk\backend\Projeto.Api\bin\Debug\net10.0\Projeto.Api.dll
  Projeto.Tests -> C:\Users\SARAH\Documents\coding\POO\PETWALK\PetWalk\backend\Projeto.Tests\bin\Debug\net10.0\Projeto.Tests.dll
Execução de teste para C:\Users\SARAH\Documents\coding\POO\PETWALK\PetWalk\backend\Projeto.Tests\bin\Debug\net10.0\Projeto.Tests.dll (.NETCoreApp,Version=v10.0)
1 arquivos de teste no total corresponderam ao padrão especificado.
[xUnit.net 00:00:22.83]     VerificacaoDeSchemaTests.BancoEmMemoria_NaoEhValidado [FAIL]
[xUnit.net 00:00:30.45]     VerificacaoDeSchemaTests.BancoComMigrationsAplicadas_NaoInterrompe [FAIL]

Com falha! – Com falha:     2, Aprovado:   289, Ignorado:     0, Total:   291, Duração: 28 s - Projeto.Tests.dll (net10.0)
```

**Comparação com o README.** O README (linhas 142, 165 e 167) registra **291 casos**, dos quais **120 não
foram executados** por bloqueio do App Control (`0x800711C7`) e **171 aprovados** na última execução
registrada. Nesta execução a divergência é:

| | README (registro anterior) | Esta execução |
| :-- | --: | --: |
| Total | 291 | 291 |
| Aprovados | 171 | **289** |
| Com falha | 0 | **2** |
| Não executados | **120** | **0** |

O bloqueio `0x800711C7` **não se repetiu**: `Ignorado: 0`, nenhum caso ficou sem executar. **Não é possível
afirmar que os 120 antes bloqueados passaram todos**, porque a lista nominal desses 120 não está no
repositório; o que se pode afirmar é que 289 de 291 passaram nesta máquina e que 2 falharam.

**As 2 falhas são pré-existentes e alheias a esta correção.** `backend/Projeto.Api` e
`backend/Projeto.Tests` estão **idênticos** ao commit base `32e5997` (`git diff` vazio para as duas pastas
depois da correção), e nenhuma das duas falhas toca média de avaliação:

1. `VerificacaoDeSchemaTests.BancoEmMemoria_NaoEhValidado` (linha 333) —
   `Assert.Null(GetConnectionString())` recebeu `"Data Source=:memory:"` (`Expected: null` / `Actual: "Data Source=:memory:"`).
2. `VerificacaoDeSchemaTests.BancoComMigrationsAplicadas_NaoInterrompe` (linha 197) —
   `Assert.Contains("_Inicial", coleção)` compara **elemento exato**, e a coleção contém
   `"20261111012746_Inicial"` (`Item not found in collection`).

**Registro honesto: a suíte não está 100% verde.** 2 casos falham e **não estão aprovados**; eles ficam fora
do escopo deste registro de depuração e devem ser tratados em item próprio.

### Comparação de técnicas

| Critério | Breakpoint | Log temporário (T1) | Teste automatizado |
| :-- | :-- | :-- | :-- |
| **Intrusividade** | **Técnica não executada** (print não fornecido). | Nenhuma mudança em produção: o log saiu do teste, via `ITestOutputHelper`. Exigiu um arquivo de teste extra, criado na Fatia 3 e apagado na Fatia 5. | Nenhuma em produção. O teste de domínio `MediaAvaliacaoTests` já existia e foi ele que ficou vermelho na Fatia 2, sem precisar escrever nada novo. |
| **Repetibilidade** | **Técnica não executada.** | Alta: comando único e determinístico (`--filter~T1_LogTemporario`), saída em texto colado em E04. | Total: o mesmo filtro rodou 3 vezes (vermelho em E01, verde em E05 e verde de novo após apagar o temporário). |
| **Custo/tempo** | **Técnica não executada.** | Baixo: um teste de 29 ms e ~5 s de build; o custo real foi escrever e depois apagar o arquivo. | Baixo e decrescente: o teste já existia; cada execução levou 49–56 ms depois do build. |
| **Adequação a código em produção** | **Técnica não executada.** | Baixa: é descartável por definição — o arquivo foi removido na Fatia 5 e não pode permanecer no repositório. | Alta: é o artefato que fica como proteção contra regressão (o mesmo teste que pegou o defeito continua no repositório). |

**Breakpoint: técnica não executada.** `docs/av2/img/` está vazia e o print
`docs/av2/img/breakpoint-media.png` não foi fornecido. Por isso nenhuma célula da coluna acima é comparada
por suposição e o marcador de E04 permanece pendente.

### Recomendação

Para este tipo de defeito — valor derivado errado, sem exceção nem erro de tipo — começar pelo **teste
automatizado**: foi ele que produziu a evidência mais barata e definitiva (`Expected: 3` / `Actual: 1`), que
já existia no repositório e que se converte naturalmente em proteção contra regressão, sem tocar em
produção. Quando o teste mostra o sintoma mas não o estado interno, migrar para o **log temporário**: foi o
que revelou que a coleção tinha `Count=2` e `[5, 1]` enquanto `MediaAvaliacao` valia `1`, apontando a falha
para a atribuição e não para o armazenamento — e o log deve ser descartado depois. O **breakpoint** fica
reservado para quando for necessário inspecionar estado privado em tempo de execução sem alterar o teste
(é o caso de `_avaliacoesRecebidas` e das duas paradas descritas em E04), mas nesta automação ele **não foi
executado**, então não há evidência para recomendá-lo com base em experiência própria: a recomendação acima
se apoia apenas nas duas técnicas efetivamente executadas.
