import { useState } from 'react';
import { avaliar } from '../services/passeioService';
import Icon from './Icon';

const MSG_JA_AVALIADO = 'Passeio já avaliado por este usuário';

const ROTULO_NOTA = {
  1: 'Ruim',
  2: 'Regular',
  3: 'Bom',
  4: 'Muito bom',
  5: 'Excelente'
};

export default function AvaliacaoForm({
  passeioId,
  usuarioId,
  onAvaliado,
  onJaAvaliado,
  onCancelar
}) {
  const [nota, setNota] = useState(5);
  const [comentario, setComentario] = useState('');
  const [erro, setErro] = useState('');
  const [enviando, setEnviando] = useState(false);

  const handleSubmit = async (evento) => {
    evento.preventDefault();

    if (enviando) {
      return;
    }

    setErro('');
    setEnviando(true);

    try {
      const avaliacao = await avaliar(passeioId, usuarioId, nota, comentario.trim());
      onAvaliado(avaliacao);
    } catch (falha) {
      setErro(falha.message);

      // A API não informa se o passeio já foi avaliado; a mensagem é a única pista.
      if (falha.message === MSG_JA_AVALIADO) {
        onJaAvaliado();
      }

      setEnviando(false);
    }
  };

  return (
    <div className="avaliacao">
      <h3 className="avaliacao-titulo">Como foi o passeio?</h3>

      <form className="avaliacao-form" onSubmit={handleSubmit}>
        <div className="campo">
          <span className="campo-rotulo" id={`nota-rotulo-${passeioId}`}>
            Nota: {ROTULO_NOTA[nota]}
          </span>

          <div
            className="estrelas"
            role="radiogroup"
            aria-labelledby={`nota-rotulo-${passeioId}`}
          >
            {[1, 2, 3, 4, 5].map((valor) => (
              <button
                key={valor}
                type="button"
                role="radio"
                aria-checked={nota === valor}
                aria-label={`${valor} ${valor === 1 ? 'estrela' : 'estrelas'}`}
                className={`estrela${valor <= nota ? ' cheia' : ''}`}
                onClick={() => setNota(valor)}
                disabled={enviando}
              >
                <Icon nome="star" />
              </button>
            ))}
          </div>
        </div>

        <div className="campo">
          <label htmlFor={`comentario-${passeioId}`}>Comentário</label>
          <textarea
            id={`comentario-${passeioId}`}
            className="textarea"
            value={comentario}
            onChange={(evento) => setComentario(evento.target.value)}
            rows="3"
            placeholder="Conte como foi a experiência"
            disabled={enviando}
          />
        </div>

        {erro && (
          <p className="alerta alerta--erro" role="alert">
            <Icon nome="alert" />
            {erro}
          </p>
        )}

        <div className="acoes-linha">
          <button
            type="button"
            className="btn btn--ghost btn--sm"
            onClick={onCancelar}
            disabled={enviando}
          >
            Cancelar
          </button>
          <button type="submit" className="btn btn--lilas btn--sm" disabled={enviando}>
            {enviando ? <span className="spinner" /> : <Icon nome="send" />}
            Enviar avaliação
          </button>
        </div>
      </form>
    </div>
  );
}
