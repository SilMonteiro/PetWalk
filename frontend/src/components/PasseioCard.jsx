import { useState } from 'react';
import { aceitar, cancelar, finalizar, iniciar, recusar } from '../services/passeioService';
import { PAPEL, ROTULO_STATUS_PASSEIO } from '../constants/enums';
import { acoesDisponiveis } from '../utils/passeioAcoes';

const FORMATO_MOEDA = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL'
});

const ROTULO_ACAO = {
  aceitar: 'Aceitar',
  recusar: 'Recusar',
  iniciar: 'Iniciar',
  finalizar: 'Finalizar',
  cancelar: 'Cancelar'
};

const EXECUTORES = { aceitar, recusar, iniciar, finalizar, cancelar };

export default function PasseioCard({ passeio, usuarioId, onAtualizado }) {
  const [acaoEmAndamento, setAcaoEmAndamento] = useState(null);
  const [confirmandoCancelamento, setConfirmandoCancelamento] = useState(false);
  const [erro, setErro] = useState('');

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
    } catch (falha) {
      setErro(falha.message);
    } finally {
      setAcaoEmAndamento(null);
    }
  };

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
      ) : (
        acoes.length > 0 && (
          <div className="passeio-acoes">
            {acoes.map((acao) =>
              acao === 'avaliar' ? (
                <button
                  key={acao}
                  type="button"
                  className="passeio-botao passeio-botao-secundario"
                  disabled
                  title="A avaliação será implementada na próxima etapa"
                >
                  Avaliar
                </button>
              ) : (
                <button
                  key={acao}
                  type="button"
                  className={`passeio-botao passeio-botao-${acao}`}
                  onClick={() =>
                    acao === 'cancelar'
                      ? setConfirmandoCancelamento(true)
                      : executar(acao)
                  }
                  disabled={ocupado}
                >
                  {ROTULO_ACAO[acao]}
                </button>
              )
            )}
          </div>
        )
      )}
    </li>
  );
}
