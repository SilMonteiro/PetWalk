 Proposta do projeto integrador

## Identificação

- Nome provisório: PetWalk

- Integrantes: Silvia Maria Albuquerque Monteiro, Guilherme Inácio dos Santos Moreira, Rafaella Alves Guerra, Sarah Letícia Domingues dos Santos, Daniel Moraes Delgado

- Público usuário: Pessoas sem tempo para passear com seus pets

- Problema concreto: Muitos tutores possuem rotina de trabalho, estudo ou outras atividades que dificultam a realização regular dos passeios dos seus pets. Ao mesmo tempo, pessoas interessadas em oferecer o serviço podem ter dificuldade para encontrar clientes e organizar horários.

- Processo atual: O tutor pode procurar um passeador por indicação, redes sociais ou aplicativos de mensagens, combinando manualmente horário, duração, local, pet e valor. Isso pode gerar conflitos de agenda, falta de informações sobre o serviço e dificuldade para acompanhar o histórico.

- Resultado esperado: Plataforma que permita ao tutor cadastrar pets e solicitar passeios, enquanto passeadores recebem e aceitam solicitações compatíveis. O sistema controla o ciclo do passeio, registra informações, calcula o valor do serviço e permite acompanhar o histórico.


## Processo principal

Início: O tutor cadastra seu pet e solicita um passeio, informando data, horário, duração, local, pet participante e o passeador de sua preferência.

Decisão: O sistema verifica as condições necessárias para a realização do passeio, principalmente os perfis envolvidos (tutor e passeador ativo), a posse do pet e a data futura da solicitação.

Mudança de estado: Após a solicitação, o passeador escolhido poderá Aceitar ou Recusar o passeio. Quando aceito, o passeio segue pelos estados Solicitado → Aceito → Em andamento → Finalizado. Caso seja recusado, a solicitação é registrada como Recusada. Se cancelada antes da realização, passa para Cancelado.

Atendimento: O passeador escolhido recebe a solicitação e decide se irá aceitá-la ou recusá-la. Caso aceite, realiza o passeio no horário e local definidos pelo tutor.

Encerramento:  Após a realização, o passeio é registrado como Finalizado, mantendo seu histórico no sistema. Em caso de recusa ou cancelamento, o registro permanece com o respectivo status.


## Conceitos do domínio

| Conceito | Identidade | Estado relevante | Comportamento próprio |
| :-- | :-- | :-- | :-- |
| Usuário | idUsuario | nome, tipoPerfil (TUTOR, PASSEADOR, AMBOS), status (ATIVO, BLOQUEADO), telefone, e-mail (único), senha (guardada apenas como hash), endereço (privado), pets (do tutor), valorHora e tipoCobranca (passeador), mediaAvaliacao | solicitarPasseio(), adicionarPet(), removerPet(), obterEnderecoPara(), bloquear(), desbloquear(), recalcularMedia() |
| Pet | idPet | nome, espécie, raça, porte (PEQUENO, MEDIO, GRANDE), idade, observações, vacinas, tutor (Usuário) | atualizar(), registrarVacina() |
| Passeio | idPasseio | data, horário, duração, local, valor, tutor (Usuário), passeador (Usuário), pet (Pet), status (SOLICITADO, ACEITO, EM_ANDAMENTO, FINALIZADO, RECUSADO, CANCELADO) | criarPasseio() (via Usuário.solicitarPasseio()), aceitarPasseio(), recusarPasseio(), iniciarPasseio(), finalizarPasseio(), cancelarPasseio(), avaliarPasseio(), participou() |
| Avaliação | idAvaliacao | nota (1 a 5), comentário, data, avaliador (Usuário), avaliado (Usuário), passeio (Passeio) | criada por Passeio.avaliarPasseio() com validação no construtor (REG-001, REG-006); calcularMediaAvaliacao() (recalculada no Usuário avaliado) |
| Notificação | idNotificacao | destinatario (Usuário), tipo (PASSEIO_SOLICITADO, PASSEIO_ACEITO, PASSEIO_RECUSADO, PASSEIO_INICIADO, PASSEIO_FINALIZADO, PASSEIO_CANCELADO), mensagem, lida, dataEnvio, passeio (Passeio) | enviarNotificacao() (executado pelo serviço de notificações após cada transição do passeio), marcarComoLida(), listarNaoLidas() (consulta por destinatário) |



## Regras e invariantes

| ID      | Regra | Objetos envolvidos | Sucesso | Falha |
| :-- | :-- | :-- | :-- | :-- |
| REG-001 | Uma Avaliação só pode ser criada se o Passeio referenciado estiver com status FINALIZADO, o mesmo avaliador ainda não tiver avaliado esse passeio e a nota estiver entre 1 e 5. | Avaliação, Passeio, Usuário | Avaliação registrada e média do avaliado recalculada | Lançar erro: "Passeio não finalizado", "Passeio já avaliado por este usuário" ou "Nota deve estar entre 1 e 5" |
| REG-002 | Somente um Usuário com tipoPerfil TUTOR ou AMBOS pode solicitar um Passeio. | Passeio, Usuário | Passeio criado com status SOLICITADO | Erro: "Usuário não possui perfil de tutor" |
| REG-003 | Somente o passeador escolhido no Passeio, com tipoPerfil PASSEADOR ou AMBOS e status ATIVO, pode aceitar, recusar, iniciar ou finalizar esse Passeio. | Passeio, Usuário | Status do Passeio alterado para ACEITO, RECUSADO, EM_ANDAMENTO ou FINALIZADO, conforme a ação | Erro: "Usuário não possui perfil de passeador", "Usuário está bloqueado" ou "Passeio não pertence a este passeador" |
| REG-004 | Um Passeio só pode ser cancelado se estiver com status SOLICITADO ou ACEITO. | Passeio | Status do Passeio alterado para CANCELADO | Erro: "Passeio não pode ser cancelado no estado atual" |
| REG-005 | O Pet vinculado ao Passeio deve pertencer ao Usuário tutor que está realizando a solicitação. | Passeio, Pet, Usuário | Pet vinculado ao Passeio com sucesso | Erro: "O pet informado não pertence ao tutor solicitante" |
| REG-006 | Um Usuário só pode avaliar um Passeio do qual participou como tutor ou passeador. | Avaliação, Passeio, Usuário | Avaliação registrada com sucesso | Erro: "Usuário não participou deste passeio" |
| REG-007 | O endereço registrado no perfil de um Usuário é privado: outros usuários não podem visualizá-lo. O endereço do local do Passeio só é compartilhado com o passeador quando o tutor o informa explicitamente no formulário de solicitação (podendo usar o endereço do perfil como sugestão de preenchimento ou inserir um diferente). | Usuário, Passeio | Endereço do perfil acessível apenas ao próprio usuário; local do Passeio visível somente após informado na solicitação | Erro: "Acesso não autorizado ao endereço do perfil" |
| REG-008 | A data e hora do Passeio devem ser futuras no momento da solicitação. | Passeio, Usuário | Passeio criado com status SOLICITADO | Erro: "A data e hora do passeio devem ser futuras" |
| REG-009 | O passeador escolhido na solicitação deve ter tipoPerfil PASSEADOR ou AMBOS e status ATIVO. | Passeio, Usuário | Passeio criado com status SOLICITADO | Erro: "Usuário não possui perfil de passeador" ou "Usuário está bloqueado" |
| REG-010 | Não podem existir dois Usuários com o mesmo e-mail. | Usuário | Usuário cadastrado | Erro: "E-mail já cadastrado" |
| REG-011 | Um Usuário com status BLOQUEADO não pode entrar no sistema. | Usuário | Login realizado | Erro: "Usuário está bloqueado" |



## Ciclo de vida

- Objeto central: Passeio
- Estados: SOLICITADO, ACEITO, EM_ANDAMENTO, FINALIZADO, RECUSADO, CANCELADO
- Transições permitidas: criarPasseio() (tutor) → SOLICITADO; aceitarPasseio() (passeador escolhido, a partir de SOLICITADO) → ACEITO; recusarPasseio() (passeador escolhido, a partir de SOLICITADO) → RECUSADO; iniciarPasseio() (passeador escolhido, a partir de ACEITO) → EM_ANDAMENTO; finalizarPasseio() (passeador escolhido, a partir de EM_ANDAMENTO) → FINALIZADO; cancelarPasseio() (tutor ou passeador do passeio, a partir de SOLICITADO ou ACEITO) → CANCELADO
- Transições proibidas: FINALIZADO ➔ qualquer estado (passeio encerrado não pode ser reaberto); RECUSADO ➔ qualquer estado (recusa é terminal); CANCELADO ➔ qualquer estado (cancelamento é terminal); SOLICITADO ➔ FINALIZADO (deve passar pelos estados intermediários ACEITO e EM_ANDAMENTO); EM_ANDAMENTO ➔ CANCELADO (REG-004, cancelamento só permitido nos estados SOLICITADO ou ACEITO). Qualquer tentativa de transição proibida lança o erro "Transição não permitida a partir do estado atual" (o cancelamento usa a mensagem da REG-004)



## Fluxos da versão final
| ID | Ação do usuário | Regra principal | Alteração persistida | Resultado |
| :-- | :-- | :-- | :-- | :-- |
| FLX-001 | Tutor acessa a listagem de passeadores ativos e seleciona um para solicitar o serviço | REG-002 (somente Usuário com perfil TUTOR ou AMBOS pode iniciar uma solicitação); REG-007 (endereços de perfil dos passeadores não são exibidos na listagem) | Nenhuma alteração persistida; passeador selecionado é carregado no formulário de solicitação | Tutor visualiza o perfil público do passeador (nome, média de avaliações, valor por hora) e avança para o formulário de solicitação |
| FLX-002 | Tutor preenche o formulário de solicitação (data, horário, duração, pet e local) — o campo local é pré-preenchido com o endereço do seu próprio perfil, podendo ser mantido ou alterado — e confirma | REG-002 (perfil TUTOR obrigatório), REG-005 (pet deve pertencer ao tutor), REG-008 (data futura), REG-009 (passeador ativo), REG-007 (o local informado no formulário é o único endereço visível ao passeador) | Passeio criado com status SOLICITADO contendo o local explicitamente informado e o valor calculado pela política de preço do passeador; Notificação (tipo PASSEIO_SOLICITADO) enviada ao passeador | Passeio registrado com o endereço escolhido pelo tutor e passeador notificado da solicitação |
| FLX-003 | Passeador visualiza a solicitação recebida e confirma a aceitação | REG-003 (perfil PASSEADOR e status ATIVO obrigatórios) | Status do Passeio alterado para ACEITO; Notificação (tipo PASSEIO_ACEITO) enviada ao tutor | Tutor notificado de que o passeio foi aceito e está confirmado |
| FLX-004 | Passeador marca o passeio como concluído ao término do serviço | Passeio deve estar com status EM_ANDAMENTO para ser finalizado (transição válida no ciclo de vida) | Status do Passeio alterado para FINALIZADO; Notificação (tipo PASSEIO_FINALIZADO) enviada ao tutor | Passeio encerrado, histórico preservado e avaliação liberada para o tutor |
| FLX-005 | Tutor atribui nota (1 a 5) e comentário ao passeador após o término do passeio | REG-001 (passeio FINALIZADO, ainda não avaliado pelo mesmo usuário e nota válida), REG-006 (tutor participou do passeio como tutor) | Avaliação criada; média de avaliações do passeador recalculada | Avaliação registrada com sucesso e reputação do passeador atualizada |
| FLX-006 | Usuário se cadastra (nome, e-mail, telefone, endereço, perfil e senha) e entra no sistema | REG-010 (e-mail único), REG-011 (usuário bloqueado não entra) | Usuário criado com a senha guardada como hash | Usuário autenticado e direcionado à tela inicial |
| FLX-007 | Tutor cadastra, consulta, edita e exclui seus pets | REG-002 (perfil TUTOR obrigatório), REG-005 (somente o tutor dono altera ou exclui o pet) | Pet criado, alterado ou removido da lista do tutor | Lista de pets do tutor atualizada na tela |
| FLX-008 | Passeador inicia o passeio aceito no horário combinado | REG-003 (passeador escolhido e ativo); Passeio deve estar com status ACEITO | Status do Passeio alterado para EM_ANDAMENTO; Notificação (tipo PASSEIO_INICIADO) enviada ao tutor | Tutor notificado de que o passeio começou |
| FLX-009 | Passeador recusa a solicitação, ou tutor/passeador cancela o passeio antes de ele começar | REG-003 (recusa pelo passeador escolhido) e REG-004 (cancelamento só em SOLICITADO ou ACEITO) | Status do Passeio alterado para RECUSADO ou CANCELADO; Notificação (PASSEIO_RECUSADO ou PASSEIO_CANCELADO) enviada à outra parte | Registro mantido no histórico com o status final |


## Variação polimórfica
- O que varia: a forma de calcular o valor cobrado pelo passeio, escolhida por cada passeador (tipoCobranca).
- Contrato possível: interface IPoliticaDePreco, com o método Calcular(Passeio), que devolve o valor do passeio.
- Implementação 1: PrecoPorHora, que multiplica a duração do passeio pelo valor por hora do passeador.
- Implementação 2: PrecoComAdicionalPorPorte, que parte do preço por hora e aplica um adicional quando o pet é de porte GRANDE.
- Por que a variação é legítima: cada passeador define como cobra, e uma nova forma de cobrança entra criando outra implementação de IPoliticaDePreco, sem alterar o Passeio nem as demais regras.


## Escopo da AV1
- Fatia vertical escolhida: cadastro e login de usuário, CRUD de pets do tutor e solicitação de passeio com aceite ou recusa pelo passeador (FLX-006, FLX-007, FLX-002 e FLX-003), do frontend à API e ao domínio.
- Três regras essenciais: REG-002 (somente tutor solicita), REG-005 (o pet pertence ao tutor) e REG-003 (somente o passeador escolhido e ativo aceita).
- Mudança de estado: SOLICITADO → ACEITO (aceitarPasseio) e SOLICITADO → RECUSADO (recusarPasseio).
- Dados persistidos: Usuário (senha como hash), Pet, Passeio e Notificação, em repositórios em memória (reiniciar a API apaga os dados).


## Escopo da AV2
- Evoluções previstas: ciclo completo do passeio (iniciar, finalizar e cancelar), avaliações com média do passeador, notificações, política de preço (variação polimórfica) e testes automatizados das regras.
- Três fluxos completos: FLX-002 (solicitar passeio), FLX-004 (finalizar passeio) e FLX-005 (avaliar passeador).
- Falhas tratadas: mensagens das REG-001 a REG-011 devolvidas pela API (400, 404 ou 409) e exibidas no frontend; transições proibidas do ciclo de vida.
- Testes esperados: testes xUnit para REG-001 a REG-011, para o ciclo de vida do Passeio (caminho feliz e transições proibidas) e para as duas políticas de preço.


## Fora do escopo

- Autenticação real (login com token/JWT): as ações do passeio identificam quem age pelo id do usuário; as regras de perfil e de endereço (REG-007) são garantidas no domínio.
- Pagamento e cobrança reais: o sistema apenas calcula e registra o valor do passeio.
- Disponibilidade e agenda do passeador (conflito de horários).
- Banco de dados real: os dados ficam em repositórios em memória.
- Busca de passeadores por proximidade e localização do pet.
- Tela para o passeador avaliar o tutor (a REG-006 já permite no domínio).


## Riscos e mitigação
| Risco | Impacto | Mitigação |
| :-- | :-- | :-- |
| Sem autenticação real, o usuário que age é informado pelo cliente | As regras de perfil e a REG-007 só são garantidas no domínio, não na API | O domínio valida perfil, status e posse em cada operação; autenticação com token registrada como fora do escopo |
| Armazenamento em memória | Os dados são perdidos ao reiniciar a API | Repositórios atrás de interfaces, permitindo trocar por banco sem alterar o domínio |
| Integração tardia entre frontend e API | Telas prontas sem endpoint correspondente e retrabalho | Contratos (DTOs) e arquivo .http definidos antes; o frontend usa apenas a camada services/ |
| Unificar Tutor e Passeador em Usuário quebra o código existente | Regressões nas telas de cadastro e login | Testes das regras escritos junto com cada etapa e PRs pequenos |
| Cinco pessoas alterando as mesmas classes | Conflitos de merge e decisões divergentes | Divisão por etapa e por objeto, com revisão cruzada |


## Participação

- **Rodízio**: a cada etapa do plano de implementação (docs/plano-implementacao.md), os integrantes alternam entre domínio, API, testes e frontend.
- **Revisões cruzadas**: todo PR é revisado por quem não o escreveu; quem escreve os testes revisa o PR do Passeio, e quem escreve o Passeio revisa os testes.
- **Conhecimento compartilhado**: regras e decisões ficam registradas nesta proposta e no plano de implementação; ao concluir cada etapa, quem a fez explica o que mudou ao grupo.