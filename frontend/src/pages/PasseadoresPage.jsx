import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import Header from '../components/Header';
import { useAuth } from '../context/AuthContext';
import { listarPasseadores } from '../services/passeadorService';
import { listarPets } from '../services/petService';
import { ROTULO_TIPO_COBRANCA, TIPO_PERFIL } from '../constants/enums';
import '../styles/passeadores.css';

const FORMATO_MOEDA = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL'
});

const FORMATO_MEDIA = new Intl.NumberFormat('pt-BR', {
  minimumFractionDigits: 1,
  maximumFractionDigits: 1
});

const PasseadoresPage = () => {
  const { usuario } = useAuth();
  const navigate = useNavigate();

  const ehTutor =
    usuario.tipoPerfil === TIPO_PERFIL.TUTOR || usuario.tipoPerfil === TIPO_PERFIL.AMBOS;

  const [passeadores, setPasseadores] = useState([]);
  const [pets, setPets] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');

  const carregar = () => {
    setCarregando(true);
    setErro('');

    Promise.all([listarPasseadores(), listarPets(usuario.id)])
      .then(([listaPasseadores, listaPets]) => {
        setPasseadores(listaPasseadores);
        setPets(listaPets);
      })
      .catch((falha) => setErro(falha.message))
      .finally(() => setCarregando(false));
  };

  useEffect(() => {
    if (!ehTutor) {
      setCarregando(false);
      return;
    }

    carregar();
  }, [ehTutor, usuario.id]);

  if (!ehTutor) {
    return (
      <div className="pagina-passeadores">
        <Header />

        <main className="passeadores-conteudo">
          <h1 className="passeadores-titulo">Passeadores</h1>

          <div className="passeadores-aviso">
            <p className="passeadores-aviso-texto">
              Apenas usuários com perfil de tutor podem contratar passeadores.
            </p>
            <Link className="passeadores-botao" to="/home">
              Voltar para a Home
            </Link>
          </div>
        </main>
      </div>
    );
  }

  // A API não sabe quem consulta; um usuário AMBOS não deve ser passeador de si mesmo.
  const disponiveis = passeadores.filter((passeador) => passeador.id !== usuario.id);
  const semPets = !carregando && !erro && pets.length === 0;

  return (
    <div className="pagina-passeadores">
      <Header />

      <main className="passeadores-conteudo">
        <h1 className="passeadores-titulo">Passeadores</h1>

        {carregando && <p className="passeadores-carregando">Carregando passeadores...</p>}

        {!carregando && erro && (
          <div className="passeadores-erro">
            <p className="passeadores-erro-mensagem">{erro}</p>
            <button type="button" className="passeadores-botao" onClick={carregar}>
              Tentar de novo
            </button>
          </div>
        )}

        {semPets && (
          <div className="passeadores-aviso">
            <p className="passeadores-aviso-texto">
              Cadastre um pet antes de solicitar um passeio.
            </p>
            <Link className="passeadores-botao" to="/pets/novo">
              Cadastrar pet
            </Link>
          </div>
        )}

        {!carregando && !erro && disponiveis.length === 0 && (
          <p className="passeadores-vazio">Nenhum passeador disponível no momento.</p>
        )}

        {!carregando && !erro && disponiveis.length > 0 && (
          <ul className="passeadores-lista">
            {disponiveis.map((passeador) => (
              <li key={passeador.id} className="passeador-card">
                <h2 className="passeador-nome">{passeador.nome}</h2>

                <p className="passeador-detalhe">
                  {passeador.mediaAvaliacao === 0
                    ? 'Sem avaliações'
                    : `Média: ${FORMATO_MEDIA.format(passeador.mediaAvaliacao)}`}
                </p>

                <p className="passeador-detalhe">
                  {FORMATO_MOEDA.format(passeador.valorHora)} por hora
                </p>

                <p className="passeador-detalhe">
                  {ROTULO_TIPO_COBRANCA[passeador.tipoCobranca]}
                </p>

                <button
                  type="button"
                  className="passeadores-botao"
                  onClick={() => navigate(`/passeios/novo?passeadorId=${passeador.id}`)}
                  disabled={semPets}
                >
                  Solicitar
                </button>
              </li>
            ))}
          </ul>
        )}
      </main>
    </div>
  );
};

export default PasseadoresPage;
