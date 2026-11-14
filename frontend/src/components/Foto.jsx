import { useState } from 'react';
import Icon from './Icon';

// Foto real de /public/img. Se o arquivo ainda não existir, mostra um bloco
// decorativo com patinha em vez de uma imagem quebrada.
export default function Foto({ src, alt, className = '', posicao = 'center' }) {
  const [falhou, setFalhou] = useState(false);

  if (falhou) {
    return (
      <div className={`foto foto--vazia ${className}`.trim()} role="img" aria-label={alt}>
        <Icon nome="pata" />
      </div>
    );
  }

  return (
    <img
      className={`foto ${className}`.trim()}
      src={src}
      alt={alt}
      style={{ objectPosition: posicao }}
      loading="lazy"
      onError={() => setFalhou(true)}
    />
  );
}
