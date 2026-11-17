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

**Ator:** passeador escolhido no passeio, com perfil PASSEADOR ou AMBOS e status ATIVO (REG-003). O frontend envia `{ usuarioId }` no corpo (`AcaoPasseioRequest`); a API não usa token, então quem age é o `UsuarioId` informado.

**Pré-condições**

- Passeio existente (senão **404** `"Passeio não encontrado"`).
- Usuário existente (senão **404** `"Usuário não encontrado"`).
- O usuário é o passeador do passeio, tem perfil de passeador e está ATIVO (REG-003).
- Passeio em EM_ANDAMENTO, isto é, já passou por SOLICITADO → ACEITO → EM_ANDAMENTO.

**Passos**

1. O passeador vê o card do passeio em `/passeios`; com `papel = "PASSEADOR"` e `status = EM_ANDAMENTO`, `acoesDisponiveis` libera apenas `finalizar` e o botão "Finalizar passeio" aparece (`frontend/src/components/PasseioCard.jsx`).
2. O passeador aciona o botão; o serviço envia `POST /passeios/{id}/finalizar` com `{ usuarioId }` (`frontend/src/services/passeioService.js:finalizar` → `executarAcao`).
3. A API resolve o passeio por id; ausente → **404** `"Passeio não encontrado"`. Resolve o usuário por `UsuarioId`; ausente → **404** `"Usuário não encontrado"`.
4. A API chama `passeio.Finalizar(quem)`, que executa nesta ordem: `ExigirPasseadorAtivo(quem)` (REG-003) e `ExigirStatus(EM_ANDAMENTO)`.
5. Se o estado não for EM_ANDAMENTO, `ExigirStatus` lança `InvalidOperationException("Transição não permitida a partir do estado atual")`; `ExecutarAcao` converte em **422** com `ErroHttp.Erro` e não notifica ninguém.
6. Em sucesso, o status passa a FINALIZADO; a API envia `PASSEIO_FINALIZADO` ao outro participante (o tutor) e chama `unidade.Salvar()` uma única vez.
7. A resposta é **200 OK** com `PasseioResponse` na perspectiva do passeador (`papel = "PASSEADOR"`).

**Resultado**

- Corpo `PasseioResponse` com `status = FINALIZADO`, `papel = "PASSEADOR"` e os demais campos do passeio (pet, data e hora, duração, local, valor, tutor e passeador com `Id` e `Nome`).
- Notificação `PASSEIO_FINALIZADO` para o tutor, com a mensagem fixa `"Seu passeio foi finalizado."`, `Lida = false` e `DataEnvio = agora`.

**Estado final**

- Passeio FINALIZADO (terminal): nenhuma outra transição é aceita depois, e o registro permanece no histórico (o fluxo não exclui nem recria o passeio).
- Avaliação liberada: o construtor de `Avaliacao` exige `Passeio.Status == FINALIZADO`, o que habilita o FLX-005.
- Passeio e notificação são persistidos na mesma gravação, no fim do fluxo.
- Em falha, nada muda: sem notificação, sem gravação e `Status` preservado (a atribuição só ocorre depois das checagens).

**Violação do fluxo (finalizar fora de EM_ANDAMENTO)**

Dado um passeio em SOLICITADO, ACEITO, FINALIZADO, RECUSADO ou CANCELADO e o passeador escolhido
Quando o passeador tenta finalizar
Então a operação é rejeitada com **422** e `"Transição não permitida a partir do estado atual"`
E o status permanece o anterior, sem notificação e sem gravação.

- Ator não autorizado tem precedência sobre o estado: `ExigirPasseadorAtivo` roda antes de `ExigirStatus`, então um usuário diferente do passeador recebe `"Passeio não pertence a este passeador"`; sem perfil, `"Usuário não possui perfil de passeador"`; bloqueado, `"Usuário está bloqueado"` — sempre **422**, com o estado inalterado.
- Não há teste de endpoint finalizando fora de EM_ANDAMENTO; o caminho é o mesmo `ExecutarAcao`, coberto para iniciar em `Iniciar_SemAceite_Devolve422SemNotificar` (422, estado inalterado e nenhuma notificação). As violações de estado ao finalizar são testadas no domínio por `SaltosDeEstado_NaoSaoPermitidos` e `PasseioFinalizado_NaoPermiteNovasTransicoes`.

**Regras exercitadas**

- Das cinco regras fixadas: **REG-003**. A transição EM_ANDAMENTO → FINALIZADO é a regra de ciclo de vida descrita em "Estados e transições do Passeio"; a avaliação (FLX-005, REG-001) só se torna possível depois deste fluxo.

**Evidência no código**

| Etapa | Evidência |
| :-- | :-- |
| Rota | `backend/Projeto.Api/Endpoints/PasseioEndpoints.cs:PasseioEndpoints.MapPasseioEndpoints` (`MapPost("/passeios/{id:int}/finalizar", ...)`) e `PasseioEndpoints.ExecutarAcao` |
| Regra do fluxo | `backend/Projeto.Api/Dominio/Passeio.cs:Passeio.Finalizar`, `Passeio.ExigirPasseadorAtivo`, `Passeio.ExigirStatus` |
| Notificação | `backend/Projeto.Api/Servicos/NotificacaoServico.cs:NotificacaoServico.Enviar`, `backend/Projeto.Api/Dominio/Notificacao.cs:construtor de Notificacao` (`GerarMensagem`) |
| Resposta | `backend/Projeto.Api/Endpoints/PasseioEndpoints.cs:PasseioEndpoints.ParaResposta(Passeio, Usuario)` sobre `Contratos/PasseioResponse.cs`; entrada em `Contratos/AcaoPasseioRequest.cs` |
| Erros | `backend/Projeto.Api/Endpoints/ErroHttp.cs:ErroHttp.Erro` e `ErroHttp.NaoEncontrado` |
| Frontend | `frontend/src/components/PasseioCard.jsx`, `frontend/src/utils/passeioAcoes.js:acoesDisponiveis`, `frontend/src/services/passeioService.js:finalizar` |

**Testes que cobrem**

| Teste | Arquivo | O que verifica |
| :-- | :-- | :-- |
| `Finalizar_EmAndamento_NotificaOTutor` | `backend/Projeto.Tests/PasseioEndpointsTests.cs` | 200; status FINALIZADO; notificação `PASSEIO_FINALIZADO` ao tutor |
| `CicloCompleto_SolicitadoAceitoEmAndamentoFinalizado` | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs` | SOLICITADO → ACEITO → EM_ANDAMENTO → FINALIZADO |
| `SaltosDeEstado_NaoSaoPermitidos` | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs` | `Finalizar` lança em SOLICITADO e em ACEITO; estado preservado |
| `UsuarioQueNaoEhOPasseador_NaoPodeIniciarNemFinalizar` | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs` | `"Passeio não pertence a este passeador"` e status EM_ANDAMENTO preservado |
| `PasseioFinalizado_NaoPermiteNovasTransicoes` | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs` | Após FINALIZADO, `Aceitar`, `Recusar`, `Iniciar` e `Finalizar` lançam e o status permanece |
| `PasseadorBloqueado_NaoPodeAceitarIniciarNemFinalizar` | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs` | `"Usuário está bloqueado"` em `Finalizar`, com EM_ANDAMENTO preservado |

Lacuna de cobertura: o teste de endpoint confere o tipo da notificação, não a mensagem `"Seu passeio foi finalizado."` nem `Lida = false`; e não há teste de endpoint para finalizar fora de EM_ANDAMENTO (o caminho equivalente é `Iniciar_SemAceite_Devolve422SemNotificar`).

### FLX-005 — Avaliar passeador

**Ator:** tutor do passeio (perfil TUTOR ou AMBOS), na tela `/passeios`. O frontend envia `AvaliacaoRequest` (`usuarioId`, `nota`, `comentario`) e a API não usa token: quem age é o `UsuarioId` informado.

**Pré-condições**

- Passeio existente (senão **404** `"Passeio não encontrado"`).
- Usuário existente (senão **404** `"Usuário não encontrado"`).
- Passeio FINALIZADO, isto é, o FLX-004 já foi concluído.
- O avaliador é participante do passeio — tutor ou passeador (REG-006); neste fluxo, o tutor.
- O avaliador ainda não avaliou este passeio (REG-001).
- Nota inteira de 1 a 5; o comentário é livre e pode ser vazio.

**Passos**

1. O card do passeio com `papel = "TUTOR"` e `status = FINALIZADO` mostra o botão "Avaliar passeador" (`frontend/src/components/PasseioCard.jsx`); o `AvaliacaoForm` abre com as estrelas de 1 a 5 (nota inicial 5) e o campo de comentário.
2. O tutor confirma; `avaliar(passeioId, usuarioId, nota, comentario.trim())` envia `POST /passeios/{id}/avaliacoes` com `{ usuarioId, nota, comentario }` (`frontend/src/services/passeioService.js`).
3. A API carrega o passeio com `ObterParaAvaliacao` (avaliações do passeio e histórico de avaliações recebidas dos dois participantes); ausente → **404** `"Passeio não encontrado"`. Carrega o avaliador; ausente → **404** `"Usuário não encontrado"`.
4. A API chama `passeio.Avaliar(avaliador, nota, comentario, agora)`: verifica duplicidade, cria a `Avaliacao` (status FINALIZADO, nota 1 a 5, participação), define `Avaliado` como o outro participante (o passeador, neste fluxo) e `Data = agora`, adiciona a avaliação em `_avaliacoes` e recalcula a média em memória com `Usuario.RegistrarAvaliacaoRecebida`.
5. A API chama `unidade.Salvar()`: na mesma transação, insere a avaliação e recalcula a média do avaliado na tabela `Avaliacoes` (`AVG(Nota)`), atualiza `Usuarios.MediaAvaliacao` e sincroniza o objeto rastreado com a média persistida.
6. A resposta é **201** com `AvaliacaoResponse` — `nota`, `comentario`, `data`, `avaliado` (`Id` e `Nome`) e `mediaAvaliacao`; o corpo não tem `id` de avaliação nem cabeçalho `Location`.

**Resultado**

- Corpo `AvaliacaoResponse` com a nota, o comentário, a data (`agora`), o passeador como `avaliado` e a média atualizada.
- O frontend exibe a confirmação montada em `PasseioCard.jsx` (`Avaliação registrada: nota` + a nota, seguida de `Nova média de` + nome e média) e esconde as ações daquele card.

**Estado final**

- Avaliação criada e vinculada ao passeio em `Passeio.Avaliacoes` (`IReadOnlyList`); o máximo é 2 por passeio, uma por participante.
- Média do passeador recalculada com todas as avaliações que ele já recebeu, não só a deste passeio, e persistida em `Usuarios.MediaAvaliacao`; a média do tutor não muda neste fluxo.
- Avaliação e média gravadas na mesma transação, e a resposta devolve a média persistida.
- Em falha, nada é criado nem gravado: a avaliação não entra na coleção, a média não muda e `unidade.Salvar()` não é chamado.

**Violações do fluxo (REG-001 e REG-006)**

- Passeio fora de FINALIZADO → **422** `"Passeio não finalizado"`.
- Segunda avaliação do mesmo avaliador → **422** `"Passeio já avaliado por este usuário"`; a primeira avaliação permanece e a média não muda.
- Nota fora de 1 a 5 → **400** `"Nota deve estar entre 1 e 5"` (a validação lança `ArgumentException`, que `ErroHttp.Erro` converte em 400).
- Usuário que não é tutor nem passeador → **422** `"Usuário não participou deste passeio"` (REG-006).
- Ordem: a duplicidade é verificada em `Passeio.Avaliar` antes das validações do construtor de `Avaliacao`; uma segunda tentativa com nota inválida devolve a mensagem de duplicidade, não a da nota.

**Regras exercitadas**

- Das cinco regras fixadas: **REG-001**. A participação (REG-006) também é verificada pelo construtor de `Avaliacao`, mas está fora das cinco.

**Evidência no código**

| Etapa | Evidência |
| :-- | :-- |
| Rota | `backend/Projeto.Api/Endpoints/PasseioEndpoints.cs:PasseioEndpoints.MapPasseioEndpoints` (`MapPost("/passeios/{id:int}/avaliacoes", ...)`) e `PasseioEndpoints.Avaliar` |
| Regra e criação | `backend/Projeto.Api/Dominio/Passeio.cs:Passeio.Avaliar`, `Passeio.ObterOutroParticipante`; `backend/Projeto.Api/Dominio/Avaliacao.cs:construtor de Avaliacao` |
| Média no domínio | `backend/Projeto.Api/Dominio/Usuario.cs:Usuario.RegistrarAvaliacaoRecebida` |
| Carga do histórico | `backend/Projeto.Api/Repositorios/IPasseioRepositorio.cs:ObterParaAvaliacao`, `Repositorios/Ef/PasseioRepositorio.cs:ObterParaAvaliacao` |
| Média persistida | `backend/Projeto.Api/Repositorios/UnidadeDeTrabalho.cs:UnidadeDeTrabalho.Salvar`, `Dominio/Usuario.cs:Usuario.AtualizarMediaAvaliacao` |
| Resposta | `backend/Projeto.Api/Endpoints/PasseioEndpoints.cs:PasseioEndpoints.ParaResposta(Avaliacao)` sobre `Contratos/AvaliacaoResponse.cs`; entrada em `Contratos/AvaliacaoRequest.cs` |
| Erros | `backend/Projeto.Api/Endpoints/ErroHttp.cs:ErroHttp.Erro` e `ErroHttp.NaoEncontrado` |
| Frontend | `frontend/src/components/PasseioCard.jsx`, `frontend/src/components/AvaliacaoForm.jsx`, `frontend/src/services/passeioService.js:avaliar` |

**Testes que cobrem**

| Teste | Arquivo | O que verifica |
| :-- | :-- | :-- |
| `TutorAvaliaPasseador_RegistraAvaliacaoERecalculaMedia` | `backend/Projeto.Tests/AvaliacaoTests.cs` | Avaliação com avaliador, avaliado, passeio, nota, comentário e data; média do passeador 5 e do tutor 0 |
| `MesmoAvaliadorNaoPodeAvaliarDuasVezes` | `backend/Projeto.Tests/AvaliacaoTests.cs` | Segunda tentativa lança `"Passeio já avaliado por este usuário"`; coleção e média preservadas |
| `PasseioNaoFinalizado_NaoPodeSerAvaliado` | `backend/Projeto.Tests/AvaliacaoTests.cs` | `"Passeio não finalizado"` e nenhuma avaliação criada |
| `UsuarioQueNaoParticipou_NaoPodeAvaliar` | `backend/Projeto.Tests/AvaliacaoTests.cs` | `"Usuário não participou deste passeio"` (REG-006) |
| `NotaForaDoIntervalo_NaoPodeSerAvaliada` | `backend/Projeto.Tests/AvaliacaoTests.cs` | Notas 0 e 6 lançam `"Nota deve estar entre 1 e 5"` |
| `AmbosAvaliam_AsMediasSaoIndividuais` | `backend/Projeto.Tests/AvaliacaoTests.cs` | Duas avaliações no mesmo passeio; médias individuais (5 e 3) |
| `Avaliar_PasseioFinalizado_Devolve201ComAMediaAtualizada` | `backend/Projeto.Tests/PasseioEndpointsTests.cs` | 201; corpo com nota, comentário, data, avaliado e média 5; uma gravação |
| `Avaliar_PasseioNaoFinalizado_Devolve422` | `backend/Projeto.Tests/PasseioEndpointsTests.cs` | 422 `"Passeio não finalizado"`, nenhuma avaliação e nenhuma gravação |
| `Avaliar_DuasVezes_Devolve422` | `backend/Projeto.Tests/PasseioEndpointsTests.cs` | 422 `"Passeio já avaliado por este usuário"` e uma única avaliação |
| `Avaliar_NotaForaDoIntervalo_Devolve400` | `backend/Projeto.Tests/PasseioEndpointsTests.cs` | 400 `"Nota deve estar entre 1 e 5"` para notas 0 e 6 |
| `Avaliar_GravaDepoisDaMutacaoDeDominio` | `backend/Projeto.Tests/PasseioEndpointsTests.cs` | Na gravação a avaliação já existe e a média já está recalculada; 201 e uma gravação |
| `Avaliar_FalhaDePersistencia_NaoEhConvertidaEm4xx` | `backend/Projeto.Tests/PasseioEndpointsTests.cs` | Falha de persistência propaga (500 no pipeline), não 4xx |
| `AvaliacaoResponse_NaoTemCampoId` | `backend/Projeto.Tests/PasseioEndpointsTests.cs` | O contrato da resposta não expõe `Id` de avaliação |
| `FluxoCompleto_DoCadastroAteAvaliacaoENotificacao` | `backend/Projeto.Tests/FluxoCompletoTests.cs` | Pela API real: 201, nota 5, `mediaAvaliacao` 5 e `avaliado.id` do passeador |
| `AvaliarDuasVezes_Devolve422` | `backend/Projeto.Tests/FluxoCompletoTests.cs` | Pela API real: 201 na primeira e 422 com a mensagem de duplicidade na segunda |
| `MediaConsideraAvaliacoesDePasseiosDiferentesDoMesmoPasseador` | `backend/Projeto.Tests/MediaAvaliacaoTests.cs` | Notas 5 e 1 em passeios diferentes geram média 3 |
| `MediaPersistida_NaoDependeDoHistoricoCarregado` | `backend/Projeto.Tests/MediaAvaliacaoConcorrenciaTests.cs` | Sem o histórico carregado, a média persistida e a devolvida são 4 (notas 3 e 5) |

Lacuna de cobertura: não há teste de endpoint para passeio ou avaliador inexistente (404) nem para usuário que não participou (a REG-006 é testada só no domínio); e o teste de 201 usa repositório em memória, então o recálculo por `AVG(Nota)` no banco é coberto por `MediaAvaliacaoConcorrenciaTests` e pelo `FluxoCompletoTests`, não por ele.

**Fora do fluxo especificado:** o código também permite que o passeador avalie o tutor (`Passeio.Avaliar` + `ObterOutroParticipante`, coberto por `PasseadorAvaliaTutor_RegistraAvaliacaoERecalculaMedia`), mas o frontend só oferece "Avaliar passeador" ao tutor; a tela do passeador avaliando o tutor segue fora do escopo da proposta.

## Regras

### REG-001 — Avaliação

**Enunciado:** uma `Avaliacao` só pode ser criada se o `Passeio` referenciado estiver com status FINALIZADO, o mesmo avaliador ainda não tiver avaliado esse passeio e a nota estiver entre 1 e 5.

**Objetos envolvidos:** Avaliação, Passeio, Usuario (avaliador e avaliado).

**Ordem das validações:** `Passeio.Avaliar` verifica a duplicidade antes de criar a `Avaliacao`; o construtor valida, nesta ordem, status FINALIZADO, nota de 1 a 5 e participação (REG-006). Por isso um passeio não finalizado responde `"Passeio não finalizado"` mesmo com nota inválida, e a nota inválida responde antes da checagem de participação; para quem já avaliou, a segunda tentativa devolve a mensagem de duplicidade mesmo com nota inválida.

**Critérios de aceitação**

**C1 — sucesso.**
Dado um passeio FINALIZADO e o tutor participante que ainda não avaliou este passeio
Quando o tutor envia nota 5 e comentário
Então a resposta é **201** com a avaliação criada (nota, comentário, `data = agora`, `avaliado` = passeador)
E a média do passeador é recalculada com todas as avaliações recebidas e persistida, o status do passeio continua FINALIZADO e a média do tutor não muda.

**C2 — violação: passeio não finalizado.**
Dado um passeio que não está FINALIZADO (por exemplo, ACEITO) e o tutor participante
Quando o tutor tenta avaliar
Então a operação é rejeitada com **422** `"Passeio não finalizado"`
E nenhuma avaliação é criada, a média não muda, `unidade.Salvar()` não é chamado e o status do passeio permanece o anterior.

**C3 — violação: já avaliado pelo mesmo usuário.**
Dado um passeio FINALIZADO já avaliado pelo tutor
Quando o mesmo tutor tenta avaliar de novo
Então a operação é rejeitada com **422** `"Passeio já avaliado por este usuário"`
E a primeira avaliação permanece (a coleção continua com uma), a média não muda e nada é gravado.

**C4 — violação: nota fora de 1 a 5.**
Dado um passeio FINALIZADO e o tutor que ainda não avaliou
Quando o tutor envia nota 0 ou 6
Então a operação é rejeitada com **400** `"Nota deve estar entre 1 e 5"` (o construtor lança `ArgumentException`, que `ErroHttp.Erro` mapeia para 400)
E nenhuma avaliação é criada, a média não muda e o status permanece FINALIZADO.

**Critério → teste**

| Critério | Teste |
| :-- | :-- |
| C1 (sucesso) | `backend/Projeto.Tests/AvaliacaoTests.cs:TutorAvaliaPasseador_RegistraAvaliacaoERecalculaMedia`; `backend/Projeto.Tests/PasseioEndpointsTests.cs:Avaliar_PasseioFinalizado_Devolve201ComAMediaAtualizada`; `backend/Projeto.Tests/FluxoCompletoTests.cs:FluxoCompleto_DoCadastroAteAvaliacaoENotificacao`; `backend/Projeto.Tests/MediaAvaliacaoTests.cs:MediaConsideraAvaliacoesDePasseiosDiferentesDoMesmoPasseador` |
| C2 (não finalizado) | `backend/Projeto.Tests/AvaliacaoTests.cs:PasseioNaoFinalizado_NaoPodeSerAvaliado`; `backend/Projeto.Tests/PasseioEndpointsTests.cs:Avaliar_PasseioNaoFinalizado_Devolve422` |
| C3 (duplicada) | `backend/Projeto.Tests/AvaliacaoTests.cs:MesmoAvaliadorNaoPodeAvaliarDuasVezes`; `backend/Projeto.Tests/PasseioEndpointsTests.cs:Avaliar_DuasVezes_Devolve422`; `backend/Projeto.Tests/FluxoCompletoTests.cs:AvaliarDuasVezes_Devolve422` |
| C4 (nota) | `backend/Projeto.Tests/AvaliacaoTests.cs:NotaForaDoIntervalo_NaoPodeSerAvaliada`; `backend/Projeto.Tests/PasseioEndpointsTests.cs:Avaliar_NotaForaDoIntervalo_Devolve400` |

### REG-003 — Ação no passeio pelo passeador escolhido

**Enunciado:** somente o passeador escolhido no `Passeio`, com `TipoPerfil` PASSEADOR ou AMBOS e status ATIVO, pode aceitar, recusar, iniciar ou finalizar esse `Passeio`.

**Objetos envolvidos:** Passeio, Usuario (passeador escolhido).

**Ordem das validações:** `ExigirPasseadorAtivo` verifica, nesta ordem, se `quem` é o passeador do passeio, se tem perfil de passeador e se está ATIVO; só depois `ExigirStatus` verifica o estado. Por isso um usuário que não é o escolhido recebe `"Passeio não pertence a este passeador"` mesmo que também esteja bloqueado ou sem perfil.

**Critérios de aceitação**

**C1 — sucesso.**
Dado um passeio SOLICITADO e o passeador escolhido, com perfil de passeador e ATIVO
Quando o passeador aceita
Então o status passa a ACEITO
E o mesmo critério vale para `Recusar` (SOLICITADO → RECUSADO), `Iniciar` (ACEITO → EM_ANDAMENTO) e `Finalizar` (EM_ANDAMENTO → FINALIZADO), cada ação exigindo o estado de origem correspondente.

**C2 — violação: sem perfil de passeador.**
Dado um passeio cujo passeador escolhido não tem perfil PASSEADOR nem AMBOS
Quando ele tenta aceitar, recusar, iniciar ou finalizar
Então a operação é rejeitada com **422** `"Usuário não possui perfil de passeador"`
E o status permanece o anterior, sem notificação e sem gravação.
Observação: a solicitação já exige passeador com perfil (REG-009), então este caso é defensivo no domínio e não é alcançável pela API.

**C3 — violação: passeador bloqueado.**
Dado um passeio cujo passeador escolhido tem perfil de passeador, mas está BLOQUEADO
Quando ele tenta aceitar, recusar, iniciar ou finalizar
Então a operação é rejeitada com **422** `"Usuário está bloqueado"`
E o status permanece o anterior (SOLICITADO, ACEITO ou EM_ANDAMENTO, conforme a ação), sem notificação e sem gravação.

**C4 — violação: passeio não pertence a este passeador.**
Dado um passeio SOLICITADO e um usuário com perfil de passeador que não é o escolhido
Quando ele tenta aceitar
Então a operação é rejeitada com **422** `"Passeio não pertence a este passeador"`
E o status permanece SOLICITADO, sem notificação e sem gravação.

**Critério → teste**

| Critério | Teste |
| :-- | :-- |
| C1 (sucesso) | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs:PasseadorEscolhido_AceitaPasseio`, `PasseadorEscolhido_RecusaPasseio`, `CicloCompleto_SolicitadoAceitoEmAndamentoFinalizado`; `backend/Projeto.Tests/PasseioEndpointsTests.cs:Aceitar_PasseadorCerto_NotificaOTutor`, `Recusar_PasseadorCerto_NotificaOTutor`, `Iniciar_AposAceite_NotificaOTutor`, `Finalizar_EmAndamento_NotificaOTutor` |
| C2 (sem perfil) | sem teste dedicado |
| C3 (bloqueado) | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs:PasseadorBloqueado_NaoPodeRecusar`, `PasseadorBloqueado_NaoPodeAceitarIniciarNemFinalizar` |
| C4 (não pertence) | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs:UsuarioQueNaoEhOPasseador_NaoPodeAceitar`, `UsuarioQueNaoEhOPasseador_NaoPodeRecusar`, `UsuarioQueNaoEhOPasseador_NaoPodeIniciarNemFinalizar`; `backend/Projeto.Tests/PasseioEndpointsTests.cs:Aceitar_PasseadorErrado_Devolve422SemNotificar` |

### REG-004 — Cancelamento

**Enunciado:** um `Passeio` só pode ser cancelado se estiver com status SOLICITADO ou ACEITO.

**Objetos envolvidos:** Passeio; o cancelamento é executado pelo tutor ou pelo passeador do passeio.

**Ordem das validações:** `Cancelar` verifica primeiro a participação (`Participou(quem)`) e só depois o estado. Por isso um usuário que não é tutor nem passeador recebe `"Usuário não participou deste passeio."` mesmo que o passeio esteja em EM_ANDAMENTO.

**Critérios de aceitação**

**C1 — sucesso.**
Dado um passeio SOLICITADO ou ACEITO e o tutor ou o passeador do passeio
Quando um dos dois cancela
Então a API responde **200**, o status passa a CANCELADO, a notificação `PASSEIO_CANCELADO` é enviada ao outro participante e a gravação acontece uma única vez
E o registro permanece no histórico com o status CANCELADO.

**C2 — violação: estado inválido.**
Dado um passeio EM_ANDAMENTO
Quando o tutor ou o passeador tenta cancelar
Então a operação é rejeitada com **422** `"Passeio não pode ser cancelado no estado atual."`
E o status permanece EM_ANDAMENTO, nenhuma notificação `PASSEIO_CANCELADO` é enviada e nada é gravado. O mesmo vale para FINALIZADO, RECUSADO e CANCELADO, cobertos pelo teste de domínio.

**C3 — violação: usuário que não participou.**
Dado um passeio SOLICITADO e um usuário que não é tutor nem passeador
Quando ele tenta cancelar
Então a operação é rejeitada com **422** `"Usuário não participou deste passeio."`
E o status permanece SOLICITADO, sem notificação e sem gravação.

**Critério → teste**

| Critério | Teste |
| :-- | :-- |
| C1 (sucesso) | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs:TutorEPasseador_CancelamPasseioSolicitadoOuAceito`; `backend/Projeto.Tests/PasseioEndpointsTests.cs:Cancelar_PeloTutor_NotificaOPasseador` |
| C2 (estado inválido) | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs:PasseioForaDeSolicitadoOuAceito_NaoPodeSerCancelado`; `backend/Projeto.Tests/PasseioEndpointsTests.cs:Cancelar_EmAndamento_Devolve422SemNotificar` |
| C3 (não participou) | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs:UsuarioQueNaoParticipou_NaoPodeCancelar` (sem teste de endpoint dedicado) |

### REG-005 — Pet pertence ao tutor

**Enunciado:** o `Pet` vinculado ao `Passeio` deve pertencer ao `Usuario` tutor que está realizando a solicitação.

**Objetos envolvidos:** Passeio, Pet, Usuario.

**Onde é aplicada:** `Usuario.SolicitarPasseio` verifica `pet is null || !_pets.Contains(pet)`; o endpoint procura o pet apenas na lista do tutor (`tutor.Pets.FirstOrDefault(p => p.Id == req.PetId)`), então pet de outro tutor ou inexistente chega ao domínio como nulo. A mesma mensagem é usada por `Usuario.AdicionarPet` quando o pet não pertence ao tutor.

**Critérios de aceitação**

**C1 — sucesso.**
Dado um tutor com o pet na própria lista `_pets`
Quando solicita um passeio informando esse pet
Então a API responde **201** e cria o passeio com `Pet` igual ao pet da lista
E o passeio é gravado com a notificação `PASSEIO_SOLICITADO` ao passeador, e o vínculo do pet com o tutor não muda.

**C2 — violação: pet de outro tutor.**
Dado um tutor e um pet que não está na lista dele (de outro tutor, ou `PetId` inexistente)
Quando o tutor tenta solicitar o passeio com esse pet
Então a operação é rejeitada com **422** `"O pet informado não pertence ao tutor solicitante"`
E nenhum passeio é criado, nenhuma notificação é enviada e nada é gravado; a lista de pets do tutor permanece igual.

**Critério → teste**

| Critério | Teste |
| :-- | :-- |
| C1 (sucesso) | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs:TutorComPetVinculado_SolicitaPasseio_CriaPasseioSolicitado`; `backend/Projeto.Tests/PasseioEndpointsTests.cs:Solicitar_GravaOPasseioENotificaOPasseador` |
| C2 (pet de outro tutor) | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs:PetDeOutroTutor_NaoPodeSerSolicitado`; `backend/Projeto.Tests/PasseioEndpointsTests.cs:Solicitar_PetDeOutroTutor_Devolve422ComAMensagemDaREG005` (o `PetId` inexistente segue o mesmo caminho, sem teste dedicado) |

### REG-008 — Data futura

**Enunciado:** a data e hora do `Passeio` devem ser futuras no momento da solicitação.

**Objetos envolvidos:** Passeio, Usuario, com a comparação feita contra o relógio injetado.

**Relógio injetado:** a API recebe `TimeProvider time` e passa `time.GetUtcNow().UtcDateTime` como `agora` para `Usuario.SolicitarPasseio`; `Program.cs` registra `TimeProvider.System` e a fábrica de testes substitui por `RelogioDeTeste`. A comparação é feita em UTC dos dois lados: `dataHora.ToUniversalTime() <= agora.ToUniversalTime()`.

**Critérios de aceitação**

**C1 — sucesso.**
Dado o instante `agora` do relógio e uma data e hora posteriores
Quando o tutor solicita o passeio
Então a API responde **201** e cria o passeio SOLICITADO com a `DataHora` informada
E a notificação `PASSEIO_SOLICITADO` é gravada para o passeador.

**C2 — violação: data no passado.**
Dado o instante `agora` do relógio e uma data e hora anteriores
Quando o tutor tenta solicitar o passeio
Então a operação é rejeitada com **422** `"A data e hora do passeio devem ser futuras"`
E nenhum passeio é criado, nenhuma notificação é enviada e nada é gravado.

**C3 — violação: fronteira igual a "agora".**
Dado uma data e hora exatamente iguais a `agora`
Quando o tutor tenta solicitar o passeio
Então a operação é rejeitada com **422** `"A data e hora do passeio devem ser futuras"` (a comparação usa `<=`, não `<`)
E nenhum passeio é criado e nada é gravado.

**Critério → teste**

| Critério | Teste |
| :-- | :-- |
| C1 (sucesso) | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs:TutorComPetVinculado_SolicitaPasseio_CriaPasseioSolicitado`; `backend/Projeto.Tests/PasseioEndpointsTests.cs:Solicitar_GravaOPasseioENotificaOPasseador` |
| C2 (data no passado) | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs:DataNoPassado_NaoPodeSerSolicitada` |
| C3 (fronteira) | `backend/Projeto.Tests/SolicitacaoDePasseioTests.cs:DataIgualAAgora_NaoPodeSerSolicitada` |

Os três critérios são cobertos no domínio; não há teste de endpoint dedicado para a REG-008.

## Falhas

O contrato de erro separa três situações: entrada inválida (**400**), regra de negócio violada (**422**) e falha excepcional (**500**). Nos dois primeiros casos o corpo é sempre `{ "erro": "<mensagem>" }`, produzido por `ErroHttp.Erro`.

| Situação | Tipo | Exceção de origem | Status | Corpo da resposta | O que acontece com o estado | Teste de referência |
| :-- | :-- | :-- | :-- | :-- | :-- | :-- |
| Entrada inválida (ex.: nota 0 ou 6; pet sem nome) | Validação | `ArgumentException` lançada pelo domínio | **400** | `{ "erro": "Nota deve estar entre 1 e 5" }` | Nada é alterado: a exceção ocorre antes da criação e da gravação | `backend/Projeto.Tests/PasseioEndpointsTests.cs:Avaliar_NotaForaDoIntervalo_Devolve400`; `backend/Projeto.Tests/EndpointsFalhaDePersistenciaTests.cs:AtualizarPet_DadosInvalidos_Continua400` |
| Regra de negócio violada (ex.: cancelar EM_ANDAMENTO; avaliar passeio não finalizado) | Violação de regra | `InvalidOperationException` lançada pelo domínio | **422** | `{ "erro": "<mensagem da regra>" }` | Estado inalterado; sem notificação e sem gravação | `backend/Projeto.Tests/PasseioEndpointsTests.cs:Cancelar_EmAndamento_Devolve422SemNotificar`; `backend/Projeto.Tests/PasseioEndpointsTests.cs:Avaliar_PasseioNaoFinalizado_Devolve422`; `backend/Projeto.Tests/AvaliacaoTests.cs:PasseioNaoFinalizado_NaoPodeSerAvaliado` |
| Falha excepcional (ex.: repositório/EF falha sem mapeamento de constraint) | Excepcional | exceção que o `ErroDePersistenciaHandler` não reconhece; nos testes, `Exception("segredo-interno")` lançada pela unidade de trabalho substituída | **500** | `{ "erro": "Erro interno. Tente novamente mais tarde." }`, escrito por `FalhaInesperadaHandler` (`backend/Projeto.Api/Endpoints/FalhaInesperadaHandler.cs`). O corpo não contém a mensagem interna, o nome do tipo nem stack trace | A gravação não conclui. Em `POST /passeios`, o teste novo confirma que não há `Passeio` nem `Notificacao` gravados; nas demais rotas o efeito no banco continua **não verificado**, e nos testes antigos de `Solicitar`/`Avaliar` o estado em memória já está preparado antes da falha | `backend/Projeto.Tests/FalhaInesperadaTests.cs:FalhaInesperada_Devolve500ComMensagemGenerica`, `FalhaInesperada_NaoExpoeMensagemInternaNemStackTrace`, `FalhaInesperada_RegistraErroNoLogComMetodoECaminho`, `FalhaInesperada_NaoPersistePasseioNemNotificacao`; `backend/Projeto.Tests/EndpointsFalhaDePersistenciaTests.cs:Cadastro_FalhaAoSalvar_Devolve500`, `CriarPet_FalhaAoSalvar_Devolve500`, `AtualizarPet_FalhaAoSalvar_Devolve500`; `backend/Projeto.Tests/PasseioEndpointsTests.cs:Solicitar_FalhaDePersistencia_NaoEhConvertidaEm4xx`, `Avaliar_FalhaDePersistencia_NaoEhConvertidaEm4xx` |

**Critério de distinção.** O próprio cabeçalho de `ErroHttp.cs` fixa a regra: **400** quando a entrada é inválida em si e corrigir o corpo da requisição resolve (`ArgumentException`); **422** quando a entrada é válida, mas a regra de negócio impede a operação, e repetir a mesma requisição sem mudar o estado não resolve (`InvalidOperationException`). **500** é defeito não previsto: o cliente não consegue corrigi-lo mudando a requisição, por isso não pode ser confundido com 4xx.

**Sobre o caso 500.** A cadeia é: o `ErroDePersistenciaHandler` intercepta `DbUpdateException` com constraint conhecida (duplicidade de e-mail, avaliação duplicada, chave estrangeira, conflito otimista) e devolve 409 ou 422; qualquer outra exceção faz esse handler retornar `false` e cair no `FalhaInesperadaHandler`, registrado logo depois em `Program.cs`. O `FalhaInesperadaHandler` registra a exceção com `ILogger` no nível `Error`, incluindo o método e o caminho HTTP (`_logger.LogError(excecao, "Falha inesperada em {Metodo} {Caminho}", ...)`), e responde **500** com `{ "erro": "Erro interno. Tente novamente mais tarde." }`, sem `ex.Message` nem stack trace. O `AddProblemDetails()` permanece como fallback do `UseExceptionHandler()` sem path, caso nenhum handler reconheça a exceção. O teste `FalhaInesperada_RegistraErroNoLogComMetodoECaminho` confirma o log e `FalhaInesperada_NaoExpoeMensagemInternaNemStackTrace` confirma a ausência de vazamento — ambos em `backend/Projeto.Tests/FalhaInesperadaTests.cs`, aprovados nesta data. Os testes de falha de persistência anteriores (`EndpointsFalhaDePersistenciaTests`) conferem apenas o status 500 e que a falha não vira 4xx. Permanece **não verificado**: o formato do corpo nas rotas que não `POST /passeios`, a escrita do log em um destino real (o teste usa um provider próprio), o caminho com a resposta já iniciada (`Response.HasStarted`) e os cabeçalhos CORS da resposta 500.

**Outros status do contrato.** **404** — `ErroHttp.NaoEncontrado`, usado em `PetEndpoints`, `PasseioEndpoints` e `NotificacaoEndpoints`, com `"Usuário não encontrado"`, `"Pet não encontrado"`, `"Passeador não encontrado"` (na solicitação de passeio), `"Passeio não encontrado"` e `"Notificação não encontrada"`. **409** — `/cadastro` para e-mail já cadastrado (REG-010, `UsuarioEndpoints`) e `ErroDePersistencia` para conflito otimista, duplicidade genérica e chave estrangeira. **403** — `/login` para usuário bloqueado que acertou a senha (REG-011, `UsuarioEndpoints`). **401** — `/login` com credenciais inválidas, com `"E-mail ou senha inválidos"`. Todos usam o corpo `{ "erro": ... }`; o 500 é a exceção do contrato.
