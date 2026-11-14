import { Link } from 'react-router-dom';
import Foto from './Foto';
import Icon from './Icon';
import Logo from './Logo';
import '../styles/auth.css';

// Moldura das telas de login e cadastro: painel da marca + formulário.
export default function AuthLayout({ titulo, subtitulo, children, rodape }) {
  return (
    <div className="auth">
      <aside className="auth-painel" aria-hidden="true">
        <span className="auth-forma auth-forma--1" />
        <span className="auth-forma auth-forma--2" />

        <div className="auth-painel-conteudo">
          <p className="auth-painel-titulo">Mais passeios, mais felicidade!</p>
          <p className="auth-painel-texto">
            Passeadores, tutores e pets em um só lugar, com cada etapa do passeio à vista.
          </p>
        </div>

        <div className="auth-foto">
          <Foto src="/img/login-cachorro.jpg" alt="" />
        </div>

        <span className="auth-flutuante auth-flutuante--1">
          <Icon nome="pata" />
        </span>
        <span className="auth-flutuante auth-flutuante--2">
          <Icon nome="heart" />
        </span>
      </aside>

      <main className="auth-lado">
        <div className="auth-topo">
          <Logo altura={46} />
          <Link to="/" className="auth-voltar">
            <Icon nome="home" />
            Início
          </Link>
        </div>

        <div className="auth-cartao">
          <h1>{titulo}</h1>
          {subtitulo && <p className="auth-subtitulo">{subtitulo}</p>}

          {children}
        </div>

        {rodape && <p className="auth-rodape">{rodape}</p>}
      </main>
    </div>
  );
}
