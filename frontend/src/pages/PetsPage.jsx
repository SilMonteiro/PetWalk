import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import PageShell, { EstadoVazio, PageHeading } from '../components/PageShell';
import PetCard from '../components/PetCard';
import Icon from '../components/Icon';
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
      <PageShell largura="estreita">
        <PageHeading icone={<Icon nome="pata" />} titulo="Meus pets" />

        <EstadoVazio
          icone={<Icon nome="info" />}
          titulo="Área para tutores"
          texto="Apenas usuários com perfil de tutor possuem pets."
          acao={
            <Link className="btn" to="/home">
              Voltar para o início
            </Link>
          }
        />
      </PageShell>
    );
  }

  return (
    <PageShell>
      <PageHeading
        icone={<Icon nome="pata" />}
        titulo="Meus pets"
        subtitulo="Quem faz parte da sua família."
        acao={
          <Link className="btn" to="/pets/novo">
            <Icon nome="plus" />
            Novo pet
          </Link>
        }
      />

      {carregando && (
        <div className="carregando" role="status">
          <span className="spinner" />
          Carregando pets...
        </div>
      )}

      {!carregando && erro && (
        <div className="bloco-erro">
          <p className="alerta alerta--erro" role="alert">
            <Icon nome="alert" />
            {erro}
          </p>
          <button type="button" className="btn btn--outline" onClick={carregarPets}>
            Tentar de novo
          </button>
        </div>
      )}

      {!carregando && !erro && pets.length === 0 && (
        <EstadoVazio
          icone={<Icon nome="pata" />}
          titulo="Você ainda não cadastrou pets"
          texto="Cadastre seu primeiro pet para poder solicitar passeios."
          acao={
            <Link className="btn" to="/pets/novo">
              <Icon nome="plus" />
              Cadastrar pet
            </Link>
          }
        />
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
    </PageShell>
  );
};

export default PetsPage;
