import { useEffect, useState } from 'react';
import Header from '../components/Header';
import PasseioCard from '../components/PasseioCard';
import { useAuth } from '../context/AuthContext';
import { listarPasseios } from '../services/passeioService';
import '../styles/passeios.css';

const PasseiosPage = () => {
  const { usuario } = useAuth();

  const [passeios, setPasseios] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');

  const carregar = () => {
    setCarregando(true);
    setErro('');

    listarPasseios(usuario.id)
      .then((lista) => setPasseios(lista))
      .catch((falha) => setErro(falha.message))
      .finally(() => setCarregando(false));
  };

  useEffect(() => {
    carregar();
  }, [usuario.id]);

  const handleAtualizado = (passeioAtualizado) => {
    setPasseios((atuais) =>
      atuais.map((passeio) =>
        passeio.id === passeioAtualizado.id ? passeioAtualizado : passeio
      )
    );
  };

  // Mais recente primeiro; não altera o estado original.
  const ordenados = [...passeios].sort(
    (a, b) => new Date(b.dataHora) - new Date(a.dataHora)
  );

  return (
    <div className="pagina-passeios">
      <Header />

      <main className="passeios-conteudo">
        <h1 className="passeios-titulo">Meus passeios</h1>

        {carregando && <p className="passeios-carregando">Carregando passeios...</p>}

        {!carregando && erro && (
          <div className="passeios-erro">
            <p className="passeios-erro-mensagem">{erro}</p>
            <button type="button" className="passeios-botao" onClick={carregar}>
              Tentar de novo
            </button>
          </div>
        )}

        {!carregando && !erro && ordenados.length === 0 && (
          <p className="passeios-vazio">Nenhum passeio encontrado.</p>
        )}

        {!carregando && !erro && ordenados.length > 0 && (
          <ul className="passeios-lista">
            {ordenados.map((passeio) => (
              <PasseioCard
                key={passeio.id}
                passeio={passeio}
                usuarioId={usuario.id}
                onAtualizado={handleAtualizado}
              />
            ))}
          </ul>
        )}
      </main>
    </div>
  );
};

export default PasseiosPage;
