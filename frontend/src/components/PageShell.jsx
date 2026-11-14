import Header from './Header';
import '../styles/shell.css';

// Moldura de todas as páginas internas: cabeçalho, fundo decorativo e entrada suave.
export default function PageShell({ children, largura = 'padrao' }) {
  return (
    <div className="shell">
      <div className="shell-fundo" aria-hidden="true">
        <span className="shell-bolha shell-bolha--1" />
        <span className="shell-bolha shell-bolha--2" />
      </div>

      <Header />

      <main className={`shell-conteudo shell-conteudo--${largura} page-enter`}>{children}</main>
    </div>
  );
}

export function PageHeading({ titulo, subtitulo, acao, icone }) {
  return (
    <div className="page-heading">
      <div className="page-heading-texto">
        {icone && <span className="page-heading-icone">{icone}</span>}
        <div>
          <h1>{titulo}</h1>
          {subtitulo && <p>{subtitulo}</p>}
        </div>
      </div>
      {acao && <div className="page-heading-acao">{acao}</div>}
    </div>
  );
}

export function EstadoVazio({ icone, titulo, texto, acao }) {
  return (
    <div className="estado-vazio">
      <span className="estado-vazio-icone">{icone}</span>
      <h2>{titulo}</h2>
      {texto && <p>{texto}</p>}
      {acao}
    </div>
  );
}
