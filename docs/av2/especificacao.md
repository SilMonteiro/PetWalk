# Especificação verificável — PetWalk (AV2)

Este documento fixa a especificação verificável dos três fluxos e das cinco regras de negócio da AV2. O escopo vem da `docs/proposta.md`; as transições e as mensagens foram conferidas no código de domínio (`backend/Projeto.Api/Dominio/`), que prevalece quando houver divergência.

Cada critério usa o formato **Dado / Quando / Então**:

- **Dado** — situação inicial observável (status do passeio, perfil e status do usuário, posse do pet, data e hora).
- **Quando** — ação do ator, pela operação de domínio ou pela rota da API.
- **Então** — resultado observável: estado resultante, mensagem literal do código e status HTTP.
- **E** — condições adicionais que valem no mesmo critério.

## Escopo da AV2

### Fluxos

A proposta declara estes três fluxos como escopo da AV2.

| ID | Fluxo | Ator | Ação | Resultado |
| :-- | :-- | :-- | :-- | :-- |
| FLX-002 | Solicitar passeio | Tutor | Preenche data, horário, duração, pet e local (o local é sugerido com o endereço do próprio perfil, podendo ser mantido ou alterado) e confirma | Passeio criado com status SOLICITADO, com o valor calculado pela política de preço do passeador; notificação `PASSEIO_SOLICITADO` enviada ao passeador |
| FLX-004 | Finalizar passeio | Passeador | Marca o passeio como concluído ao término do serviço | Status alterado de EM_ANDAMENTO para FINALIZADO; notificação `PASSEIO_FINALIZADO` enviada ao tutor; avaliação liberada |
| FLX-005 | Avaliar passeador | Tutor | Atribui nota (1 a 5) e comentário ao passeador após o término do passeio | Avaliação criada e média de avaliações do passeador recalculada |

### Regras

| ID | Enunciado (resumo) | Objetos envolvidos | Por que foi escolhida |
| :-- | :-- | :-- | :-- |
| REG-001 | Uma Avaliação só pode ser criada se o Passeio estiver FINALIZADO, o mesmo avaliador ainda não tiver avaliado esse passeio e a nota estiver entre 1 e 5. | Avaliação, Passeio, Usuario | Cruza três classes e concentra três falhas distintas (passeio não finalizado, avaliação duplicada, nota inválida); é o fecho do FLX-005. |
| REG-003 | Somente o passeador escolhido no Passeio, com perfil de passeador e status ATIVO, pode aceitar, recusar, iniciar ou finalizar esse Passeio. | Passeio, Usuario | Protege todas as transições do ciclo de vida (INV-04) e é aplicada por `Passeio.Finalizar` no FLX-004. |
| REG-004 | Um Passeio só pode ser cancelado se estiver com status SOLICITADO ou ACEITO. | Passeio | Transição proibida explícita, com mensagem própria, distinta da mensagem genérica de transição. |
| REG-005 | O Pet vinculado ao Passeio deve pertencer ao Usuario tutor que está realizando a solicitação. | Passeio, Pet, Usuario | Multiplicidade e posse: prova que `Usuario.SolicitarPasseio` só aceita pet da própria lista. |
| REG-008 | A data e hora do Passeio devem ser futuras no momento da solicitação. | Passeio, Usuario | Usa o relógio injetado (parâmetro `agora`), o que torna o teste determinístico. |

### Fluxo × regras

Relação conferida no código; "sim" indica que o fluxo exercita a regra.

| Fluxo | REG-001 | REG-003 | REG-004 | REG-005 | REG-008 |
| :-- | :--: | :--: | :--: | :--: | :--: |
| FLX-002 | — | — | — | sim | sim |
| FLX-004 | — | sim | — | — | — |
| FLX-005 | sim | — | — | — | — |

- **FLX-002** — `Usuario.SolicitarPasseio` aplica REG-005 (`!_pets.Contains(pet)`) e REG-008 (`dataHora.ToUniversalTime() <= agora.ToUniversalTime()`); a mesma operação aplica REG-002 e REG-009, e a REG-007 é garantida por `Usuario.ObterEnderecoPara` (o endereço só é entregue ao próprio usuário).
- **FLX-004** — `Passeio.Finalizar` chama `ExigirPasseadorAtivo` (REG-003) e exige o status EM_ANDAMENTO.
- **FLX-005** — `Passeio.Avaliar` e o construtor de `Avaliacao` aplicam REG-001; a participação no passeio (REG-006) também é verificada, mas está fora das cinco.
- **REG-004** — não é exercitada por nenhum dos três fluxos: o cancelamento é o assunto do FLX-009, fora do escopo desta AV2. Permanece especificada por ser a regra de estado que restringe uma transição do ciclo de vida.

## Estados e transições do Passeio

O enum `StatusPasseio` (`Dominio/Enums.cs`) tem seis valores; o construtor de `Passeio` inicia o objeto em SOLICITADO.

| Estado | Significado | Terminal? |
| :-- | :-- | :-- |
| SOLICITADO | Passeio criado pelo tutor, aguardando decisão do passeador | não |
| ACEITO | Passeador escolhido aceitou o passeio | não |
| EM_ANDAMENTO | Passeador iniciou o passeio | não |
| FINALIZADO | Passeador concluiu o passeio | sim |
| RECUSADO | Passeador escolhido recusou o passeio | sim |
| CANCELADO | Tutor ou passeador cancelou o passeio | sim |

### Transições permitidas

| Operação | Quem executa | Origem | Destino |
| :-- | :-- | :-- | :-- |
| `new Passeio` por `Usuario.SolicitarPasseio` | Tutor solicitante, com pet da própria lista | — (criação) | SOLICITADO |
| `Passeio.Aceitar(quem)` | Passeador escolhido, com perfil de passeador e ATIVO (REG-003) | SOLICITADO | ACEITO |
| `Passeio.Recusar(quem)` | Passeador escolhido, com perfil de passeador e ATIVO (REG-003) | SOLICITADO | RECUSADO |
| `Passeio.Iniciar(quem)` | Passeador escolhido, com perfil de passeador e ATIVO (REG-003) | ACEITO | EM_ANDAMENTO |
| `Passeio.Finalizar(quem)` | Passeador escolhido, com perfil de passeador e ATIVO (REG-003) | EM_ANDAMENTO | FINALIZADO |
| `Passeio.Cancelar(quem)` | Tutor ou passeador do passeio (`Participou(quem)`) | SOLICITADO ou ACEITO | CANCELADO |

A mensagem de rejeição é literal do código:

- `"Transição não permitida a partir do estado atual"` — lançada por `Passeio.ExigirStatus`, usada por `Aceitar` e `Recusar` (exigem SOLICITADO), `Iniciar` (exige ACEITO) e `Finalizar` (exige EM_ANDAMENTO). Como a exceção é lançada antes da atribuição, o `Status` permanece inalterado.
- `"Passeio não pode ser cancelado no estado atual."` — exceção da REG-004: `Cancelar` não usa `ExigirStatus`; verifica `Participou(quem)` e depois exige SOLICITADO ou ACEITO com mensagem própria.

Pela API, as duas são `InvalidOperationException` e são convertidas em **422** por `ErroHttp.Erro`.

### Transições proibidas

| Origem | Tentativa | Destino pretendido | Resultado |
| :-- | :-- | :-- | :-- |
| SOLICITADO | `Iniciar` | EM_ANDAMENTO | Rejeitada — `"Transição não permitida a partir do estado atual"` |
| SOLICITADO | `Finalizar` | FINALIZADO | Rejeitada — `"Transição não permitida a partir do estado atual"` |
| ACEITO | `Aceitar` | ACEITO | Rejeitada — `"Transição não permitida a partir do estado atual"` |
| ACEITO | `Recusar` | RECUSADO | Rejeitada — `"Transição não permitida a partir do estado atual"` |
| ACEITO | `Finalizar` | FINALIZADO | Rejeitada — `"Transição não permitida a partir do estado atual"` |
| EM_ANDAMENTO | `Aceitar` | ACEITO | Rejeitada — `"Transição não permitida a partir do estado atual"` |
| EM_ANDAMENTO | `Recusar` | RECUSADO | Rejeitada — `"Transição não permitida a partir do estado atual"` |
| EM_ANDAMENTO | `Cancelar` | CANCELADO | Rejeitada — `"Passeio não pode ser cancelado no estado atual."` (REG-004) |
| FINALIZADO | Qualquer ação do ciclo de vida | Qualquer estado | Rejeitada — mensagem de `ExigirStatus` em `Aceitar`, `Recusar`, `Iniciar` e `Finalizar`; mensagem da REG-004 em `Cancelar` (estado terminal) |
| RECUSADO | Qualquer ação do ciclo de vida | Qualquer estado | Rejeitada — mesma distinção de mensagens (estado terminal) |
| CANCELADO | Qualquer ação do ciclo de vida | Qualquer estado | Rejeitada — mesma distinção de mensagens (estado terminal) |

Nas linhas de estado terminal, supõe-se um ator autorizado: o passeador escolhido, com perfil e ATIVO, para `Aceitar`, `Recusar`, `Iniciar` e `Finalizar` (REG-003), e um dos participantes para `Cancelar`. Um ator não autorizado é rejeitado antes da checagem de estado — `"Passeio não pertence a este passeador"`, `"Usuário não possui perfil de passeador"` ou `"Usuário está bloqueado"` (REG-003); em `Cancelar`, `"Usuário não participou deste passeio."` — sempre sem alterar o estado.

## Fluxos

### FLX-002 — Solicitar passeio

**Ator:** tutor com perfil TUTOR ou AMBOS, autenticado na sessão do frontend. A API recebe quem age no corpo da requisição (`UsuarioId` de `SolicitacaoPasseioRequest`); não há token de autenticação (fora do escopo registrado na proposta).

**Pré-condições**

- Tutor existente, com perfil TUTOR ou AMBOS (REG-002).
- Pet existente e já vinculado à lista do tutor (REG-005).
- Passeador existente, com perfil PASSEADOR ou AMBOS e status ATIVO (REG-009).
- `DataHora` estritamente futura em relação ao relógio injetado (`agora`); data igual a "agora" também é recusada (REG-008).
- Corpo conforme `SolicitacaoPasseioRequest`: `UsuarioId`, `PetId`, `PasseadorId`, `DataHora`, `DuracaoMinutos` (inteiro, convertido em `TimeSpan` pelo endpoint) e `Local`.

**Passos**

1. O tutor escolhe o passeador na listagem e abre o formulário; o campo local vem pré-preenchido com o endereço do próprio perfil (`frontend/src/pages/SolicitarPasseioPage.jsx`) e pode ser mantido ou alterado.
2. O tutor preenche pet, data e hora, duração e local e confirma; o frontend envia `POST /passeios` com `dataHora` em ISO UTC e `duracaoMinutos` numérico.
3. A API localiza o tutor por `UsuarioId`; se não existir, responde **404** `"Usuário não encontrado"`. Localiza o passeador por `PasseadorId`; se não existir, responde **404** `"Passeador não encontrado"`.
4. A API procura o pet por `PetId` dentro de `tutor.Pets`. Pet inexistente ou de outro tutor chega ao domínio como nulo.
5. A API chama `Usuario.SolicitarPasseio`, que valida nesta ordem: perfil de tutor (REG-002), posse do pet (REG-005), perfil e status do passeador (REG-009) e data futura (REG-008). Toda falha de regra é `InvalidOperationException` e a API responde **422** com a mensagem da regra.
6. O construtor de `Passeio` grava o status SOLICITADO e calcula `Valor` com a política devolvida por `PoliticaDePrecoFabrica.Para(passeador.TipoCobranca)`.
7. A API adiciona o passeio ao repositório, envia a notificação `PASSEIO_SOLICITADO` ao passeador e chama `unidade.Salvar()` uma única vez.
8. A resposta é **201 Created**, com `Location: /passeios/{id}` e corpo `PasseioResponse`.

**Resultado**

- Corpo `PasseioResponse`: `id`, `status = SOLICITADO`, `dataHora`, `duracaoMinutos`, `local` exatamente como informado, `valor`, `nomePet`, `tutor` e `passeador` (cada participante apenas com `Id` e `Nome`) e `papel = "TUTOR"`.
- Notificação `PASSEIO_SOLICITADO` para o passeador, com a mensagem fixa `"Novo passeio solicitado."`, `Lida = false` e `DataEnvio = agora`.

**Estado final**

- Passeio em SOLICITADO com o valor calculado na solicitação; `Valor` só tem getter, então mudar `ValorHora` do passeador depois não recalcula o passeio já criado (`Passeio_MantemOValorDepoisDeMudarOValorHoraDoPasseador`).
- Cálculo do valor: `PrecoPorHora` multiplica `Duracao.TotalHours` por `Passeador.ValorHora` e arredonda em 2 casas com `MidpointRounding.AwayFromZero`; `PrecoComAdicionalPorPorte` usa essa base e aplica fator 1,3 quando o pet é GRANDE. Passeador sem cobrança configurada tem `ValorHora = 0`, logo `Valor = 0` (`Passeio_SemCobrancaConfigurada_TemValorZero`).
- Passeio e notificação são persistidos na mesma gravação (`unidade.Salvar()` chamado uma vez, no fim do fluxo).
- Em falha de regra, nada é criado: a exceção ocorre antes de `passeios.Adicionar` e de `notificacoes.Enviar`, e `unidade.Salvar()` não é chamado.

**Regras exercitadas**

- Das cinco regras fixadas: **REG-005** e **REG-008**. A mesma operação aplica REG-002 e REG-009 e respeita a REG-007 (o endereço do perfil não é lido nem devolvido).

**Evidência no código**

| Etapa | Evidência |
| :-- | :-- |
| Rota | `backend/Projeto.Api/Endpoints/PasseioEndpoints.cs:PasseioEndpoints.MapPasseioEndpoints` (`MapPost("/passeios", ...)`) e `PasseioEndpoints.Solicitar` |
| Regras do fluxo | `backend/Projeto.Api/Dominio/Usuario.cs:Usuario.SolicitarPasseio` |
| Criação e status inicial | `backend/Projeto.Api/Dominio/Passeio.cs:construtor de Passeio` |
| Escolha e cálculo do valor | `backend/Projeto.Api/Dominio/PoliticaDePrecoFabrica.cs:PoliticaDePrecoFabrica.Para`, `Dominio/PrecoPorHora.cs:PrecoPorHora.Calcular`, `Dominio/PrecoComAdicionalPorPorte.cs:PrecoComAdicionalPorPorte.Calcular` |
| Notificação | `backend/Projeto.Api/Servicos/NotificacaoServico.cs:NotificacaoServico.Enviar`, `backend/Projeto.Api/Dominio/Notificacao.cs:construtor de Notificacao` (`GerarMensagem`) |
| Resposta | `backend/Projeto.Api/Endpoints/PasseioEndpoints.cs:PasseioEndpoints.ParaResposta(Passeio, Usuario)` sobre `Contratos/PasseioResponse.cs` |
| Erros | `backend/Projeto.Api/Endpoints/ErroHttp.cs:ErroHttp.Erro` e `ErroHttp.NaoEncontrado` |

**Testes que cobrem**

| Teste | Arquivo | O que verifica |
| :-- | :-- | :-- |
| `Solicitar_GravaOPasseioENotificaOPasseador` | `backend/Projeto.Tests/PasseioEndpointsTests.cs` | 201; passeio SOLICITADO com tutor, passeador e local informados; notificação `PASSEIO_SOLICITADO` não lida; uma única gravação |
| `Solicitar_Resposta_NaoVazaEnderecoDoPerfil` | `backend/Projeto.Tests/PasseioEndpointsTests.cs` | Resposta com o local informado, nomes, nome do pet, duração e `papel = "TUTOR"` |
| `Solicitar_TutorInexistente_Devolve404` | `backend/Projeto.Tests/PasseioEndpointsTests.cs` | 404 `"Usuário não encontrado"` e nenhuma gravação |
| `Solicitar_PasseadorInexistente_Devolve404` | `backend/Projeto.Tests/PasseioEndpointsTests.cs` | 404 `"Passeador não encontrado"` |
| `Solicitar_PetDeOutroTutor_Devolve422ComAMensagemDaREG005` | `backend/Projeto.Tests/PasseioEndpointsTests.cs` | 422 `"O pet informado não pertence ao tutor solicitante"` e nenhum passeio criado |
| `Solicitar_GravaDepoisDeCriarOPasseioENotificar` | `backend/Projeto.Tests/PasseioEndpointsTests.cs` | Ordem domínio → repositórios → `Salvar`; 201 e uma única gravação |
| `Solicitar_FalhaDePersistencia_NaoEhConvertidaEm4xx` | `backend/Projeto.Tests/PasseioEndpointsTests.cs` | Falha de persistência propaga para o tratamento central (500), nunca 4xx |
| `TutorComPetVinculado_SolicitaPasseio_CriaPasseioSolicitado` | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs` | Passeio SOLICITADO com as referências de tutor, passeador e pet |
| `UsuarioSemPerfilDeTutor_NaoPodeSolicitar` | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs` | REG-002: `"Usuário não possui perfil de tutor"` |
| `PetDeOutroTutor_NaoPodeSerSolicitado` | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs` | REG-005: `"O pet informado não pertence ao tutor solicitante"` |
| `DataNoPassado_NaoPodeSerSolicitada` / `DataIgualAAgora_NaoPodeSerSolicitada` | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs` | REG-008: `"A data e hora do passeio devem ser futuras"` (passado e instante igual a `agora`) |
| `PasseadorEscolhidoSemPerfilDePasseador_NaoPodeReceberSolicitacao` | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs` | REG-009: `"Usuário não possui perfil de passeador"` |
| `Passeio_PorHora_CalculaPelaDuracaoEValorHora` | `backend/Projeto.Tests/PasseioValorTests.cs` | Valor por hora calculado na criação |
| `Passeio_ComAdicionalPorPorte_CobraAdicionalDePetGrande` | `backend/Projeto.Tests/PasseioValorTests.cs` | Adicional de 30% para pet GRANDE |
| `Passeio_SemCobrancaConfigurada_TemValorZero` | `backend/Projeto.Tests/PasseioValorTests.cs` | Passeador sem cobrança configurada gera valor 0 |
| `Passeio_MantemOValorDepoisDeMudarOValorHoraDoPasseador` | `backend/Projeto.Tests/PasseioValorTests.cs` | Valor não é recalculado depois da solicitação |

Lacunas de cobertura: não há teste para passeador bloqueado na solicitação (a mensagem `"Usuário está bloqueado"` da REG-009 é testada nas ações do passeio, em `PasseadorBloqueado_NaoPodeRecusar` e `PasseadorBloqueado_NaoPodeAceitarIniciarNemFinalizar`); a REG-002 e a REG-008 são testadas só no domínio, sem teste de endpoint; e `DuracaoMinutos` e `Local` não têm validação de faixa ou obrigatoriedade no domínio nem na API — o formulário limita a duração a `min="15"` e `step="15"`. Nenhum teste de endpoint confere o `Valor` devolvido no `POST /passeios`; o cálculo é coberto no domínio por `PasseioValorTests`.

**Nota sobre o endereço (REG-007)**

O `POST /passeios` grava e devolve exatamente o `Local` recebido e nunca chama `Usuario.ObterEnderecoPara`. O endereço do perfil do tutor só é usado como sugestão de preenchimento no próprio formulário (`SolicitarPasseioPage.jsx`, `useState(usuario.endereco ?? '')`); o valor que o tutor confirma é o único endereço que o passeador vê no passeio. `PasseioResponse` expõe de cada participante apenas `Id` e `Nome`, sem e-mail, telefone ou endereço.

### FLX-004 — Finalizar passeio

<!-- a preencher na Fatia N -->

### FLX-005 — Avaliar passeador

<!-- a preencher na Fatia N -->

## Regras

### REG-001 — Avaliação

<!-- a preencher na Fatia N -->

### REG-003 — Ação no passeio pelo passeador escolhido

<!-- a preencher na Fatia N -->

### REG-004 — Cancelamento

<!-- a preencher na Fatia N -->

### REG-005 — Pet pertence ao tutor

<!-- a preencher na Fatia N -->

### REG-008 — Data futura

<!-- a preencher na Fatia N -->

## Falhas

<!-- a preencher na Fatia N -->
