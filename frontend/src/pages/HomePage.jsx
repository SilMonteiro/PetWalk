import { Link } from 'react-router-dom';
import Header from '../components/Header';
import { useAuth } from '../context/AuthContext';
import { TIPO_PERFIL } from '../constants/enums';
import '../styles/home.css';

const CARDS = {
  pets: {
    icone: '🐶',
    titulo: 'Meus pets',
    descricao: 'Cadastre e gerencie seus pets',
    destino: '/pets'
  },
  passeadores: {
    icone: '🚶',
    titulo: 'Buscar passeadores',
    descricao: 'Escolha um passeador para o passeio',
    destino: '/passeadores'
  },
  passeios: {
    icone: '🗓️',
    titulo: 'Meus passeios',
    descricao: 'Acompanhe solicitações e status',
    destino: '/passeios'
  }
};

// Rotas que já existem. As fatias seguintes adicionam os destinos e habilitam os cards.
const DESTINOS_DISPONIVEIS = new Set();

function CardHome({ card }) {
  const disponivel = DESTINOS_DISPONIVEIS.has(card.destino);

  const conteudo = (
    <>
      <span className="card-icon">{card.icone}</span>
      <h3>{card.titulo}</h3>
      <p>{card.descricao}</p>
      {!disponivel && <span className="card-em-breve">Em breve</span>}
    </>
  );

  if (disponivel) {
    return (
      <Link className="card" to={card.destino}>
        {conteudo}
      </Link>
    );
  }

  return (
    <button type="button" className="card card-desabilitado" disabled>
      {conteudo}
    </button>
  );
}

const HomePage = () => {
  const { usuario } = useAuth();

  const ehTutor =
    usuario.tipoPerfil === TIPO_PERFIL.TUTOR || usuario.tipoPerfil === TIPO_PERFIL.AMBOS;
  const ehPasseador =
    usuario.tipoPerfil === TIPO_PERFIL.PASSEADOR || usuario.tipoPerfil === TIPO_PERFIL.AMBOS;

  const cards = [];

  if (ehTutor) {
    cards.push(CARDS.pets, CARDS.passeadores, CARDS.passeios);
  } else if (ehPasseador) {
    cards.push({ ...CARDS.passeios, titulo: 'Solicitações e passeios' });
  }

  return (
    <div className="container-home">
      <Header />

      <main className="main-home">
        <h2>Bem-vindo ao PetWalk!</h2>
        <p>Plataforma de intermediação de passeios com pets</p>

        <div className="cards-home">
          {cards.map((card) => (
            <CardHome key={card.destino} card={card} />
          ))}
        </div>
      </main>
    </div>
  );
};

export default HomePage;
