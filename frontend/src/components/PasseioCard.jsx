import { useState } from 'react';
import AvaliacaoForm from './AvaliacaoForm';
import { useNotificacoes } from '../context/NotificacoesContext';
import { aceitar, cancelar, finalizar, iniciar, recusar } from '../services/passeioService';
import { PAPEL, ROTULO_STATUS_PASSEIO } from '../constants/enums';
import { acoesDisponiveis } from '../utils/passeioAcoes';

const FORMATO_MOEDA = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL'
});

const FORMATO_NOTA = new Intl.NumberFormat('pt-BR', {
  minimumFractionDigits: 1,
  maximumFractionDigits: 1
});

const ROTULO_ACAO = {
  aceitar: 'Aceitar',
  recusar: 'Recusar',
  iniciar: 'Iniciar',
  finalizar: 'Finalizar',
  cancelar: 'Cancelar',
  avaliar: 'Avaliar'
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

  const podeMostrarAcoes =
    acoes.length > 0 && !avaliacao && !avaliacaoIndisponivel;

  return (
    <li className="passeio-card">
      <div className="passeio-topo">
        <h2 className="passeio-pet">{passeio.nomePet}</h2>
        <span className={`passeio-status passeio-status-${passeio.status}`}>
          {ROTULO_STATUS_PASSEIO[passeio.status]}
        </span>
      </div>

      <p className="passeio-detalhe">
        Data e hora: {new Date(passeio.dataHora).toLocaleString('pt-BR')}
      </p>
      <p className="passeio-detalhe">Duração: {passeio.duracaoMinutos} minutos</p>
      <p className="passeio-detalhe">Local: {passeio.local}</p>
      <p className="passeio-detalhe">Valor: {FORMATO_MOEDA.format(passeio.valor)}</p>
      <p className="passeio-detalhe">
        {ehTutor ? 'Passeador' : 'Tutor'}: {outraParte.nome}
      </p>

      {erro && <p className="passeio-erro">{erro}</p>}

      {avaliacao && (
        <p className="passeio-avaliacao-sucesso">
          Avaliação registrada: nota {avaliacao.nota}. Nova média de{' '}
          {avaliacao.avaliado.nome}:{' '}
          {FORMATO_NOTA.format(avaliacao.mediaAvaliacao)}.
        </p>
      )}

      {confirmandoCancelamento ? (
        <div className="passeio-confirmacao">
          <p className="passeio-confirmacao-texto">Cancelar este passeio?</p>

          <div className="passeio-acoes">
            <button
              type="button"
              className="passeio-botao passeio-botao-secundario"
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
              className="passeio-botao passeio-botao-perigo"
              onClick={() => executar('cancelar')}
              disabled={ocupado}
            >
              Confirmar
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
                className={`passeio-botao passeio-botao-${acao}`}
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
                {ROTULO_ACAO[acao]}
              </button>
            ))}
          </div>
        )
      )}
    </li>
  );
}
