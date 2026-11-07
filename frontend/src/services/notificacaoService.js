import { api } from './api';

export function listarNotificacoes(usuarioId, apenasNaoLidas = false) {
  const filtro = apenasNaoLidas ? '?naoLidas=true' : '';

  return api(`/usuarios/${usuarioId}/notificacoes${filtro}`);
}

export function marcarComoLida(notificacaoId, usuarioId) {
  return api(`/notificacoes/${notificacaoId}/lida`, {
    method: 'POST',
    body: { usuarioId }
  });
}
