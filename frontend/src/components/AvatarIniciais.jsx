// Identificação sem foto: a API não envia fotos de pets nem de usuários,
// então usamos a inicial do nome com uma cor estável.
function corDoNome(nome) {
  let soma = 0;

  for (const letra of nome || '') {
    soma += letra.charCodeAt(0);
  }

  return soma % 6;
}

export default function AvatarIniciais({ nome, tamanho = '' }) {
  const inicial = (nome || '?').trim().charAt(0).toUpperCase();

  return (
    <span
      className={`avatar avatar--c${corDoNome(nome)} ${tamanho ? `avatar--${tamanho}` : ''}`.trim()}
      aria-hidden="true"
    >
      {inicial}
    </span>
  );
}
