import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Header from '../components/Header';
import { useNotificacoes } from '../context/NotificacoesContext';
import '../styles/notificacoes.css';

const NotificacoesPage = () => {
  const { notificacoes, carregando, erro, recarregar, marcarComoLida } = useNotificacoes();
  const navigate = useNavigate();

  const [erroAcao, setErroAcao] = useState('');

  const abrir = async (notificacao) => {
    setErroAcao('');

    try {
      await marcarComoLida(notificacao.id);
      navigate('/passeios');
    } catch (falha) {
      setErroAcao(falha.message);
    }
  };

  return (
    <div className="pagina-notificacoes">
      <Header />

      <main className="notificacoes-conteudo">
        <h1 className="notificacoes-titulo">Notificações</h1>

        {erroAcao && <p className="notificacoes-erro">{erroAcao}</p>}

        {carregando && <p className="notificacoes-carregando">Carregando notificações...</p>}

        {!carregando && erro && (
          <div className="notificacoes-erro-box">
            <p className="notificacoes-erro">{erro}</p>
            <button type="button" className="notificacoes-botao" onClick={recarregar}>
              Tentar de novo
            </button>
          </div>
        )}

        {!carregando && !erro && notificacoes.length === 0 && (
          <p className="notificacoes-vazio">Nenhuma notificação.</p>
        )}

        {!carregando && !erro && notificacoes.length > 0 && (
          <ul className="notificacoes-lista">
            {notificacoes.map((notificacao) => (
              <li key={notificacao.id}>
                <button
                  type="button"
                  className={`notificacao-item${
                    notificacao.lida ? '' : ' notificacao-nao-lida'
                  }`}
                  onClick={() => abrir(notificacao)}
                >
                  <span className="notificacao-mensagem">{notificacao.mensagem}</span>
                  <span className="notificacao-data">
                    {new Date(notificacao.dataEnvio).toLocaleString('pt-BR')}
                  </span>
                  {!notificacao.lida && <span className="notificacao-marcador">Não lida</span>}
                </button>
              </li>
            ))}
          </ul>
        )}
      </main>
    </div>
  );
};

export default NotificacoesPage;
