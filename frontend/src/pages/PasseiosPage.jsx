import { useEffect, useState } from 'react';
import { PageHeading, EstadoVazio } from '../components/PageShell';
import PageShell from '../components/PageShell';
import PasseioCard from '../components/PasseioCard';
import Icon from '../components/Icon';
import { useAuth } from '../context/AuthContext';
import { listarPasseios } from '../services/passeioService';
import { ROTULO_STATUS_PASSEIO, STATUS_PASSEIO } from '../constants/enums';
import '../styles/passeios.css';

const PasseiosPage = () => {
  const { usuario } = useAuth();

  const [passeios, setPasseios] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');
  const [filtro, setFiltro] = useState('todos');

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
  const ordenados = [...passeios].sort((a, b) => new Date(b.dataHora) - new Date(a.dataHora));
  const visiveis =
    filtro === 'todos' ? ordenados : ordenados.filter((passeio) => passeio.status === filtro);

  const contar = (status) => passeios.filter((passeio) => passeio.status === status).length;

  // Só mostra filtros de status que existem na lista, para não poluir a tela.
  const filtros = Object.values(STATUS_PASSEIO).filter((status) => contar(status) > 0);

  return (
    <PageShell largura="media">
      <PageHeading
        icone={<Icon nome="calendar" />}
        titulo="Meus passeios"
        subtitulo="Acompanhe cada etapa das suas solicitações."
      />

      {carregando && (
        <div className="carregando" role="status">
          <span className="spinner" />
          Carregando passeios...
        </div>
      )}

      {!carregando && erro && (
        <div className="bloco-erro">
          <p className="alerta alerta--erro" role="alert">
            <Icon nome="alert" />
            {erro}
          </p>
          <button type="button" className="btn btn--outline" onClick={carregar}>
            Tentar de novo
          </button>
        </div>
      )}

      {!carregando && !erro && ordenados.length === 0 && (
        <EstadoVazio
          icone={<Icon nome="calendar" />}
          titulo="Nenhum passeio encontrado"
          texto="Quando você solicitar ou receber um passeio, ele aparece aqui com o status atualizado."
        />
      )}

      {!carregando && !erro && ordenados.length > 0 && (
        <>
          {filtros.length > 1 && (
            <div className="passeios-filtros" role="group" aria-label="Filtrar por status">
              <button
                type="button"
                className="filtro"
                aria-pressed={filtro === 'todos'}
                onClick={() => setFiltro('todos')}
              >
                Todos <span className="filtro-contagem">{passeios.length}</span>
              </button>

              {filtros.map((status) => (
                <button
                  key={status}
                  type="button"
                  className="filtro"
                  aria-pressed={filtro === status}
                  onClick={() => setFiltro(status)}
                >
                  {ROTULO_STATUS_PASSEIO[status]}
                  <span className="filtro-contagem">{contar(status)}</span>
                </button>
              ))}
            </div>
          )}

          <ul className="passeios-lista">
            {visiveis.map((passeio) => (
              <PasseioCard
                key={passeio.id}
                passeio={passeio}
                usuarioId={usuario.id}
                onAtualizado={handleAtualizado}
              />
            ))}
          </ul>
        </>
      )}
    </PageShell>
  );
};

export default PasseiosPage;
