import { api } from './api';

export function solicitarPasseio(dados) {
  return api('/passeios', { method: 'POST', body: dados });
}

export function listarPasseios(usuarioId) {
  return api(`/usuarios/${usuarioId}/passeios`);
}

function executarAcao(passeioId, acao, usuarioId) {
  return api(`/passeios/${passeioId}/${acao}`, { method: 'POST', body: { usuarioId } });
}

export function aceitar(passeioId, usuarioId) {
  return executarAcao(passeioId, 'aceitar', usuarioId);
}

export function recusar(passeioId, usuarioId) {
  return executarAcao(passeioId, 'recusar', usuarioId);
}

export function iniciar(passeioId, usuarioId) {
  return executarAcao(passeioId, 'iniciar', usuarioId);
}

export function finalizar(passeioId, usuarioId) {
  return executarAcao(passeioId, 'finalizar', usuarioId);
}

export function cancelar(passeioId, usuarioId) {
  return executarAcao(passeioId, 'cancelar', usuarioId);
}

export function avaliar(passeioId, usuarioId, nota, comentario) {
  return api(`/passeios/${passeioId}/avaliacoes`, {
    method: 'POST',
    body: { usuarioId, nota, comentario }
  });
}
