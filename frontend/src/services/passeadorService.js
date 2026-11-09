import { api } from './api';

export function listarPasseadores() {
  return api('/passeadores');
}
