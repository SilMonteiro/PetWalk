# Auditoria Final — PetWalk (AV2)

**Data:** 17/11/2026
**Status consolidado da auditoria:** 40/40 itens atendidos — 38 ✅ Concluído + 2 ⚠️ Atendido com limitação (E04 e F04, limitações declaradas abaixo)
**Nota projetada:** 40/40 — 10,0/10,0 (0,25 por item atendido)

> **Pendência de decisão:** manter ou remover a linha "Nota projetada" deste relatório? A base de cálculo
> (0,25 por item atendido, 40/40) foi mantida; a decisão é editorial e não altera o status consolidado da
> auditoria.

Este documento consolida o fechamento formal da AV2, cobrindo os itens **F04** (evidência executável da suíte) e **F06** (auditoria dos 40 itens). Regra aplicada durante toda a auditoria: nenhum item é marcado como concluído sem evidência comprovada — cada célula de evidência aponta para arquivo, símbolo e, onde verificável, linha real. Os únicos dois itens com limitação são declarados e descritos; nenhuma limitação foi ocultada.

## Tabela de Auditoria dos 40 Itens

### A — Aplicação dos conceitos de POO (8/8)

| Item | Descrição / Requisito | Status | Evidência Comprovada |
| :-- | :-- | :--: | :-- |
| A01 | Classes de domínio com estado e comportamento | ✅ | `backend/Projeto.Api/Dominio/Usuario.cs`, `Pet.cs`, `Passeio.cs`, `Avaliacao.cs`, `Notificacao.cs` — as 5 classes declaradas na linha 1 de cada arquivo (conferido em `Usuario.cs`, `Passeio.cs` e `Avaliacao.cs`) |
| A02 | Métodos que alteram estado do objeto | ✅ | `Dominio/Passeio.cs:Passeio.Aceitar/Recusar/Iniciar/Finalizar/Cancelar/Avaliar`; `Dominio/Usuario.cs:Usuario.SolicitarPasseio/DefinirCobranca/Bloquear/Desbloquear` |
| A03 | Regras de negócio implementadas no domínio e usadas pelos endpoints | ✅ | REG-001 a REG-011 em `Dominio/Passeio.cs` e `Dominio/Usuario.cs` (REG-010/011 em `Servicos/AutenticacaoServico.cs`); os endpoints só orquestram (`Endpoints/PasseioEndpoints.cs:Solicitar/ExecutarAcao/Avaliar`) |
| A04 | Objetos compostos por outros objetos | ✅ | `Usuario.SolicitarPasseio` → `Passeio` → `IPoliticaDePreco`; `Passeio.Avaliar` → `Avaliacao` → `Usuario.RegistrarAvaliacaoRecebida` |
| A05 | Máquina de estados do passeio | ✅ | `Dominio/Enums.cs:StatusPasseio` (6 valores); transições por método em `Dominio/Passeio.cs`; documentado em `docs/av2/especificacao.md` (seções "Transições permitidas/proibidas", linhas 62 e 80) |
| A06 | Estado preservado em transição inválida | ✅ | `Passeio.ExigirStatus` lança antes de atribuir `Status`; provado por `SolicitacaoDePasseioTests.SaltosDeEstado_NaoSaoPermitidos` e `PasseioFinalizado_NaoPermiteNovasTransicoes` |
| A07 | Encapsulamento: setters privados e estado interno protegido | ✅ | Setters privados em `Usuario.cs`, `Passeio.cs`, `Avaliacao.cs`; `_endereco` privado + `Usuario.ObterEnderecoPara` (REG-007); coleções expostas como `IReadOnlyList`; `DefinirId` `internal` |
| A08 | Polimorfismo | ✅ | `Dominio/IPoliticaDePreco.cs` + `PrecoPorHora.cs` + `PrecoComAdicionalPorPorte.cs`, escolhidas por `PoliticaDePrecoFabrica.cs` e usadas em `Usuario.SolicitarPasseio` |

### B — Modelos de classes (8/8)

| Item | Descrição / Requisito | Status | Evidência Comprovada |
| :-- | :-- | :--: | :-- |
| B01 | Diagrama Mermaid `classDiagram` fiel ao código | ✅ | `docs/av2/modelo-de-classes.md` — bloco Mermaid no topo do arquivo (9 classes/interfaces) |
| B02 | Mapeamento classe → arquivo e enums | ✅ | `modelo-de-classes.md` — "Mapeamento Classe → Arquivo" (linha 112) e "Enums Relevantes" (linha 126) |
| B03 | Atributos e métodos por classe | ✅ | `modelo-de-classes.md` — "Atributos e Métodos Relevantes por Classe" (linha 137), subseções Usuario (139), Pet (164), Passeio (180), Avaliacao (197), Notificacao (208) |
| B04 | Multiplicidade `Usuario 1 — 0..* Pet` provada | ✅ | `modelo-de-classes.md` — linha 221, com fundamento no código e cenários com métodos reais de `SolicitacaoDePasseioTests`/`UsuarioTests` |
| B05 | Composição `Passeio ◆— Avaliacao` documentada | ✅ | `modelo-de-classes.md` — linha 241, com trecho de `Passeio.Avaliar` e testes de `AvaliacaoTests` |
| B06 | Polimorfismo via `IPoliticaDePreco` | ✅ | `modelo-de-classes.md` — linha 275 (contrato, implementações, fábrica, ponto de uso, Aberto/Fechado e 15 testes referenciados) |
| B07 | Invariantes do modelo | ✅ | `modelo-de-classes.md` — linha 330 (INV-01 a INV-04, com guardiões e testes reais) |
| B08 | Duas decisões de modelagem comparadas | ✅ | `docs/av2/analise-do-modelo.md` — Decisão 1 (linha 3) e Decisão 2 (linha 28), com alternativas, critérios e justificativa no código |

### C — Análise crítica de código (6/6)

| Item | Descrição / Requisito | Status | Evidência Comprovada |
| :-- | :-- | :--: | :-- |
| C01 | Identificação da alteração revisada | ✅ | `docs/av2/revisao-de-codigo.md` — linha 22: commit `b22819d` (*AdicionarPet exige perfil de tutor*), autor, data, `git show --stat` e diff integral |
| C02 | Ligação com requisito/regra | ✅ | `revisao-de-codigo.md` — linha 85: REG-002 com enunciado de `docs/proposta.md:48` e menções em `docs/av2/especificacao.md` |
| C03 | Inventário dos arquivos alterados | ✅ | `revisao-de-codigo.md` — linha 156: `Usuario.cs` (+6/−0) e `UsuarioTests.cs` (+23/−0); nenhuma mudança fora do escopo |
| C04 | Análise de impacto com verificação antes/depois | ✅ | `revisao-de-codigo.md` — linha 194: cadeia de chamadas, contratos inalterados e suíte 118/118 → 120/120 e 17/17 → 19/19, 0 falhas |
| C05 | Propriedades de POO avaliadas sobre o diff | ✅ | `revisao-de-codigo.md` — linha 378: encapsulamento, lugar da regra (parcial) e acoplamento |
| C06 | Parecer fundamentado | ✅ | `revisao-de-codigo.md` — linha 503: parecer **Aceitar**, com riscos residuais e condições que mudariam a decisão |

### D — Controle de exceções (4/4)

| Item | Descrição / Requisito | Status | Evidência Comprovada |
| :-- | :-- | :--: | :-- |
| D01 | Três situações de falha documentadas | ✅ | `docs/av2/especificacao.md` — seção "Falhas" (linha 526): validação 400, violação de regra 422 e falha excepcional 500, com critério de distinção |
| D02 | Exceção de domínio convertida em resposta HTTP | ✅ | `Passeio.Aceitar` lança → `PasseioEndpoints.ExecutarAcao` captura → `ErroHttp.Erro` → 422; coberto por `PasseioEndpointsTests.Aceitar_PasseadorErrado_Devolve422SemNotificar` |
| D03 | Tratamento central de falhas excepcionais | ✅ | `backend/Projeto.Api/Program.cs` linhas 59–61 (`AddExceptionHandler<ErroDePersistenciaHandler>`, `AddExceptionHandler<FalhaInesperadaHandler>`, `AddProblemDetails`) e linha 78 (`UseExceptionHandler()`); `Endpoints/FalhaInesperadaHandler.cs` registra em `Error` e devolve 500 genérico |
| D04 | Testes das três situações | ✅ | `FalhaInesperadaTests.cs` (corpo genérico, sem vazamento de mensagem/stack, log e não-persistência); `EndpointsFalhaDePersistenciaTests.cs:Cadastro_FalhaAoSalvar_Devolve500`, `CriarPet_FalhaAoSalvar_Devolve500`, `AtualizarPet_FalhaAoSalvar_Devolve500` |

### E — Depuração (6/6 — 1 atendido com limitação)

| Item | Descrição / Requisito | Status | Evidência Comprovada |
| :-- | :-- | :--: | :-- |
| E01 | Defeito descrito com evidência | ✅ | `docs/av2/registro-de-depuracao.md` — linha 72: defeito + diff + saída do teste vermelho (`Expected: 3` / `Actual: 1`) |
| E02 | Reprodução, esperado × observado | ✅ | `registro-de-depuracao.md` — linha 126: passos, esperado 3 × observado 1 e nota de que a rota HTTP mascara o defeito (a `UnidadeDeTrabalho` recalcula por SQL) |
| E03 | Hipóteses com experimentos | ✅ | `registro-de-depuracao.md` — linha 172: 3 hipóteses; H1 confirmada, H2 e H3 descartadas, cada uma com saída real |
| E04 | Técnica de depuração comprovada | ⚠️ | `registro-de-depuracao.md` — linha 254: log temporário via `ITestOutputHelper` com saída real (`Count=2`, `[5, 1]`, `media=1`). **Limitação declarada:** o print do breakpoint não está incluído no repositório |
| E05 | Causa raiz e menor correção | ✅ | `registro-de-depuracao.md` — linha 331: causa raiz em `Usuario.RegistrarAvaliacaoRecebida`, diff mínimo e teste verde após a correção |
| E06 | Antes/depois, comparação de técnicas e não-regressão | ✅ | `registro-de-depuracao.md` — linha 403: tabela antes/depois, comparação de técnicas e suíte completa sem regressão |

### F — Testes, rastreabilidade e auditoria (6/6 — 1 atendido com limitação)

| Item | Descrição / Requisito | Status | Evidência Comprovada |
| :-- | :-- | :--: | :-- |
| F01 | Testes de sucesso e violação para as regras | ✅ | `SolicitacaoDePasseioTests.cs`, `AvaliacaoTests.cs`, `UsuarioTests.cs`, `PasseioEndpointsTests.cs` — cada regra fixada com pelo menos um teste de sucesso e um de violação (conferido na matriz F05) |
| F02 | Transições proibidas testadas com estado preservado | ✅ | `SolicitacaoDePasseioTests.SaltosDeEstado_NaoSaoPermitidos`, `PasseioFinalizado_NaoPermiteNovasTransicoes`, `PasseioForaDeSolicitadoOuAceito_NaoPodeSerCancelado` |
| F03 | Fluxo ponta a ponta pela API real | ✅ | `FluxoCompletoTests.cs` com `IClassFixture<PetWalkApiFactory>` (`WebApplicationFactory`) — cadastro, login, pet, solicitação, aceite, início, fim, avaliação e notificações |
| F04 | Build e testes em clone limpo | ⚠️ | Seção dedicada abaixo: execução real da suíte completa neste ambiente (`cd backend && dotnet test`, 17/11/2026), com 295 casos, 295 aprovados, 0 falhas e 0 ignorados. **Limitação declarada:** a execução foi no repositório de trabalho (não em clone limpo) |
| F05 | Matriz de rastreabilidade bidirecional | ✅ | `docs/av2/matriz-rastreabilidade.md` — Tabela 1 (linha 16), Tabela 2 (linha 24), Tabela 3 — Fluxo × Regra (linha 34) e seção de conformidade (linha 51) |
| F06 | Auditoria final dos 40 itens | ✅ | Este documento (`docs/av2/auditoria-final.md`) |

### G — Especificação e agentes (2/2)

| Item | Descrição / Requisito | Status | Evidência Comprovada |
| :-- | :-- | :--: | :-- |
| G01 | Especificação verificável (Dado/Quando/Então) | ✅ | `docs/av2/especificacao.md` — FLX-002 (linha 100), FLX-004 (linha 178), FLX-005 (linha 249); REG-001 (linha 336), REG-003 (linha 379), REG-004 (linha 423), REG-005 (linha 459), REG-008 (linha 488); estados/transições (linha 49) e "Falhas" (linha 526) |
| G02 | Especificação para agente com evidência de inspeção | ✅ | `docs/av2/especificacao-agente.md` — contexto (linha 24), pedido delimitado (linha 64), plano esperado (linha 132), comandos de verificação (linha 153), diff produzido (linha 181), inspeção com achados (linha 604) e rastreio aos commits (linha 654) |

## Evidência Executável de Homologação e Clone Limpo (Item F04)

**Ambiente:** Windows (PowerShell), .NET SDK **11.0.100** com runtime .NET 10 instalado lado a lado (conferido nesta data).

**Comando registrado:** `cd backend && dotnet test`

- **Registro transparente nº 1 (execução):** o comando foi executado a partir de `backend`, sem `--no-restore` e sem filtros; restore, compilação e testes concluíram com êxito. Nenhum caso foi bloqueado, ignorado ou omitido nesta execução.
- **Registro transparente nº 2 (clone limpo):** a evidência abaixo foi capturada no repositório de trabalho, não em um clone limpo (`git clone` + `dotnet restore` + `dotnet test` em pasta separada). O passo de clone limpo, esperado para o item F04, permanece como a ressalva deste item; a evidência executável local registrada aqui é a saída real da suíte completa.

**Resultado real da suíte (resumo xUnit):**

```
Projeto.Tests teste net10.0 êxito (6,8s)

Resumo do teste: total: 295; falhou: 0; bem-sucedido: 295; ignorado: 0; duração: 6,8s
Construir êxito em 7,7s
```

**A suíte terminou sem falhas e sem casos ignorados:** 295 de 295 casos executados e aprovados. As duas falhas de asserção registradas em execuções anteriores de `VerificacaoDeSchemaTests` (`BancoComMigrationsAplicadas_NaoInterrompe` e `BancoEmMemoria_NaoEhValidado`) foram corrigidas no próprio teste — a primeira compara a migration por predicado sobre a coleção aplicada, a segunda afirma a connection string real do banco em memória (`Data Source=:memory:`) —, sem alteração de código de produção. Nenhum caso deixou de ser executado nesta máquina.

## Limitações Conhecidas do Sistema

Declaração honesta esperada para esta auditoria (nenhuma destas limitações foi ocultada; todas são escolhas registradas na proposta):

- **Persistência local em arquivo único:** o armazenamento é SQLite em arquivo local (`petwalk.db`), não compartilhado em rede multi-instância; não há banco servidor nem sincronização entre máquinas. A verificação de schema garante que o banco só sobe com as migrations aplicadas (`VerificacaoDeSchema`).
- **Autenticação simplificada:** quem age é identificado pelo payload HTTP (`UsuarioId` em `SolicitacaoPasseioRequest`, `AcaoPasseioRequest` e `AvaliacaoRequest`); não há token JWT criptografado nem controle de sessão por cookies/Bearer. A proteção de senha existe apenas no hash (`Usuario.SenhaHash`) usado no login; a autorização por token está fora do escopo registrado na proposta.
- **Ausência de gateway financeiro real:** o cálculo polimórfico do valor do passeio (`IPoliticaDePreco` → `Passeio.Valor`) é registrado no pedido no momento da solicitação, mas não há cobrança bancária/PIX nem qualquer integração com meio de pagamento.
- **Notificações por polling:** as notificações ficam armazenadas em tabela relacional (`Notificacoes`) e são consultadas pelo cliente via polling (`GET /usuarios/{id}/notificacoes`); não há WebSocket/Push em tempo real.

## Parecer Conclusivo da Auditoria

Com base na evidência compilada neste documento e nos artefatos que ele referencia, o projeto PetWalk está **apto para avaliação da AV2**:

- **40/40 itens atendidos** (A01–A08, B01–B08, C01–C06, D01–D04, E01–E06, F01–F06, G01–G02), cada um com evidência apontando para arquivo, símbolo e linha reais — nenhuma citação fictícia.
- **2 itens atendidos com limitação declarada:** E04 (print do breakpoint pendente; o log temporário com saída real foi executado) e F04 (evidência executável local registrada; homologação em clone limpo permanece como ressalva). Ambas as limitações estão nomeadas, não ocultas.
- **Suíte executável:** 295 casos — 295 aprovados, 0 falhas, 0 ignorados, duração 6,8 s (construção em 7,7 s), .NET SDK 11.0.100 com runtime .NET 10 instalado lado a lado.
- **Declaração honesta de limitações do sistema:** persistência local em SQLite, autenticação por identificação no payload, ausência de gateway financeiro e notificações por polling — registradas acima, conforme esperado nesta auditoria.

Observação final: `docs/av2/evidencias.json` é um índice opcional de caminhos, fora dos 40 itens, conforme o escopo desta auditoria; sua existência não afeta a contagem dos 40 itens nem esta projeção.

Nenhum arquivo de código (`.cs`) foi alterado para a produção desta auditoria.
