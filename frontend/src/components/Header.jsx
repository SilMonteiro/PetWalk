import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import '../styles/header.css';

export default function Header() {
  const { usuario, sair } = useAuth();
  const navigate = useNavigate();

  const handleSair = () => {
    sair();
    navigate('/');
  };

  return (
    <header className="header">
      <span className="header-logo">🐾 PetWalk</span>

      <div className="header-acoes">
        {/* Espaço reservado para o contador de notificações não lidas (S11). */}
        <div className="header-notificacoes" aria-hidden="true" />

        <span className="header-usuario">{usuario.nome}</span>

        <button type="button" className="header-sair" onClick={handleSair}>
          Sair
        </button>
      </div>
    </header>
  );
}
