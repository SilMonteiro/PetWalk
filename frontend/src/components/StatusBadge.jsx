import Icon from './Icon';
import { ROTULO_STATUS_PASSEIO } from '../constants/enums';
import { VISUAL_STATUS } from '../constants/statusPasseio';

export default function StatusBadge({ status }) {
  const visual = VISUAL_STATUS[status];

  return (
    <span className={`status-badge status-badge--${status}`}>
      {visual && <Icon nome={visual.icone} />}
      {ROTULO_STATUS_PASSEIO[status]}
    </span>
  );
}
