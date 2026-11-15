import Icon from './Icon';
import '../styles/patinhas.css';

// Posição (x/y em % do container), tamanho (px), duração/atraso (s) e giro (graus).
// Valores fixos: o efeito é igual a cada visita e não depende de Math.random.
const PATINHAS = [
  { x: 3, y: 10, tam: 30, dur: 15, atraso: 0, rot: -18 },
  { x: 93, y: 18, tam: 26, dur: 17, atraso: 3, rot: 22 },
  { x: 8, y: 42, tam: 22, dur: 13, atraso: 6, rot: 12 },
  { x: 90, y: 50, tam: 34, dur: 18, atraso: 1, rot: -25 },
  { x: 5, y: 72, tam: 28, dur: 16, atraso: 4, rot: 20 },
  { x: 95, y: 82, tam: 24, dur: 14, atraso: 7, rot: -10 },
  { x: 48, y: 6, tam: 20, dur: 19, atraso: 2, rot: 8 },
  { x: 30, y: 92, tam: 22, dur: 15, atraso: 5, rot: -15 },
  { x: 70, y: 64, tam: 20, dur: 20, atraso: 8, rot: 18 },
  { x: 18, y: 28, tam: 18, dur: 16, atraso: 9, rot: -22 },
  { x: 82, y: 34, tam: 18, dur: 18, atraso: 10, rot: 14 },
  { x: 60, y: 90, tam: 24, dur: 17, atraso: 11, rot: -8 }
];

// Patinhas decorativas que flutuam no fundo do container pai (precisa ter
// position: relative/absolute). Fica atrás do conteúdo, ignora cliques e só anima
// transform e opacity, então não pesa na página.
export default function PatinhasFlutuantes({ quantidade = PATINHAS.length }) {
  return (
    <div className="patinhas" aria-hidden="true">
      {PATINHAS.slice(0, quantidade).map((p, i) => (
        <span
          key={i}
          className="patinha"
          style={{
            '--x': `${p.x}%`,
            '--y': `${p.y}%`,
            '--tam': `${p.tam}px`,
            '--dur': `${p.dur}s`,
            '--atraso': `${p.atraso}s`,
            '--rot': `${p.rot}deg`
          }}
        >
          <Icon nome="pata" />
        </span>
      ))}
    </div>
  );
}
