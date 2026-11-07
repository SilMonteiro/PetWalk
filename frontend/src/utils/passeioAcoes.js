import { PAPEL, STATUS_PASSEIO } from '../constants/enums';

// Perspectiva do usuário no passeio (`papel`) + status => ações permitidas.
export function acoesDisponiveis(papel, status) {
  if (papel === PAPEL.PASSEADOR) {
    if (status === STATUS_PASSEIO.SOLICITADO) {
      return ['aceitar', 'recusar', 'cancelar'];
    }

    if (status === STATUS_PASSEIO.ACEITO) {
      return ['iniciar', 'cancelar'];
    }

    if (status === STATUS_PASSEIO.EM_ANDAMENTO) {
      return ['finalizar'];
    }

    return [];
  }

  if (papel === PAPEL.TUTOR) {
    if (status === STATUS_PASSEIO.SOLICITADO || status === STATUS_PASSEIO.ACEITO) {
      return ['cancelar'];
    }

    if (status === STATUS_PASSEIO.FINALIZADO) {
      return ['avaliar'];
    }
  }

  return [];
}
