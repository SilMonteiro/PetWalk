import React, { useState } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import AuthLayout from '../components/AuthLayout';
import Icon from '../components/Icon';
import { useAuth } from '../context/AuthContext';
import { login } from '../services/usuarioService';

const LoginPage = () => {
  const { usuario, entrar } = useAuth();
  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const [erro, setErro] = useState('');
  const [enviando, setEnviando] = useState(false);
  const navigate = useNavigate();

  if (usuario) {
    return <Navigate to="/home" replace />;
  }

  const handleSubmit = async (e) => {
    e.preventDefault();
    setErro('');
    setEnviando(true);

    try {
      const dados = await login(email, senha);
      entrar(dados);
      navigate('/home');
    } catch (falha) {
      setErro(falha.message);
    } finally {
      setEnviando(false);
    }
  };

  return (
    <AuthLayout
      titulo="Que bom te ver!"
      subtitulo="Entre para acompanhar seus passeios."
      rodape={
        <>
          Você ainda não possui cadastro? <Link to="/cadastro">Cadastre-se aqui</Link>
        </>
      }
    >
      <form onSubmit={handleSubmit} className="auth-form">
        <div className="campo">
          <label htmlFor="email">E-mail</label>
          <div className="input-com-icone">
            <Icon nome="mail" />
            <input
              type="email"
              id="email"
              className="input"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="voce@exemplo.com"
              autoComplete="email"
              required
            />
          </div>
        </div>

        <div className="campo">
          <label htmlFor="senha">Senha</label>
          <div className="input-com-icone">
            <Icon nome="lock" />
            <input
              type="password"
              id="senha"
              className="input"
              value={senha}
              onChange={(e) => setSenha(e.target.value)}
              placeholder="Sua senha"
              autoComplete="current-password"
              required
            />
          </div>
        </div>

        {erro && (
          <p className="alerta alerta--erro" role="alert">
            <Icon nome="alert" />
            {erro}
          </p>
        )}

        <button type="submit" className="btn btn--lg btn--block" disabled={enviando}>
          {enviando ? <span className="spinner" /> : null}
          {enviando ? 'Entrando...' : 'Entrar'}
        </button>
      </form>
    </AuthLayout>
  );
};

export default LoginPage;
