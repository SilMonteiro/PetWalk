# Matriz de Rastreabilidade Bidirecional — PetWalk (AV2)

Documento exigido pelo item **F05** da AV2: cruza os 3 fluxos e as 5 regras de negócio fixadas em [`docs/av2/especificacao.md`](especificacao.md) com suas **implementações reais** (`backend/Projeto.Api/`) e seus **testes automatizados** (`backend/Projeto.Tests/`).

A rastreabilidade é bidirecional: de cada fluxo/regra parte um ponteiro para o código e para os testes que o comprovam; e de cada teste é possível voltar ao requisito que ele cobre. A matriz cruzada (Tabela 3) reproduz a relação Fluxo × Regra da especificação, que por sua vez foi conferida no código.

## Convenções adotadas

- **Formato das citações de código:** `arquivo:Classe.Método`, com caminho relativo à raiz do repositório. O arquivo é citado pelo nome (ex.: `Passeio.cs`) porque todos os arquivos aqui citados estão em `backend/Projeto.Api/Dominio/` ou `backend/Projeto.Api/Endpoints/`.
- **Formato das citações de teste:** `arquivo:Classe.Método`, com `arquivo` relativo à raiz (ex.: `backend/Projeto.Tests/PasseioEndpointsTests.cs:PasseioEndpointsTests.Solicitar_GravaOPasseioENotificaOPasseador`).
- **Guardian of the rule:** a operação de domínio que aplica a regra é o guardião; os testes que a provam podem exercitá-la pela via do domínio (direta) ou pela via do endpoint (integração), e as duas vias estão identificadas.
- **Métodos `internal`/`private`:** `PasseioEndpoints.Solicitar`, `ExecutarAcao` e `Avaliar` são `internal` (exercitados diretamente pelos testes, sem subir a API); `Passeio.ExigirPasseadorAtivo` e `Passeio.ExigirStatus` são `private` (guardianas internas, citadas como local da regra). A menção aqui documenta o ponto exato do código, não sua visibilidade.
- **Nomes:** todos os nomes de classe, método e teste citados foram conferidos um a um contra o código nesta data; nenhum nome é fictício (ver [Conformidade das citações](#conformidade-das-citacoes)).
- **Status HTTP:** os valores listados são os do contrato fixado em `especificacao.md` ("Falhas"): `ArgumentException` → **400**, `InvalidOperationException` → **422**, recurso ausente → **404** — mapeamento único feito por `ErroHttp.cs`.

## Tabela 1 — Rastreabilidade dos 3 Fluxos

| ID | Nome do Fluxo | Ator | Operação de Domínio (arquivo:Classe.Método) | Rota HTTP / Endpoint (arquivo:Método) | Testes de Integração / E2E (arquivo:Classe.Método) |
| :-- | :-- | :-- | :-- | :-- | :-- |
| FLX-002 | Solicitar passeio | Tutor | `Dominio/Usuario.cs:Usuario.SolicitarPasseio` (valida REG-002, REG-005, REG-008, REG-009 e cria o `Passeio` em SOLICITADO com `Valor` calculado pela `PoliticaDePrecoFabrica`); `Dominio/Passeio.cs:construtor de Passeio` | `POST /passeios` — `Endpoints/PasseioEndpoints.cs:PasseioEndpoints.MapPasseioEndpoints` (`MapPost`) e `PasseioEndpoints.Solicitar` (201 / 404 / 422) | `PasseioEndpointsTests.cs:PasseioEndpointsTests.Solicitar_GravaOPasseioENotificaOPasseador`, `Solicitar_Resposta_NaoVazaEnderecoDoPerfil`, `Solicitar_PetDeOutroTutor_Devolve422ComAMensagemDaREG005`; `SolicitacaoDePasseioTests.cs:SolicitacaoDePasseioTests.TutorComPetVinculado_SolicitaPasseio_CriaPasseioSolicitado`; `FluxoCompletoTests.cs:FluxoCompletoTests.FluxoCompleto_DoCadastroAteAvaliacaoENotificacao` (etapa FLX-002, pela API real) |
| FLX-004 | Finalizar passeio | Passeador | `Dominio/Passeio.cs:Passeio.Finalizar` (chama `Passeio.ExigirPasseadorAtivo` — REG-003 — e `Passeio.ExigirStatus(EM_ANDAMENTO)`) | `POST /passeios/{id}/finalizar` — `Endpoints/PasseioEndpoints.cs:PasseioEndpoints.MapPasseioEndpoints` (`MapPost`) e `PasseioEndpoints.ExecutarAcao` (200 / 404 / 422) | `PasseioEndpointsTests.cs:PasseioEndpointsTests.Finalizar_EmAndamento_NotificaOTutor`; `SolicitacaoDePasseioTests.cs:SolicitacaoDePasseioTests.CicloCompleto_SolicitadoAceitoEmAndamentoFinalizado`, `UsuarioQueNaoEhOPasseador_NaoPodeIniciarNemFinalizar`, `PasseadorBloqueado_NaoPodeAceitarIniciarNemFinalizar`, `PasseioFinalizado_NaoPermiteNovasTransicoes`; `FluxoCompletoTests.cs:FluxoCompletoTests.FluxoCompleto_DoCadastroAteAvaliacaoENotificacao` (etapa FLX-004, pela API real) |
| FLX-005 | Avaliar passeador | Tutor | `Dominio/Passeio.cs:Passeio.Avaliar` (duplicidade — REG-001 — e criação); `Dominio/Avaliacao.cs:construtor de Avaliacao` (FINALIZADO, nota 1–5, participação); `Dominio/Usuario.cs:Usuario.RegistrarAvaliacaoRecebida` (recálculo da média) | `POST /passeios/{id}/avaliacoes` — `Endpoints/PasseioEndpoints.cs:PasseioEndpoints.MapPasseioEndpoints` (`MapPost`) e `PasseioEndpoints.Avaliar` (201 / 400 / 404 / 422) | `AvaliacaoTests.cs:AvaliacaoTests.TutorAvaliaPasseador_RegistraAvaliacaoERecalculaMedia`, `PasseioNaoFinalizado_NaoPodeSerAvaliado`, `MesmoAvaliadorNaoPodeAvaliarDuasVezes`, `NotaForaDoIntervalo_NaoPodeSerAvaliada`; `PasseioEndpointsTests.cs:PasseioEndpointsTests.Avaliar_PasseioFinalizado_Devolve201ComAMediaAtualizada`, `Avaliar_PasseioNaoFinalizado_Devolve422`, `Avaliar_DuasVezes_Devolve422`, `Avaliar_NotaForaDoIntervalo_Devolve400`; `FluxoCompletoTests.cs:FluxoCompletoTests.FluxoCompleto_DoCadastroAteAvaliacaoENotificacao` (etapa FLX-005, pela API real) |

## Tabela 2 — Rastreabilidade das 5 Regras de Negócio

| ID | Enunciado Resumido | Guardião no Domínio (arquivo:Classe.Método) | Teste de Sucesso (arquivo:Classe.Método) | Teste de Violação / Exceção (arquivo:Classe.Método) | Status HTTP na API |
| :-- | :-- | :-- | :-- | :-- | :-- |
| REG-001 | Avaliação só com passeio FINALIZADO, uma vez por avaliador, nota 1–5 | `Dominio/Passeio.cs:Passeio.Avaliar` (duplicidade) e `Dominio/Avaliacao.cs:construtor de Avaliacao` (FINALIZADO, nota 1–5, participação) | `AvaliacaoTests.cs:AvaliacaoTests.TutorAvaliaPasseador_RegistraAvaliacaoERecalculaMedia`, `AmbosAvaliam_AsMediasSaoIndividuais`; `PasseioEndpointsTests.cs:PasseioEndpointsTests.Avaliar_PasseioFinalizado_Devolve201ComAMediaAtualizada` | `AvaliacaoTests.cs:AvaliacaoTests.PasseioNaoFinalizado_NaoPodeSerAvaliado`, `MesmoAvaliadorNaoPodeAvaliarDuasVezes`, `NotaForaDoIntervalo_NaoPodeSerAvaliada`; `PasseioEndpointsTests.cs:PasseioEndpointsTests.Avaliar_PasseioNaoFinalizado_Devolve422`, `Avaliar_DuasVezes_Devolve422`, `Avaliar_NotaForaDoIntervalo_Devolve400`; `FluxoCompletoTests.cs:FluxoCompletoTests.AvaliarDuasVezes_Devolve422` | 201 (sucesso); 422 (não finalizado / duplicada); 400 (nota 0 ou 6) |
| REG-003 | Só o passeador escolhido, com perfil de passeador e ATIVO, aceita, recusa, inicia ou finaliza | `Dominio/Passeio.cs:Passeio.ExigirPasseadorAtivo` (chamado por `Passeio.Aceitar`, `Recusar`, `Iniciar`, `Finalizar`) | `SolicitacaoDePasseioTests.cs:SolicitacaoDePasseioTests.PasseadorEscolhido_AceitaPasseio`, `PasseadorEscolhido_RecusaPasseio`, `CicloCompleto_SolicitadoAceitoEmAndamentoFinalizado`; `PasseioEndpointsTests.cs:PasseioEndpointsTests.Aceitar_PasseadorCerto_NotificaOTutor`, `Iniciar_AposAceite_NotificaOTutor`, `Finalizar_EmAndamento_NotificaOTutor` | `SolicitacaoDePasseioTests.cs:SolicitacaoDePasseioTests.UsuarioQueNaoEhOPasseador_NaoPodeAceitar`, `UsuarioQueNaoEhOPasseador_NaoPodeRecusar`, `UsuarioQueNaoEhOPasseador_NaoPodeIniciarNemFinalizar`, `PasseadorBloqueado_NaoPodeRecusar`, `PasseadorBloqueado_NaoPodeAceitarIniciarNemFinalizar`; `PasseioEndpointsTests.cs:PasseioEndpointsTests.Aceitar_PasseadorErrado_Devolve422SemNotificar` | 200 (sucesso); 422 (não é o escolhido / sem perfil / bloqueado) |
| REG-004 | Cancelamento só em SOLICITADO ou ACEITO | `Dominio/Passeio.cs:Passeio.Cancelar` (verifica `Participou(quem)` e depois o estado) | `SolicitacaoDePasseioTests.cs:SolicitacaoDePasseioTests.TutorEPasseador_CancelamPasseioSolicitadoOuAceito`; `PasseioEndpointsTests.cs:PasseioEndpointsTests.Cancelar_PeloTutor_NotificaOPasseador` | `SolicitacaoDePasseioTests.cs:SolicitacaoDePasseioTests.PasseioForaDeSolicitadoOuAceito_NaoPodeSerCancelado`, `UsuarioQueNaoParticipou_NaoPodeCancelar`; `PasseioEndpointsTests.cs:PasseioEndpointsTests.Cancelar_EmAndamento_Devolve422SemNotificar` | 200 (sucesso); 422 (estado inválido / não participou) |
| REG-005 | O pet do passeio pertence ao tutor solicitante | `Dominio/Usuario.cs:Usuario.SolicitarPasseio` (verifica `pet is null \|\| !_pets.Contains(pet)`); o endpoint só procura o pet em `tutor.Pets` | `SolicitacaoDePasseioTests.cs:SolicitacaoDePasseioTests.TutorComPetVinculado_SolicitaPasseio_CriaPasseioSolicitado`; `PasseioEndpointsTests.cs:PasseioEndpointsTests.Solicitar_GravaOPasseioENotificaOPasseador` | `SolicitacaoDePasseioTests.cs:SolicitacaoDePasseioTests.PetDeOutroTutor_NaoPodeSerSolicitado`; `PasseioEndpointsTests.cs:PasseioEndpointsTests.Solicitar_PetDeOutroTutor_Devolve422ComAMensagemDaREG005` | 201 (sucesso); 422 (`"O pet informado não pertence ao tutor solicitante"`) |
| REG-008 | Data e hora do passeio futuras no momento da solicitação | `Dominio/Usuario.cs:Usuario.SolicitarPasseio` (compara `dataHora.ToUniversalTime() <= agora.ToUniversalTime()` contra o relógio injetado `agora`) | `SolicitacaoDePasseioTests.cs:SolicitacaoDePasseioTests.TutorComPetVinculado_SolicitaPasseio_CriaPasseioSolicitado`; `PasseioEndpointsTests.cs:PasseioEndpointsTests.Solicitar_GravaOPasseioENotificaOPasseador` | `SolicitacaoDePasseioTests.cs:SolicitacaoDePasseioTests.DataNoPassado_NaoPodeSerSolicitada`, `DataIgualAAgora_NaoPodeSerSolicitada` | 201 (sucesso); 422 (`"A data e hora do passeio devem ser futuras"`) |

## Tabela 3 — Matriz Cruzada Fluxo × Regra

Reproduz a matriz fixada em `docs/av2/especificacao.md` (seção "Fluxo × regras"); "sim" indica que o fluxo exercita a regra.

| Fluxo | REG-001 | REG-003 | REG-004 | REG-005 | REG-008 |
| :-- | :--: | :--: | :--: | :--: | :--: |
| FLX-002 | — | — | — | sim | sim |
| FLX-004 | — | sim | — | — | — |
| FLX-005 | sim | — | — | — | — |

Leituras da matriz (conferidas no código):

- **FLX-002** — `Usuario.SolicitarPasseio` aplica REG-005 (`!_pets.Contains(pet)`) e REG-008 (data futura); a mesma operação aplica REG-002 e REG-009, e respeita a REG-007.
- **FLX-004** — `Passeio.Finalizar` chama `ExigirPasseadorAtivo` (REG-003) e exige EM_ANDAMENTO via `ExigirStatus`.
- **FLX-005** — `Passeio.Avaliar` e o construtor de `Avaliacao` aplicam REG-001; a participação (REG-006) também é verificada, mas está fora das cinco.
- **REG-004** — não é exercitada por nenhum dos três fluxos do escopo (o cancelamento é o FLX-009); permanece especificada por restringir uma transição do ciclo de vida, e seus testes estão na Tabela 2.

## Conformidade das citações

Todos os métodos e testes citados neste documento foram verificados um a um contra o código em `backend/Projeto.Api/` e `backend/Projeto.Tests/`. Nenhum nome é fictício.

### Implementação (`backend/Projeto.Api/`) — verificado

| Citação | Onde foi verificada | Status |
| :-- | :-- | :--: |
| `Usuario.SolicitarPasseio` | `Dominio/Usuario.cs` — valida perfil de tutor, posse do pet, perfil/status do passeador e data futura; cria o `Passeio` com a política da fábrica | ✓ |
| `Passeio.Finalizar` | `Dominio/Passeio.cs` — `ExigirPasseadorAtivo(quem)` + `ExigirStatus(EM_ANDAMENTO)` | ✓ |
| `Passeio.ExigirPasseadorAtivo` | `Dominio/Passeio.cs` (método privado) — mensagens `"Passeio não pertence a este passeador"`, `"Usuário não possui perfil de passeador"`, `"Usuário está bloqueado"` | ✓ |
| `Passeio.ExigirStatus` | `Dominio/Passeio.cs` (método privado) — `"Transição não permitida a partir do estado atual"` | ✓ |
| `Passeio.Cancelar` | `Dominio/Passeio.cs` — `Participou(quem)` + exigência de SOLICITADO/ACEITO com mensagem própria | ✓ |
| `Passeio.Avaliar` | `Dominio/Passeio.cs` — loop de duplicidade + `new Avaliacao(...)` + `RegistrarAvaliacaoRecebida` | ✓ |
| Construtor de `Avaliacao` | `Dominio/Avaliacao.cs` — valida FINALIZADO, nota 1–5 e participação, nesta ordem | ✓ |
| `Usuario.RegistrarAvaliacaoRecebida` | `Dominio/Usuario.cs` (método `internal`) — recálculo da média com todas as avaliações recebidas | ✓ |
| `PasseioEndpoints.MapPasseioEndpoints` | `Endpoints/PasseioEndpoints.cs` — `MapPost("/passeios")`, `MapPost("/passeios/{id:int}/finalizar")`, `MapPost("/passeios/{id:int}/avaliacoes")` | ✓ |
| `PasseioEndpoints.Solicitar` / `ExecutarAcao` / `Avaliar` | `Endpoints/PasseioEndpoints.cs` (métodos `internal`) | ✓ |
| `ErroHttp.Erro` / `ErroHttp.NaoEncontrado` | `Endpoints/ErroHttp.cs` — `ArgumentException` → 400; `InvalidOperationException` → 422; recurso ausente → 404 | ✓ |

### Testes (`backend/Projeto.Tests/`) — verificado

| Classe | Métodos citados | Status |
| :-- | :-- | :--: |
| `FluxoCompletoTests` | `FluxoCompleto_DoCadastroAteAvaliacaoENotificacao`, `AvaliarDuasVezes_Devolve422` | ✓ |
| `PasseioEndpointsTests` | `Solicitar_GravaOPasseioENotificaOPasseador`, `Solicitar_Resposta_NaoVazaEnderecoDoPerfil`, `Solicitar_PetDeOutroTutor_Devolve422ComAMensagemDaREG005`, `Aceitar_PasseadorCerto_NotificaOTutor`, `Aceitar_PasseadorErrado_Devolve422SemNotificar`, `Iniciar_AposAceite_NotificaOTutor`, `Finalizar_EmAndamento_NotificaOTutor`, `Cancelar_PeloTutor_NotificaOPasseador`, `Cancelar_EmAndamento_Devolve422SemNotificar`, `Avaliar_PasseioFinalizado_Devolve201ComAMediaAtualizada`, `Avaliar_PasseioNaoFinalizado_Devolve422`, `Avaliar_DuasVezes_Devolve422`, `Avaliar_NotaForaDoIntervalo_Devolve400` | ✓ |
| `AvaliacaoTests` | `TutorAvaliaPasseador_RegistraAvaliacaoERecalculaMedia`, `AmbosAvaliam_AsMediasSaoIndividuais`, `PasseioNaoFinalizado_NaoPodeSerAvaliado`, `MesmoAvaliadorNaoPodeAvaliarDuasVezes`, `NotaForaDoIntervalo_NaoPodeSerAvaliada` | ✓ |
| `SolicitacaoDePasseioTests` | `TutorComPetVinculado_SolicitaPasseio_CriaPasseioSolicitado`, `PetDeOutroTutor_NaoPodeSerSolicitado`, `DataNoPassado_NaoPodeSerSolicitada`, `DataIgualAAgora_NaoPodeSerSolicitada`, `PasseadorEscolhido_AceitaPasseio`, `PasseadorEscolhido_RecusaPasseio`, `CicloCompleto_SolicitadoAceitoEmAndamentoFinalizado`, `UsuarioQueNaoEhOPasseador_NaoPodeAceitar`, `UsuarioQueNaoEhOPasseador_NaoPodeRecusar`, `UsuarioQueNaoEhOPasseador_NaoPodeIniciarNemFinalizar`, `PasseadorBloqueado_NaoPodeRecusar`, `PasseadorBloqueado_NaoPodeAceitarIniciarNemFinalizar`, `TutorEPasseador_CancelamPasseioSolicitadoOuAceito`, `PasseioForaDeSolicitadoOuAceito_NaoPodeSerCancelado`, `UsuarioQueNaoParticipou_NaoPodeCancelar` | ✓ |

### Divergências resolvidas entre o pedido e o código

A instrução da Tabela 1 sugeria quatro nomes de teste que **não existem** na suíte. Para cumprir a exigência de que nenhum método citado seja fictício, a matriz usa os testes reais correspondentes:

| Nome sugerido no pedido | Nome real usado na matriz | O que o teste real verifica |
| :-- | :-- | :-- |
| `FluxoCompletoTests.FluxoCompleto_DePontaAPonta` | `FluxoCompletoTests.FluxoCompleto_DoCadastroAteAvaliacaoENotificacao` | O E2E pela API real (`PetWalkApiFactory`), que atravessa os três fluxos (solicitar → finalizar → avaliar) com as notificações |
| `PasseioEndpointsTests.Solicitar_ComDadosValidos_CriaPasseio` | `PasseioEndpointsTests.Solicitar_GravaOPasseioENotificaOPasseador` | 201, passeio SOLICITADO com tutor/passeador/local, notificação `PASSEIO_SOLICITADO`, uma única gravação |
| `PasseioEndpointsTests.Finalizar_MudaStatusParaFinalizado` | `PasseioEndpointsTests.Finalizar_EmAndamento_NotificaOTutor` | 200, status FINALIZADO e notificação `PASSEIO_FINALIZADO` ao tutor |
| `PasseioEndpointsTests.Avaliar_RegistraAvaliacao` | `PasseioEndpointsTests.Avaliar_PasseioFinalizado_Devolve201ComAMediaAtualizada` | 201, corpo com nota, comentário, data, avaliado e média atualizada |

Os quatro nomes sugeridos não foram incluídos em nenhuma tabela, justamente para que a seção de conformidade permaneça verdadeira: **todos os métodos citados neste documento existem no código**.

Nenhum arquivo de código (`.cs`) foi alterado.
