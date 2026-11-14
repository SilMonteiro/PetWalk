import { useEffect } from 'react';

// Revela elementos .revelar quando entram na tela (respeita "reduzir movimento" via CSS).
export default function useReveal(dependencia) {
  useEffect(() => {
    const elementos = document.querySelectorAll('.revelar:not(.visivel)');

    if (!('IntersectionObserver' in window)) {
      elementos.forEach((el) => el.classList.add('visivel'));
      return undefined;
    }

    const observador = new IntersectionObserver(
      (entradas) => {
        entradas.forEach((entrada) => {
          if (entrada.isIntersecting) {
            entrada.target.classList.add('visivel');
            observador.unobserve(entrada.target);
          }
        });
      },
      { threshold: 0.12 }
    );

    elementos.forEach((el) => observador.observe(el));

    return () => observador.disconnect();
  }, [dependencia]);
}
