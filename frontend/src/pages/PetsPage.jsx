import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import Header from '../components/Header';
import PetCard from '../components/PetCard';
import { useAuth } from '../context/AuthContext';
import { listarPets } from '../services/petService';
import { TIPO_PERFIL } from '../constants/enums';
import '../styles/pets.css';

const PetsPage = () => {
  const { usuario } = useAuth();

  const ehTutor =
    usuario.tipoPerfil === TIPO_PERFIL.TUTOR || usuario.tipoPerfil === TIPO_PERFIL.AMBOS;

  const [pets, setPets] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');

  const carregarPets = () => {
    setCarregando(true);
    setErro('');

    listarPets(usuario.id)
      .then((lista) => setPets(lista))
      .catch((falha) => setErro(falha.message))
      .finally(() => setCarregando(false));
  };

  useEffect(() => {
    if (!ehTutor) {
      setCarregando(false);
      return;
    }

    carregarPets();
  }, [ehTutor, usuario.id]);

  const handleExcluido = (petId) => {
    setPets((atuais) => atuais.filter((pet) => pet.id !== petId));
  };

  if (!ehTutor) {
    return (
      <div className="pagina-pets">
        <Header />

        <main className="pets-conteudo">
          <h1 className="pets-titulo">Meus pets</h1>
          <p className="pets-aviso">Apenas usuários com perfil de tutor possuem pets.</p>
          <Link className="pets-botao" to="/home">
            Voltar para a Home
          </Link>
        </main>
      </div>
    );
  }

  return (
    <div className="pagina-pets">
      <Header />

      <main className="pets-conteudo">
        <div className="pets-topo">
          <h1 className="pets-titulo">Meus pets</h1>
          <Link className="pets-botao" to="/pets/novo">
            Novo pet
          </Link>
        </div>

        {carregando && <p className="pets-carregando">Carregando pets...</p>}

        {!carregando && erro && (
          <div className="pets-erro">
            <p className="pets-erro-mensagem">{erro}</p>
            <button type="button" className="pets-botao" onClick={carregarPets}>
              Tentar de novo
            </button>
          </div>
        )}

        {!carregando && !erro && pets.length === 0 && (
          <div className="pets-vazio">
            <p>Você ainda não cadastrou pets</p>
            <Link className="pets-botao" to="/pets/novo">
              Cadastrar pet
            </Link>
          </div>
        )}

        {!carregando && !erro && pets.length > 0 && (
          <ul className="pets-lista">
            {pets.map((pet) => (
              <PetCard
                key={pet.id}
                pet={pet}
                usuarioId={usuario.id}
                onExcluido={handleExcluido}
              />
            ))}
          </ul>
        )}
      </main>
    </div>
  );
};

export default PetsPage;
