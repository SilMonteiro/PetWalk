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
| Usuário | idUsuario | tipoPerfil (TUTOR, PASSEADOR, AMBOS), status (ATIVO, BLOQUEADO), telefone, e-mail | solicitarPasseio(), aceitarPasseio(), recusarPasseio(), marcarPasseioComoFinalizado(), atualizarDisponibilidade() |
| Pet | idPet | nome, espécie, porte, idade, vacinas, castrado, localização, tutor (Usuário) | adicionarPet(), removerPet(), buscarPetsDisponiveis(), atualizarStatusPasseio() |
| Passeio | idPasseio | data, horário, duração, local, tutor (Usuário), passeador (Usuário), pet (Pet), status (SOLICITADO, ACEITO, EM_ANDAMENTO, FINALIZADO, RECUSADO, CANCELADO) | criarPasseio(), aceitarPasseio(), recusarPasseio(), iniciarPasseio(), finalizarPasseio(), cancelarPasseio() |
| Avaliação | idAvaliacao | nota, comentário, data, avaliador (Usuário), avaliado (Usuário), passeio (Passeio) | avaliarPasseio(), calcularMediaAvaliacao() |



## Regras e invariantes

| ID      | Regra | Objetos envolvidos | Sucesso | Falha |
| :-- | :-- | :-- | :-- | :-- |
| REG-001 | Uma Avaliação só pode ser criada se o Passeio referenciado estiver com status FINALIZADO e o mesmo avaliador ainda não tiver avaliado esse passeio. | Avaliação, Passeio, Usuário | Avaliação registrada e média do avaliado recalculada | Lançar erro: "Passeio não finalizado" ou "Passeio já avaliado por este usuário" |
| REG-002 | Somente um Usuário com tipoPerfil TUTOR ou AMBOS pode solicitar um Passeio. | Passeio, Usuário | Passeio criado com status SOLICITADO | Erro: "Usuário não possui perfil de tutor" |
| REG-003 | Somente um Usuário com tipoPerfil PASSEADOR ou AMBOS e status ATIVO pode aceitar um Passeio. | Passeio, Usuário | Status do Passeio alterado para ACEITO | Erro: "Usuário não possui perfil de passeador" ou "Usuário está bloqueado" |
| REG-004 | Um Passeio só pode ser cancelado se estiver com status SOLICITADO ou ACEITO. | Passeio | Status do Passeio alterado para CANCELADO | Erro: "Passeio não pode ser cancelado no estado atual" |
| REG-005 | O Pet vinculado ao Passeio deve pertencer ao Usuário tutor que está realizando a solicitação. | Passeio, Pet, Usuário | Pet vinculado ao Passeio com sucesso | Erro: "O pet informado não pertence ao tutor solicitante" |
| REG-006 | Um Usuário só pode avaliar um Passeio do qual participou como tutor ou passeador. | Avaliação, Passeio, Usuário | Avaliação registrada com sucesso | Erro: "Usuário não participou deste passeio" |



## Ciclo de vida

- Objeto central: Passeio
- Estados: SOLICITADO, ACEITO, EM_ANDAMENTO, FINALIZADO, RECUSADO, CANCELADO
- Transições permitidas: criarPasseio() → SOLICITADO; aceitarPasseio() → ACEITO; recusarPasseio() → RECUSADO; iniciarPasseio() → EM_ANDAMENTO; finalizarPasseio() → FINALIZADO; cancelarPasseio() → CANCELADO
- Transições proibidas: FINALIZADO ➔ qualquer estado (passeio encerrado não pode ser reaberto); RECUSADO ➔ qualquer estado (recusa é terminal); CANCELADO ➔ qualquer estado (cancelamento é terminal); SOLICITADO ➔ FINALIZADO (deve passar pelos estados intermediários ACEITO e EM_ANDAMENTO); EM_ANDAMENTO ➔ CANCELADO (REG-004, cancelamento só permitido nos estados SOLICITADO ou ACEITO)



## Fluxos da versão final
| ID | Ação do usuário | Regra principal | Alteração persistida | Resultado |
| :-- | :-- | :-- | :-- | :-- |


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