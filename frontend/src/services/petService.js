import { api } from './api';

export function listarPets(usuarioId) {
  return api(`/usuarios/${usuarioId}/pets`);
}

export function obterPet(usuarioId, petId) {
  return api(`/usuarios/${usuarioId}/pets/${petId}`);
}

export function criarPet(usuarioId, dados) {
  return api(`/usuarios/${usuarioId}/pets`, { method: 'POST', body: dados });
}

export function atualizarPet(usuarioId, petId, dados) {
  return api(`/usuarios/${usuarioId}/pets/${petId}`, { method: 'PUT', body: dados });
}

export function excluirPet(usuarioId, petId) {
  return api(`/usuarios/${usuarioId}/pets/${petId}`, { method: 'DELETE' });
}
