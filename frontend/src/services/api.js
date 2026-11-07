const BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5229';

export async function api(caminho, { method = 'GET', body } = {}) {
  const resposta = await fetch(`${BASE_URL}${caminho}`, {
    method,
    headers: body ? { 'Content-Type': 'application/json' } : undefined,
    body: body ? JSON.stringify(body) : undefined
  });

  if (!resposta.ok) {
    const dados = await resposta.json().catch(() => ({}));
    throw new Error(dados.erro || `Erro ${resposta.status}`);
  }

  return resposta.status === 204 ? null : resposta.json();
}
