import { createContext, useContext, useState } from 'react';

const CHAVE_USUARIO = 'petwalk_usuario';

const AuthContext = createContext(null);

function lerUsuarioSalvo() {
  try {
    const salvo = localStorage.getItem(CHAVE_USUARIO);
    return salvo ? JSON.parse(salvo) : null;
  } catch (erro) {
    // Conteúdo inválido não pode derrubar o app: sem sessão, volta ao login.
    console.error('Usuário salvo inválido:', erro);
    return null;
  }
}

export function AuthProvider({ children }) {
  const [usuario, setUsuario] = useState(lerUsuarioSalvo);

  const entrar = (dados) => {
    // Sem JWT: a sessão é este objeto. Só os campos do contexto, nunca a senha.
    const usuarioLogado = {
      id: dados.id,
      nome: dados.nome,
      email: dados.email,
      tipoPerfil: dados.tipoPerfil,
      endereco: dados.endereco
    };

    localStorage.setItem(CHAVE_USUARIO, JSON.stringify(usuarioLogado));
    setUsuario(usuarioLogado);
  };

  const sair = () => {
    localStorage.removeItem(CHAVE_USUARIO);
    setUsuario(null);
  };

  return (
    <AuthContext.Provider value={{ usuario, entrar, sair }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const contexto = useContext(AuthContext);

  if (!contexto) {
    throw new Error('useAuth deve ser usado dentro de AuthProvider');
  }

  return contexto;
}
