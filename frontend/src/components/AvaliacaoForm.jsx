import { useState } from 'react';
import { avaliar } from '../services/passeioService';

const MSG_JA_AVALIADO = 'Passeio já avaliado por este usuário';

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
    <div className="passeio-avaliacao">
      <h3 className="passeio-avaliacao-titulo">Avaliar passeio</h3>

      <form className="passeio-avaliacao-form" onSubmit={handleSubmit}>
        <div className="passeio-avaliacao-campo">
          <label htmlFor={`nota-${passeioId}`}>Nota (1 a 5)</label>
          <select
            id={`nota-${passeioId}`}
            value={nota}
            onChange={(evento) => setNota(Number(evento.target.value))}
            disabled={enviando}
          >
            {[1, 2, 3, 4, 5].map((valor) => (
              <option key={valor} value={valor}>
                {valor}
              </option>
            ))}
          </select>
        </div>

        <div className="passeio-avaliacao-campo">
          <label htmlFor={`comentario-${passeioId}`}>Comentário</label>
          <textarea
            id={`comentario-${passeioId}`}
            value={comentario}
            onChange={(evento) => setComentario(evento.target.value)}
            rows="2"
            placeholder="Como foi o passeio?"
            disabled={enviando}
          />
        </div>

        {erro && <p className="passeio-erro">{erro}</p>}

        <div className="passeio-avaliacao-acoes">
          <button
            type="button"
            className="passeio-botao passeio-botao-secundario"
            onClick={onCancelar}
            disabled={enviando}
          >
            Cancelar
          </button>
          <button type="submit" className="passeio-botao" disabled={enviando}>
            Enviar avaliação
          </button>
        </div>
      </form>
    </div>
  );
}
