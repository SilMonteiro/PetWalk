import { Link } from 'react-router-dom';

// Logo oficial (arquivos em /public/brand). `variante="icone"` usa só o pin com a trilha.
export default function Logo({ variante = 'completa', para = '/', altura = 44, className = '' }) {
  const src = variante === 'icone' ? '/brand/petwalk-icon.png' : '/brand/petwalk-logo.png';

  return (
    <Link to={para} className={`logo ${className}`.trim()} aria-label="PetWalk — página inicial">
      <img src={src} alt="PetWalk" style={{ height: altura, width: 'auto' }} />
    </Link>
  );
}
