import { useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import PageShell, { EstadoVazio, PageHeading } from '../components/PageShell';
import AvatarIniciais from '../components/AvatarIniciais';
import Icon from '../components/Icon';
import { useAuth } from '../context/AuthContext';
import { listarPasseadores } from '../services/passeadorService';
import { listarPets } from '../services/petService';
import { solicitarPasseio } from '../services/passeioService';
import { useNotificacoes } from '../context/NotificacoesContext';
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
  const { recarregar } = useNotificacoes();
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

      recarregar();

      setTimeout(() => {
        navigate('/passeios');
      }, 1500);
    } catch (falha) {
      setErro(falha.message);
      setEnviando(false);
    }
  };

  const aviso = (texto, destino, rotuloDestino) => (
    <PageShell largura="estreita">
      <PageHeading icone={<Icon nome="send" />} titulo="Solicitar passeio" />

      <EstadoVazio
        icone={<Icon nome="info" />}
        titulo={texto}
        acao={
          <Link className="btn" to={destino}>
            {rotuloDestino}
          </Link>
        }
      />
    </PageShell>
  );

  if (!ehTutor) {
    return aviso(
      'Apenas usuários com perfil de tutor podem solicitar passeios.',
      '/home',
      'Voltar para o início'
    );
  }

  if (carregando) {
    return (
      <PageShell largura="estreita">
        <PageHeading icone={<Icon nome="send" />} titulo="Solicitar passeio" />
        <div className="carregando" role="status">
          <span className="spinner" />
          Carregando...
        </div>
      </PageShell>
    );
  }

  if (erroCarga) {
    return aviso(erroCarga, '/passeadores', 'Voltar para passeadores');
  }

  if (!passeador) {
    return aviso('Passeador não encontrado.', '/passeadores', 'Voltar para passeadores');
  }

  if (pets.length === 0) {
    return aviso('Cadastre um pet antes de solicitar um passeio.', '/pets/novo', 'Cadastrar pet');
  }

  return (
    <PageShell largura="estreita">
      <PageHeading
        icone={<Icon nome="send" />}
        titulo="Solicitar passeio"
        subtitulo="Informe quando e onde o passeio vai acontecer."
      />

      <div className="solicitar-passeador">
        <AvatarIniciais nome={passeador.nome} tamanho="lg" />
        <div>
          <p className="solicitar-passeador-nome">{passeador.nome}</p>
          <p className="solicitar-passeador-detalhe">
            {FORMATO_MOEDA.format(passeador.valorHora)} por hora
          </p>
        </div>
        <span className="pill pill--verde">O valor final é calculado ao solicitar</span>
      </div>

      <form className="form-card" onSubmit={handleSubmit}>
        <div className="campo">
          <label htmlFor="pet">Pet</label>
          <select
            id="pet"
            className="select"
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

        <div className="linha-campos">
          <div className="campo">
            <label htmlFor="dataHora">Data e hora</label>
            <input
              type="datetime-local"
              id="dataHora"
              className="input"
              value={dataHora}
              min={paraDataHoraLocal(new Date())}
              onChange={(evento) => setDataHora(evento.target.value)}
              required
            />
          </div>

          <div className="campo">
            <label htmlFor="duracaoMinutos">Duração (minutos)</label>
            <input
              type="number"
              id="duracaoMinutos"
              className="input"
              min="15"
              step="15"
              value={duracaoMinutos}
              onChange={(evento) => setDuracaoMinutos(evento.target.value)}
              placeholder="Ex.: 60"
              required
            />
          </div>
        </div>

        <div className="campo">
          <label htmlFor="local">Local do passeio</label>
          <div className="input-com-icone">
            <Icon nome="pin" />
            <input
              type="text"
              id="local"
              className="input"
              value={local}
              onChange={(evento) => setLocal(evento.target.value)}
              required
            />
          </div>
        </div>

        {erro && (
          <p className="alerta alerta--erro" role="alert">
            <Icon nome="alert" />
            {erro}
          </p>
        )}
        {sucesso && (
          <p className="alerta alerta--sucesso" role="status">
            <Icon nome="check" />
            {sucesso}
          </p>
        )}

        <div className="acoes-linha">
          <button
            type="button"
            className="btn btn--ghost"
            onClick={() => navigate('/passeadores')}
            disabled={enviando}
          >
            Cancelar
          </button>
          <button type="submit" className="btn" disabled={enviando}>
            {enviando ? <span className="spinner" /> : <Icon nome="send" />}
            Solicitar passeio
          </button>
        </div>
      </form>
    </PageShell>
  );
};

export default SolicitarPasseioPage;
