import { useState } from 'react';
import AvaliacaoForm from './AvaliacaoForm';
import AvatarIniciais from './AvatarIniciais';
import Icon from './Icon';
import PasseioTimeline from './PasseioTimeline';
import StatusBadge from './StatusBadge';
import { useNotificacoes } from '../context/NotificacoesContext';
import { aceitar, cancelar, finalizar, iniciar, recusar } from '../services/passeioService';
import { PAPEL } from '../constants/enums';
import { VISUAL_STATUS } from '../constants/statusPasseio';
import { acoesDisponiveis } from '../utils/passeioAcoes';

const FORMATO_MOEDA = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL'
});

const FORMATO_NOTA = new Intl.NumberFormat('pt-BR', {
  minimumFractionDigits: 1,
  maximumFractionDigits: 1
});

const FORMATO_DATA = new Intl.DateTimeFormat('pt-BR', {
  weekday: 'short',
  day: '2-digit',
  month: 'short'
});

const FORMATO_HORA = new Intl.DateTimeFormat('pt-BR', {
  hour: '2-digit',
  minute: '2-digit'
});

const ACOES = {
  aceitar: { rotulo: 'Aceitar', icone: 'check', classe: 'btn' },
  recusar: { rotulo: 'Recusar', icone: 'ban', classe: 'btn btn--perigo-suave' },
  iniciar: { rotulo: 'Iniciar passeio', icone: 'play', classe: 'btn' },
  finalizar: { rotulo: 'Finalizar passeio', icone: 'flag', classe: 'btn' },
  cancelar: { rotulo: 'Cancelar', icone: 'x', classe: 'btn btn--perigo-suave' },
  avaliar: { rotulo: 'Avaliar passeador', icone: 'star', classe: 'btn btn--lilas' }
};

const EXECUTORES = { aceitar, recusar, iniciar, finalizar, cancelar };

export default function PasseioCard({ passeio, usuarioId, onAtualizado }) {
  const { recarregar } = useNotificacoes();
  const [acaoEmAndamento, setAcaoEmAndamento] = useState(null);
  const [confirmandoCancelamento, setConfirmandoCancelamento] = useState(false);
  const [erro, setErro] = useState('');
  const [avaliarAberto, setAvaliarAberto] = useState(false);
  const [avaliacao, setAvaliacao] = useState(null);
  const [avaliacaoIndisponivel, setAvaliacaoIndisponivel] = useState(false);

  const acoes = acoesDisponiveis(passeio.papel, passeio.status);
  const ehTutor = passeio.papel === PAPEL.TUTOR;
  const outraParte = ehTutor ? passeio.passeador : passeio.tutor;
  const data = new Date(passeio.dataHora);
  const visual = VISUAL_STATUS[passeio.status];

  const ocupado = acaoEmAndamento !== null;

  const executar = async (acao) => {
    setErro('');
    setAcaoEmAndamento(acao);

    try {
      const atualizado = await EXECUTORES[acao](passeio.id, usuarioId);
      setConfirmandoCancelamento(false);
      onAtualizado(atualizado);
      recarregar();
    } catch (falha) {
      setErro(falha.message);
    } finally {
      setAcaoEmAndamento(null);
    }
  };

  const handleAvaliado = (resultado) => {
    setAvaliarAberto(false);
    setAvaliacao(resultado);
  };

  const handleJaAvaliado = () => {
    setAvaliacaoIndisponivel(true);
  };

  const abrirAvaliacao = () => {
    setErro('');
    setAvaliarAberto(true);
  };

  const podeMostrarAcoes = acoes.length > 0 && !avaliacao && !avaliacaoIndisponivel;

  return (
    <li className={`passeio-card passeio-card--${passeio.status}`}>
      <div className="passeio-topo">
        <div className="passeio-identidade">
          <AvatarIniciais nome={passeio.nomePet} tamanho="lg" />
          <div>
            <h2 className="passeio-pet">{passeio.nomePet}</h2>
            <p className="passeio-parte">
              {ehTutor ? 'Passeador' : 'Tutor'}: <strong>{outraParte.nome}</strong>
            </p>
          </div>
        </div>

        <StatusBadge key={passeio.status} status={passeio.status} />
      </div>

      <PasseioTimeline status={passeio.status} />

      {visual && (
        <p className="passeio-descricao">
          {ehTutor ? visual.descricaoTutor : visual.descricaoPasseador}
        </p>
      )}

      <ul className="passeio-infos">
        <li>
          <Icon nome="calendar" />
          <span>
            <small>Data</small>
            {FORMATO_DATA.format(data)}
          </span>
        </li>
        <li>
          <Icon nome="clock" />
          <span>
            <small>Horário</small>
            {FORMATO_HORA.format(data)} · {passeio.duracaoMinutos} min
          </span>
        </li>
        <li>
          <Icon nome="pin" />
          <span>
            <small>Local</small>
            {passeio.local}
          </span>
        </li>
        <li>
          <Icon nome="wallet" />
          <span>
            <small>Valor</small>
            {FORMATO_MOEDA.format(passeio.valor)}
          </span>
        </li>
      </ul>

      {erro && (
        <p className="alerta alerta--erro" role="alert">
          <Icon nome="alert" />
          {erro}
        </p>
      )}

      {avaliacao && (
        <p className="alerta alerta--sucesso" role="status">
          <Icon nome="check" />
          Avaliação registrada: nota {avaliacao.nota}. Nova média de {avaliacao.avaliado.nome}:{' '}
          {FORMATO_NOTA.format(avaliacao.mediaAvaliacao)}.
        </p>
      )}

      {confirmandoCancelamento ? (
        <div className="passeio-confirmacao">
          <p>Cancelar este passeio?</p>

          <div className="acoes-linha">
            <button
              type="button"
              className="btn btn--ghost btn--sm"
              onClick={() => {
                setConfirmandoCancelamento(false);
                setErro('');
              }}
              disabled={ocupado}
            >
              Voltar
            </button>
            <button
              type="button"
              className="btn btn--perigo btn--sm"
              onClick={() => executar('cancelar')}
              disabled={ocupado}
            >
              {ocupado ? <span className="spinner" /> : <Icon nome="x" />}
              Confirmar cancelamento
            </button>
          </div>
        </div>
      ) : avaliarAberto ? (
        <AvaliacaoForm
          passeioId={passeio.id}
          usuarioId={usuarioId}
          onAvaliado={handleAvaliado}
          onJaAvaliado={handleJaAvaliado}
          onCancelar={() => setAvaliarAberto(false)}
        />
      ) : (
        podeMostrarAcoes && (
          <div className="passeio-acoes">
            {acoes.map((acao) => (
              <button
                key={acao}
                type="button"
                className={`${ACOES[acao].classe} btn--sm`}
                onClick={() => {
                  if (acao === 'cancelar') {
                    setConfirmandoCancelamento(true);
                  } else if (acao === 'avaliar') {
                    abrirAvaliacao();
                  } else {
                    executar(acao);
                  }
                }}
                disabled={ocupado}
              >
                {acaoEmAndamento === acao ? (
                  <span className="spinner" />
                ) : (
                  <Icon nome={ACOES[acao].icone} />
                )}
                {ACOES[acao].rotulo}
              </button>
            ))}
          </div>
        )
      )}
    </li>
  );
}
