import Icon from './Icon';
import { ROTULO_STATUS_PASSEIO } from '../constants/enums';
import { ETAPAS_FLUXO, VISUAL_STATUS } from '../constants/statusPasseio';

// Linha do tempo do passeio. Para Recusado/Cancelado a API não informa em que etapa
// o passeio parou, então mostramos só o desfecho, sem inventar progresso.
export default function PasseioTimeline({ status }) {
  const indiceAtual = ETAPAS_FLUXO.indexOf(status);

  if (indiceAtual === -1) {
    return (
      <div className={`timeline-final timeline-final--${status}`}>
        <Icon nome={VISUAL_STATUS[status].icone} />
        <span>{ROTULO_STATUS_PASSEIO[status]}</span>
      </div>
    );
  }

  return (
    <ol className="timeline" aria-label="Andamento do passeio">
      {ETAPAS_FLUXO.map((etapa, indice) => {
        const estado =
          indice < indiceAtual ? 'feita' : indice === indiceAtual ? 'atual' : 'futura';

        return (
          <li
            key={etapa}
            className={`timeline-etapa timeline-etapa--${estado}`}
            aria-current={estado === 'atual' ? 'step' : undefined}
          >
            <span className="timeline-ponto">
              {estado === 'feita' || (estado === 'atual' && etapa === ETAPAS_FLUXO[3]) ? (
                <Icon nome="check" />
              ) : null}
            </span>
            <span className="timeline-rotulo">{ROTULO_STATUS_PASSEIO[etapa]}</span>
          </li>
        );
      })}
    </ol>
  );
}
