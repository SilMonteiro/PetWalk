import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useNotificacoes } from '../context/NotificacoesContext';
import '../styles/header.css';

export default function Header() {
  const { usuario, sair } = useAuth();
  const { naoLidas } = useNotificacoes();
  const navigate = useNavigate();

  const handleSair = () => {
    sair();
    navigate('/');
  };

  return (
    <header className="header">
      <span className="header-logo">🐾 PetWalk</span>

      <div className="header-acoes">
        <Link
          className="header-notificacoes"
          to="/notificacoes"
          aria-label={
            naoLidas > 0 ? `Notificações: ${naoLidas} não lidas` : 'Notificações'
          }
        >
          <span aria-hidden="true">🔔</span>
          {naoLidas > 0 && <span className="header-contador">{naoLidas}</span>}
        </Link>

        <span className="header-usuario">{usuario.nome}</span>

        <button type="button" className="header-sair" onClick={handleSair}>
          Sair
        </button>
      </div>
    </header>
  );
}
