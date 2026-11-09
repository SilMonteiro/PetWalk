import { createContext, useCallback, useContext, useEffect, useState } from 'react';
import { useAuth } from './AuthContext';
import {
  listarNotificacoes as listarNotificacoesApi,
  marcarComoLida as marcarComoLidaApi
} from '../services/notificacaoService';

const INTERVALO_POLLING_MS = 30000;

const NotificacoesContext = createContext(null);

export function NotificacoesProvider({ children }) {
  const { usuario } = useAuth();
  const usuarioId = usuario?.id ?? null;

  const [notificacoes, setNotificacoes] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');

  const recarregar = useCallback(async () => {
    if (!usuarioId) {
      return;
    }

    try {
      const lista = await listarNotificacoesApi(usuarioId, false);
      setNotificacoes(lista);
      setErro('');
    } catch (falha) {
      setErro(falha.message);
    }
  }, [usuarioId]);

  useEffect(() => {
    if (!usuarioId) {
      // Ao sair: zera a lista e o intervalo é limpo pelo cleanup abaixo.
      setNotificacoes([]);
      setErro('');
      setCarregando(false);
      return;
    }

    let ativo = true;

    setCarregando(true);
    setErro('');

    listarNotificacoesApi(usuarioId, false)
      .then((lista) => {
        if (ativo) {
          setNotificacoes(lista);
        }
      })
      .catch((falha) => {
        if (ativo) {
          setErro(falha.message);
        }
      })
      .finally(() => {
        if (ativo) {
          setCarregando(false);
        }
      });

    const intervalo = setInterval(() => {
      listarNotificacoesApi(usuarioId, false)
        .then((lista) => {
          if (ativo) {
            setNotificacoes(lista);
            setErro('');
          }
        })
        .catch((falha) => {
          if (ativo) {
            setErro(falha.message);
          }
        });
    }, INTERVALO_POLLING_MS);

    return () => {
      ativo = false;
      clearInterval(intervalo);
    };
  }, [usuarioId]);

  const marcarComoLida = async (notificacaoId) => {
    if (!usuarioId) {
      return;
    }

    await marcarComoLidaApi(notificacaoId, usuarioId);

    setNotificacoes((atuais) =>
      atuais.map((notificacao) =>
        notificacao.id === notificacaoId ? { ...notificacao, lida: true } : notificacao
      )
    );
  };

  const naoLidas = notificacoes.filter((notificacao) => !notificacao.lida).length;

  return (
    <NotificacoesContext.Provider
      value={{ notificacoes, naoLidas, carregando, erro, recarregar, marcarComoLida }}
    >
      {children}
    </NotificacoesContext.Provider>
  );
}

export function useNotificacoes() {
  const contexto = useContext(NotificacoesContext);

  if (!contexto) {
    throw new Error('useNotificacoes deve ser usado dentro de NotificacoesProvider');
  }

  return contexto;
}
