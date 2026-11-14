import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Icon from '../components/Icon';
import PageShell, { EstadoVazio, PageHeading } from '../components/PageShell';
import { useNotificacoes } from '../context/NotificacoesContext';
import { TIPO_NOTIFICACAO } from '../constants/enums';
import '../styles/notificacoes.css';

const ICONE_TIPO = {
  [TIPO_NOTIFICACAO.SOLICITADO]: 'send',
  [TIPO_NOTIFICACAO.ACEITO]: 'check',
  [TIPO_NOTIFICACAO.RECUSADO]: 'ban',
  [TIPO_NOTIFICACAO.INICIADO]: 'play',
  [TIPO_NOTIFICACAO.FINALIZADO]: 'flag',
  [TIPO_NOTIFICACAO.CANCELADO]: 'x'
};

const NotificacoesPage = () => {
  const { notificacoes, naoLidas, carregando, erro, recarregar, marcarComoLida } =
    useNotificacoes();
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
    <PageShell largura="estreita">
      <PageHeading
        icone={<Icon nome="bell" />}
        titulo="Notificações"
        subtitulo={
          naoLidas > 0
            ? `${naoLidas} ${naoLidas === 1 ? 'nova' : 'novas'} para você.`
            : 'Você está em dia.'
        }
      />

      {erroAcao && (
        <p className="alerta alerta--erro" role="alert">
          <Icon nome="alert" />
          {erroAcao}
        </p>
      )}

      {carregando && (
        <div className="carregando" role="status">
          <span className="spinner" />
          Carregando notificações...
        </div>
      )}

      {!carregando && erro && (
        <div className="bloco-erro">
          <p className="alerta alerta--erro" role="alert">
            <Icon nome="alert" />
            {erro}
          </p>
          <button type="button" className="btn btn--outline" onClick={recarregar}>
            Tentar de novo
          </button>
        </div>
      )}

      {!carregando && !erro && notificacoes.length === 0 && (
        <EstadoVazio
          icone={<Icon nome="bell" />}
          titulo="Nenhuma notificação"
          texto="Avisos sobre solicitações e passeios aparecem aqui."
        />
      )}

      {!carregando && !erro && notificacoes.length > 0 && (
        <ul className="notificacoes-lista">
          {notificacoes.map((notificacao) => (
            <li key={notificacao.id}>
              <button
                type="button"
                className={`notificacao-item${notificacao.lida ? '' : ' notificacao-nao-lida'}`}
                onClick={() => abrir(notificacao)}
              >
                <span className={`notificacao-icone notificacao-icone--${notificacao.tipo}`}>
                  <Icon nome={ICONE_TIPO[notificacao.tipo] ?? 'bell'} />
                </span>

                <span className="notificacao-corpo">
                  <span className="notificacao-mensagem">{notificacao.mensagem}</span>
                  <span className="notificacao-data">
                    {new Date(notificacao.dataEnvio).toLocaleString('pt-BR')}
                  </span>
                </span>

                {!notificacao.lida && <span className="notificacao-marcador">Nova</span>}
              </button>
            </li>
          ))}
        </ul>
      )}
    </PageShell>
  );
};

export default NotificacoesPage;
