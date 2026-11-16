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

<!-- a preencher na Fatia N -->

## E02 — Reprodução, esperado e observado

<!-- a preencher na Fatia N -->

## E03 — Hipóteses e experimentos

<!-- a preencher na Fatia N -->

## E04 — Técnica de depuração

<!-- a preencher na Fatia N -->

## E05 — Causa raiz e correção

<!-- a preencher na Fatia N -->

## E06 — Antes/depois e comparação de técnicas

<!-- a preencher na Fatia N -->
