import { useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import Header from '../components/Header';
import { useAuth } from '../context/AuthContext';
import { listarPasseadores } from '../services/passeadorService';
import { listarPets } from '../services/petService';
import { solicitarPasseio } from '../services/passeioService';
import { ROTULO_STATUS_PASSEIO, TIPO_PERFIL } from '../constants/enums';
import '../styles/solicitar-passeio.css';

const FORMATO_MOEDA = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL'
});

// datetime-local não tem fuso: o valor é local e o min também.
function paraDataHoraLocal(data) {
  const local = new Date(data.getTime() - data.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 16);
}

const SolicitarPasseioPage = () => {
  const { usuario } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const passeadorId = Number(searchParams.get('passeadorId'));

  const ehTutor =
    usuario.tipoPerfil === TIPO_PERFIL.TUTOR || usuario.tipoPerfil === TIPO_PERFIL.AMBOS;

  const [passeadores, setPasseadores] = useState([]);
  const [pets, setPets] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [erroCarga, setErroCarga] = useState('');

  const [petId, setPetId] = useState('');
  const [dataHora, setDataHora] = useState('');
  const [duracaoMinutos, setDuracaoMinutos] = useState('');
  const [local, setLocal] = useState(usuario.endereco ?? '');

  const [erro, setErro] = useState('');
  const [sucesso, setSucesso] = useState('');
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    if (!ehTutor) {
      setCarregando(false);
      return;
    }

    let ativo = true;

    Promise.all([listarPasseadores(), listarPets(usuario.id)])
      .then(([listaPasseadores, listaPets]) => {
        if (!ativo) {
          return;
        }

        setPasseadores(listaPasseadores);
        setPets(listaPets);

        if (listaPets.length > 0) {
          setPetId(String(listaPets[0].id));
        }
      })
      .catch((falha) => {
        if (ativo) {
          setErroCarga(falha.message);
        }
      })
      .finally(() => {
        if (ativo) {
          setCarregando(false);
        }
      });

    return () => {
      ativo = false;
    };
  }, [ehTutor, usuario.id]);

  const passeador = passeadores.find((item) => item.id === passeadorId);

  const handleSubmit = async (evento) => {
    evento.preventDefault();

    if (enviando) {
      return;
    }

    setErro('');
    setSucesso('');
    setEnviando(true);

    try {
      const passeio = await solicitarPasseio({
        usuarioId: usuario.id,
        petId: Number(petId),
        passeadorId: passeador.id,
        // O datetime-local não tem fuso; a API compara com UTC (REG-008).
        dataHora: new Date(dataHora).toISOString(),
        duracaoMinutos: Number(duracaoMinutos),
        local: local.trim()
      });

      // Valor calculado pela API; o cliente não faz política de preço.
      setSucesso(
        `Passeio solicitado! Valor: ${FORMATO_MOEDA.format(passeio.valor)} · ` +
          `Status: ${ROTULO_STATUS_PASSEIO[passeio.status]}.`
      );

      setTimeout(() => {
        navigate('/passeios');
      }, 1500);
    } catch (falha) {
      setErro(falha.message);
      setEnviando(false);
    }
  };

  if (!ehTutor) {
    return (
      <div className="pagina-solicitar">
        <Header />

        <main className="solicitar-conteudo">
          <h1 className="solicitar-titulo">Solicitar passeio</h1>

          <div className="solicitar-aviso">
            <p className="solicitar-aviso-texto">
              Apenas usuários com perfil de tutor podem solicitar passeios.
            </p>
            <Link className="solicitar-link" to="/home">
              Voltar para a Home
            </Link>
          </div>
        </main>
      </div>
    );
  }

  if (carregando) {
    return (
      <div className="pagina-solicitar">
        <Header />

        <main className="solicitar-conteudo">
          <h1 className="solicitar-titulo">Solicitar passeio</h1>
          <p className="solicitar-carregando">Carregando...</p>
        </main>
      </div>
    );
  }

  if (erroCarga) {
    return (
      <div className="pagina-solicitar">
        <Header />

        <main className="solicitar-conteudo">
          <h1 className="solicitar-titulo">Solicitar passeio</h1>

          <div className="solicitar-aviso">
            <p className="solicitar-aviso-texto">{erroCarga}</p>
            <Link className="solicitar-link" to="/passeadores">
              Voltar para passeadores
            </Link>
          </div>
        </main>
      </div>
    );
  }

  if (!passeador) {
    return (
      <div className="pagina-solicitar">
        <Header />

        <main className="solicitar-conteudo">
          <h1 className="solicitar-titulo">Solicitar passeio</h1>

          <div className="solicitar-aviso">
            <p className="solicitar-aviso-texto">Passeador não encontrado.</p>
            <Link className="solicitar-link" to="/passeadores">
              Voltar para passeadores
            </Link>
          </div>
        </main>
      </div>
    );
  }

  if (pets.length === 0) {
    return (
      <div className="pagina-solicitar">
        <Header />

        <main className="solicitar-conteudo">
          <h1 className="solicitar-titulo">Solicitar passeio</h1>

          <div className="solicitar-aviso">
            <p className="solicitar-aviso-texto">
              Cadastre um pet antes de solicitar um passeio.
            </p>
            <Link className="solicitar-link" to="/pets/novo">
              Cadastrar pet
            </Link>
          </div>
        </main>
      </div>
    );
  }

  return (
    <div className="pagina-solicitar">
      <Header />

      <main className="solicitar-conteudo">
        <h1 className="solicitar-titulo">Solicitar passeio</h1>

        <div className="solicitar-passeador">
          <p className="solicitar-passeador-nome">{passeador.nome}</p>
          <p className="solicitar-passeador-detalhe">
            {FORMATO_MOEDA.format(passeador.valorHora)} por hora
          </p>
        </div>

        <form className="solicitar-form" onSubmit={handleSubmit}>
          <div className="solicitar-campo">
            <label htmlFor="pet">Pet</label>
            <select
              id="pet"
              value={petId}
              onChange={(evento) => setPetId(evento.target.value)}
              required
            >
              {pets.map((pet) => (
                <option key={pet.id} value={pet.id}>
                  {pet.nome}
                </option>
              ))}
            </select>
          </div>

          <div className="solicitar-linha">
            <div className="solicitar-campo">
              <label htmlFor="dataHora">Data e hora</label>
              <input
                type="datetime-local"
                id="dataHora"
                value={dataHora}
                min={paraDataHoraLocal(new Date())}
                onChange={(evento) => setDataHora(evento.target.value)}
                required
              />
            </div>

            <div className="solicitar-campo">
              <label htmlFor="duracaoMinutos">Duração (minutos)</label>
              <input
                type="number"
                id="duracaoMinutos"
                min="15"
                step="15"
                value={duracaoMinutos}
                onChange={(evento) => setDuracaoMinutos(evento.target.value)}
                required
              />
            </div>
          </div>

          <div className="solicitar-campo">
            <label htmlFor="local">Local do passeio</label>
            <input
              type="text"
              id="local"
              value={local}
              onChange={(evento) => setLocal(evento.target.value)}
              required
            />
          </div>

          {erro && <p className="solicitar-erro">{erro}</p>}
          {sucesso && <p className="solicitar-sucesso">{sucesso}</p>}

          <div className="solicitar-acoes">
            <button
              type="button"
              className="solicitar-botao solicitar-botao-secundario"
              onClick={() => navigate('/passeadores')}
              disabled={enviando}
            >
              Cancelar
            </button>
            <button type="submit" className="solicitar-botao" disabled={enviando}>
              Solicitar passeio
            </button>
          </div>
        </form>
      </main>
    </div>
  );
};

export default SolicitarPasseioPage;
