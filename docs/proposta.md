 Proposta do projeto integrador

## Identificação

- Nome provisório: PetWalk

- Integrantes: Silvia Maria Albuquerque Monteiro, Guilherme Inácio dos Santos Moreira, Rafaella Alves Guerra, Sarah Letícia Domingues dos Santos, Daniel Moraes Delgado

- Público usuário: Pessoas sem tempo para passear com seus pets

- Problema concreto: Muitos tutores possuem rotina de trabalho, estudo ou outras atividades que dificultam a realização regular dos passeios dos seus pets. Ao mesmo tempo, pessoas interessadas em oferecer o serviço podem ter dificuldade para encontrar clientes e organizar horários.

- Processo atual: O tutor pode procurar um passeador por indicação, redes sociais ou aplicativos de mensagens, combinando manualmente horário, duração, local, pet e valor. Isso pode gerar conflitos de agenda, falta de informações sobre o serviço e dificuldade para acompanhar o histórico.

- Resultado esperado: Plataforma que permita ao tutor cadastrar pets e solicitar passeios, enquanto passeadores informam disponibilidade e aceitam solicitações compatíveis. O sistema controla o ciclo do passeio, registra informações e permite acompanhar serviço e pagamento.


## Processo principal

Início: O tutor cadastra seu pet e solicita um passeio, informando data, horário, duração, local, pet participante e o passeador de sua preferência.

Decisão: O sistema verifica as condições necessárias para a realização do passeio, principalmente a disponibilidade do horário e as condições da solicitação.

Mudança de estado: Após a solicitação, o passeador escolhido poderá Aceitar ou Recusar o passeio. Quando aceito, o passeio segue pelos estados Solicitado → Aceito → Em andamento → Finalizado. Caso seja recusado, a solicitação é registrada como Recusada. Se cancelada antes da realização, passa para Cancelado.

Atendimento: O passeador escolhido recebe a solicitação e decide se irá aceitá-la ou recusá-la. Caso aceite, realiza o passeio no horário e local definidos pelo tutor.

Encerramento:  Após a realização, o passeio é registrado como Finalizado, mantendo seu histórico no sistema. Em caso de recusa ou cancelamento, o registro permanece com o respectivo status.


## Conceitos do domínio

| Conceito | Identidade | Estado relevante | Comportamento próprio |
| :-- | :-- | :-- | :-- |
| Usuário | idUsuario | tipoPerfil (TUTOR, PASSEADOR, AMBOS), status (ATIVO, BLOQUEADO), telefone, e-mail, endereço (privado) | solicitarPasseio(), aceitarPasseio(), recusarPasseio(), marcarPasseioComoFinalizado(), atualizarDisponibilidade(), listarPasseadoresDisponiveis() |
| Pet | idPet | nome, espécie, porte, idade, localização, tutor (Usuário) | adicionarPet(), removerPet(), buscarPetsDisponiveis(), atualizarStatusPasseio() |
| Passeio | idPasseio | data, horário, duração, local, tutor (Usuário), passeador (Usuário), pet (Pet), status (SOLICITADO, ACEITO, EM_ANDAMENTO, FINALIZADO, RECUSADO, CANCELADO) | criarPasseio(), aceitarPasseio(), recusarPasseio(), iniciarPasseio(), finalizarPasseio(), cancelarPasseio() |
| Avaliação | idAvaliacao | nota, comentário, data, avaliador (Usuário), avaliado (Usuário), passeio (Passeio) | avaliarPasseio(), calcularMediaAvaliacao() |
| Notificação | idNotificacao | destinatario (Usuário), tipo (PASSEIO_SOLICITADO, PASSEIO_ACEITO, PASSEIO_RECUSADO, PASSEIO_INICIADO, PASSEIO_FINALIZADO, PASSEIO_CANCELADO), mensagem, lida, dataEnvio, passeio (Passeio) | enviarNotificacao(), marcarComoLida(), listarNaoLidas() |



## Regras e invariantes

| ID      | Regra | Objetos envolvidos | Sucesso | Falha |
| :-- | :-- | :-- | :-- | :-- |
| REG-001 | Uma Avaliação só pode ser criada se o Passeio referenciado estiver com status FINALIZADO e o mesmo avaliador ainda não tiver avaliado esse passeio. | Avaliação, Passeio, Usuário | Avaliação registrada e média do avaliado recalculada | Lançar erro: "Passeio não finalizado" ou "Passeio já avaliado por este usuário" |
| REG-002 | Somente um Usuário com tipoPerfil TUTOR ou AMBOS pode solicitar um Passeio. | Passeio, Usuário | Passeio criado com status SOLICITADO | Erro: "Usuário não possui perfil de tutor" |
| REG-003 | Somente um Usuário com tipoPerfil PASSEADOR ou AMBOS e status ATIVO pode aceitar um Passeio. | Passeio, Usuário | Status do Passeio alterado para ACEITO | Erro: "Usuário não possui perfil de passeador" ou "Usuário está bloqueado" |
| REG-004 | Um Passeio só pode ser cancelado se estiver com status SOLICITADO ou ACEITO. | Passeio | Status do Passeio alterado para CANCELADO | Erro: "Passeio não pode ser cancelado no estado atual" |
| REG-005 | O Pet vinculado ao Passeio deve pertencer ao Usuário tutor que está realizando a solicitação. | Passeio, Pet, Usuário | Pet vinculado ao Passeio com sucesso | Erro: "O pet informado não pertence ao tutor solicitante" |
| REG-006 | Um Usuário só pode avaliar um Passeio do qual participou como tutor ou passeador. | Avaliação, Passeio, Usuário | Avaliação registrada com sucesso | Erro: "Usuário não participou deste passeio" |
| REG-007 | O endereço registrado no perfil de um Usuário é privado: outros usuários não podem visualizá-lo. O endereço do local do Passeio só é compartilhado com o passeador quando o tutor o informa explicitamente no formulário de solicitação (podendo usar o endereço do perfil como sugestão de preenchimento ou inserir um diferente). | Usuário, Passeio | Endereço do perfil acessível apenas ao próprio usuário; local do Passeio visível somente após informado na solicitação | Erro: "Acesso não autorizado ao endereço do perfil" |



## Ciclo de vida

- Objeto central: Passeio
- Estados: SOLICITADO, ACEITO, EM_ANDAMENTO, FINALIZADO, RECUSADO, CANCELADO
- Transições permitidas: criarPasseio() → SOLICITADO; aceitarPasseio() → ACEITO; recusarPasseio() → RECUSADO; iniciarPasseio() → EM_ANDAMENTO; finalizarPasseio() → FINALIZADO; cancelarPasseio() → CANCELADO
- Transições proibidas: FINALIZADO ➔ qualquer estado (passeio encerrado não pode ser reaberto); RECUSADO ➔ qualquer estado (recusa é terminal); CANCELADO ➔ qualquer estado (cancelamento é terminal); SOLICITADO ➔ FINALIZADO (deve passar pelos estados intermediários ACEITO e EM_ANDAMENTO); EM_ANDAMENTO ➔ CANCELADO (REG-004, cancelamento só permitido nos estados SOLICITADO ou ACEITO)



## Fluxos da versão final
| ID | Ação do usuário | Regra principal | Alteração persistida | Resultado |
| :-- | :-- | :-- | :-- | :-- |
| FLX-001 | Tutor acessa a listagem de passeadores disponíveis e seleciona um para solicitar o serviço | REG-002 (somente Usuário com perfil TUTOR ou AMBOS pode iniciar uma solicitação); REG-007 (endereços de perfil dos passeadores não são exibidos na listagem) | Nenhuma alteração persistida; passeador selecionado é carregado no formulário de solicitação | Tutor visualiza o perfil público do passeador e avança para o formulário de solicitação |
| FLX-002 | Tutor preenche o formulário de solicitação (data, horário, duração, pet e local) — o campo local é pré-preenchido com o endereço do seu próprio perfil, podendo ser mantido ou alterado — e confirma | REG-002 (perfil TUTOR obrigatório), REG-005 (pet deve pertencer ao tutor), REG-007 (o local informado no formulário é o único endereço visível ao passeador) | Passeio criado com status SOLICITADO contendo o local explicitamente informado; Notificação (tipo PASSEIO_SOLICITADO) enviada ao passeador | Passeio registrado com o endereço escolhido pelo tutor e passeador notificado da solicitação |
| FLX-003 | Passeador visualiza a solicitação recebida e confirma a aceitação | REG-003 (perfil PASSEADOR e status ATIVO obrigatórios) | Status do Passeio alterado para ACEITO; Notificação (tipo PASSEIO_ACEITO) enviada ao tutor | Tutor notificado de que o passeio foi aceito e está confirmado |
| FLX-004 | Passeador marca o passeio como concluído ao término do serviço | Passeio deve estar com status EM_ANDAMENTO para ser finalizado (transição válida no ciclo de vida) | Status do Passeio alterado para FINALIZADO; Notificação (tipo PASSEIO_FINALIZADO) enviada ao tutor | Passeio encerrado, histórico preservado e avaliação liberada para o tutor |
| FLX-005 | Tutor atribui nota e comentário ao passeador após o término do passeio | REG-001 (passeio FINALIZADO e ainda não avaliado pelo mesmo usuário), REG-006 (tutor participou do passeio como tutor) | Avaliação criada; média de avaliações do passeador recalculada | Avaliação registrada com sucesso e reputação do passeador atualizada |


## Variação polimórfica
- O que varia: 
- Contrato possível: 
- Implementação 1: 
- Implementação 2: 
- Por que a variação é legítima: 


## Escopo da AV1
- Fatia vertical escolhida: 
- Três regras essenciais: 
- Mudança de estado: 
- Dados persistidos: 


## Escopo da AV2
- Evoluções previstas: 
- Três fluxos completos: 
- Falhas tratadas: 
- Testes esperados: 


## Fora do escopo


## Riscos e mitigação
| Risco | Impacto | Mitigação |
| :-- | :-- | :-- |


## Participação

- **Rodízio**: 
- **Revisões cruzadas**: 
- **Conhecimento compartilhado**: 