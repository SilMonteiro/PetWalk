// Enums da API .NET. Chegam como NÚMERO; só o campo `papel` do passeio é texto.

export const TIPO_PERFIL = {
  TUTOR: 0,
  PASSEADOR: 1,
  AMBOS: 2
};

export const PORTE = {
  PEQUENO: 0,
  MEDIO: 1,
  GRANDE: 2
};

export const TIPO_COBRANCA = {
  POR_HORA: 0,
  POR_HORA_COM_ADICIONAL_DE_PORTE: 1
};

export const STATUS_PASSEIO = {
  SOLICITADO: 0,
  ACEITO: 1,
  EM_ANDAMENTO: 2,
  FINALIZADO: 3,
  RECUSADO: 4,
  CANCELADO: 5
};

export const TIPO_NOTIFICACAO = {
  SOLICITADO: 0,
  ACEITO: 1,
  RECUSADO: 2,
  INICIADO: 3,
  FINALIZADO: 4,
  CANCELADO: 5
};

export const ROTULO_TIPO_PERFIL = {
  [TIPO_PERFIL.TUTOR]: 'Tutor',
  [TIPO_PERFIL.PASSEADOR]: 'Passeador',
  [TIPO_PERFIL.AMBOS]: 'Ambos'
};

export const ROTULO_PORTE = {
  [PORTE.PEQUENO]: 'Pequeno',
  [PORTE.MEDIO]: 'Médio',
  [PORTE.GRANDE]: 'Grande'
};

export const ROTULO_TIPO_COBRANCA = {
  [TIPO_COBRANCA.POR_HORA]: 'Por hora',
  [TIPO_COBRANCA.POR_HORA_COM_ADICIONAL_DE_PORTE]: 'Por hora com adicional de porte'
};

export const ROTULO_STATUS_PASSEIO = {
  [STATUS_PASSEIO.SOLICITADO]: 'Solicitado',
  [STATUS_PASSEIO.ACEITO]: 'Aceito',
  [STATUS_PASSEIO.EM_ANDAMENTO]: 'Em andamento',
  [STATUS_PASSEIO.FINALIZADO]: 'Finalizado',
  [STATUS_PASSEIO.RECUSADO]: 'Recusado',
  [STATUS_PASSEIO.CANCELADO]: 'Cancelado'
};

export const ROTULO_TIPO_NOTIFICACAO = {
  [TIPO_NOTIFICACAO.SOLICITADO]: 'Solicitado',
  [TIPO_NOTIFICACAO.ACEITO]: 'Aceito',
  [TIPO_NOTIFICACAO.RECUSADO]: 'Recusado',
  [TIPO_NOTIFICACAO.INICIADO]: 'Iniciado',
  [TIPO_NOTIFICACAO.FINALIZADO]: 'Finalizado',
  [TIPO_NOTIFICACAO.CANCELADO]: 'Cancelado'
};
