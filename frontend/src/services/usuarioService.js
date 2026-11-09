import { api } from './api';

export function login(email, senha) {
  return api('/login', { method: 'POST', body: { email, senha } });
}

export function cadastrar(dados) {
  return api('/cadastro', { method: 'POST', body: dados });
}
