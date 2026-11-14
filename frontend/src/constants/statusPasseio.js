import { STATUS_PASSEIO } from './enums';

// Apenas os 6 status reais da API. Cores ficam em design-system.css (.status-badge--N).
export const VISUAL_STATUS = {
  [STATUS_PASSEIO.SOLICITADO]: {
    icone: 'clock',
    descricaoTutor: 'Aguardando o passeador aceitar.',
    descricaoPasseador: 'Aguardando a sua resposta.'
  },
  [STATUS_PASSEIO.ACEITO]: {
    icone: 'calendar',
    descricaoTutor: 'Passeio confirmado para a data combinada.',
    descricaoPasseador: 'Confirmado. Inicie quando chegar a hora.'
  },
  [STATUS_PASSEIO.EM_ANDAMENTO]: {
    icone: 'play',
    descricaoTutor: 'O passeio está acontecendo agora.',
    descricaoPasseador: 'Passeio em andamento.'
  },
  [STATUS_PASSEIO.FINALIZADO]: {
    icone: 'check',
    descricaoTutor: 'Passeio concluído.',
    descricaoPasseador: 'Passeio concluído.'
  },
  [STATUS_PASSEIO.RECUSADO]: {
    icone: 'ban',
    descricaoTutor: 'O passeador recusou esta solicitação.',
    descricaoPasseador: 'Você recusou esta solicitação.'
  },
  [STATUS_PASSEIO.CANCELADO]: {
    icone: 'x',
    descricaoTutor: 'Este passeio foi cancelado.',
    descricaoPasseador: 'Este passeio foi cancelado.'
  }
};

// Etapas da linha do tempo (fluxo feliz). Recusado/Cancelado são desfechos finais.
export const ETAPAS_FLUXO = [
  STATUS_PASSEIO.SOLICITADO,
  STATUS_PASSEIO.ACEITO,
  STATUS_PASSEIO.EM_ANDAMENTO,
  STATUS_PASSEIO.FINALIZADO
];
