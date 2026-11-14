import { NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useNotificacoes } from '../context/NotificacoesContext';
import { ROTULO_TIPO_PERFIL, TIPO_PERFIL } from '../constants/enums';
import AvatarIniciais from './AvatarIniciais';
import Icon from './Icon';
import Logo from './Logo';
import '../styles/header.css';

export default function Header() {
  const { usuario, sair } = useAuth();
  const { naoLidas } = useNotificacoes();
  const navigate = useNavigate();

  const ehTutor =
    usuario.tipoPerfil === TIPO_PERFIL.TUTOR || usuario.tipoPerfil === TIPO_PERFIL.AMBOS;

  // Mesmas permissões das páginas: pets e passeadores são só para tutor.
  const links = [{ para: '/home', rotulo: 'Início', icone: 'home' }];

  if (ehTutor) {
    links.push(
      { para: '/pets', rotulo: 'Meus pets', icone: 'pata' },
      { para: '/passeadores', rotulo: 'Passeadores', icone: 'user' }
    );
  }

  links.push({ para: '/passeios', rotulo: 'Meus passeios', icone: 'calendar' });

  const handleSair = () => {
    sair();
    navigate('/');
  };

  const rotuloNotificacoes =
    naoLidas > 0 ? `Notificações: ${naoLidas} não lidas` : 'Notificações';

  return (
    <>
      <header className="header">
        <div className="header-interno">
          <Logo para="/home" altura={42} />

          <nav className="header-nav" aria-label="Navegação principal">
            {links.map((link) => (
              <NavLink
                key={link.para}
                to={link.para}
                className={({ isActive }) => `header-link${isActive ? ' ativo' : ''}`}
              >
                <Icon nome={link.icone} />
                {link.rotulo}
              </NavLink>
            ))}
          </nav>

          <div className="header-acoes">
            <NavLink
              className={({ isActive }) => `header-sino${isActive ? ' ativo' : ''}`}
              to="/notificacoes"
              aria-label={rotuloNotificacoes}
            >
              <Icon nome="bell" />
              {naoLidas > 0 && <span className="header-contador">{naoLidas}</span>}
            </NavLink>

            <div className="header-usuario">
              <AvatarIniciais nome={usuario.nome} tamanho="sm" />
              <div className="header-usuario-texto">
                <span className="header-usuario-nome">{usuario.nome}</span>
                <span className="header-usuario-perfil">
                  {ROTULO_TIPO_PERFIL[usuario.tipoPerfil]}
                </span>
              </div>
            </div>

            <button type="button" className="header-sair" onClick={handleSair}>
              <Icon nome="logout" />
              <span>Sair</span>
            </button>
          </div>
        </div>
      </header>

      {/* Navegação inferior para celulares */}
      <nav className="nav-inferior" aria-label="Navegação principal (celular)">
        {links.map((link) => (
          <NavLink
            key={link.para}
            to={link.para}
            className={({ isActive }) => `nav-inferior-link${isActive ? ' ativo' : ''}`}
          >
            <Icon nome={link.icone} />
            <span>{link.rotulo}</span>
          </NavLink>
        ))}
      </nav>
    </>
  );
}
