import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import AvatarIniciais from '../components/AvatarIniciais';
import Icon from '../components/Icon';
import PageShell, { EstadoVazio, PageHeading } from '../components/PageShell';
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
      <PageShell largura="estreita">
        <PageHeading icone={<Icon nome="user" />} titulo="Passeadores" />

        <EstadoVazio
          icone={<Icon nome="info" />}
          titulo="Área para tutores"
          texto="Apenas usuários com perfil de tutor podem contratar passeadores."
          acao={
            <Link className="btn" to="/home">
              Voltar para o início
            </Link>
          }
        />
      </PageShell>
    );
  }

  // A API não sabe quem consulta; um usuário AMBOS não deve ser passeador de si mesmo.
  const disponiveis = passeadores.filter((passeador) => passeador.id !== usuario.id);
  const semPets = !carregando && !erro && pets.length === 0;

  return (
    <PageShell>
      <PageHeading
        icone={<Icon nome="user" />}
        titulo="Passeadores"
        subtitulo="Escolha quem vai passear com o seu pet."
      />

      {carregando && (
        <div className="carregando" role="status">
          <span className="spinner" />
          Carregando passeadores...
        </div>
      )}

      {!carregando && erro && (
        <div className="bloco-erro">
          <p className="alerta alerta--erro" role="alert">
            <Icon nome="alert" />
            {erro}
          </p>
          <button type="button" className="btn btn--outline" onClick={carregar}>
            Tentar de novo
          </button>
        </div>
      )}

      {semPets && (
        <div className="passeadores-aviso">
          <p className="alerta alerta--info">
            <Icon nome="info" />
            Cadastre um pet antes de solicitar um passeio.
          </p>
          <Link className="btn btn--sm" to="/pets/novo">
            <Icon nome="plus" />
            Cadastrar pet
          </Link>
        </div>
      )}

      {!carregando && !erro && disponiveis.length === 0 && (
        <EstadoVazio
          icone={<Icon nome="user" />}
          titulo="Nenhum passeador disponível no momento"
          texto="Volte em breve: novos passeadores podem se cadastrar a qualquer momento."
        />
      )}

      {!carregando && !erro && disponiveis.length > 0 && (
        <ul className="passeadores-lista">
          {disponiveis.map((passeador) => {
            const semAvaliacao = passeador.mediaAvaliacao === 0;

            return (
              <li key={passeador.id} className="passeador-card">
                <div className="passeador-topo">
                  <AvatarIniciais nome={passeador.nome} tamanho="lg" />
                  <div>
                    <h2 className="passeador-nome">{passeador.nome}</h2>
                    <p className={`passeador-media${semAvaliacao ? ' vazia' : ''}`}>
                      <Icon nome="star" />
                      {semAvaliacao
                        ? 'Sem avaliações'
                        : `Média ${FORMATO_MEDIA.format(passeador.mediaAvaliacao)}`}
                    </p>
                  </div>
                </div>

                <div className="passeador-preco">
                  <strong>{FORMATO_MOEDA.format(passeador.valorHora)}</strong>
                  <span>por hora</span>
                </div>

                <p className="passeador-cobranca">
                  <Icon nome="wallet" />
                  {ROTULO_TIPO_COBRANCA[passeador.tipoCobranca]}
                </p>

                <button
                  type="button"
                  className="btn btn--block"
                  onClick={() => navigate(`/passeios/novo?passeadorId=${passeador.id}`)}
                  disabled={semPets}
                >
                  <Icon nome="send" />
                  Solicitar passeio
                </button>
              </li>
            );
          })}
        </ul>
      )}
    </PageShell>
  );
};

export default PasseadoresPage;
