import React, { useState } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { login } from '../services/usuarioService';
import '../styles/login.css';

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
        <div className="tela-login">
            <h1>Login</h1>
            <form onSubmit={handleSubmit} className="form-login">
                <div className="campo">
                    <label htmlFor="email">Email</label>
                    <input
                        type="email"
                        id="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        required
                    />
                </div>
                <div className="campo">
                    <label htmlFor="senha">Senha</label>
                    <input
                        type="password"
                        id="senha"
                        value={senha}
                        onChange={(e) => setSenha(e.target.value)}
                        required
                    />
                </div>
                {erro && <p className="mensagem-erro">{erro}</p>}
                <button type="submit" className="botao-login" disabled={enviando}>
                    Entrar
                </button>
                <p>
    Você ainda não possui cadastro?{' '}
    <button className="cadastro-pet" type="button" onClick={() => navigate('/cadastro')}>
        Cadastre-se aqui
    </button>
</p>
            </form>
        </div>
    );
};

export default LoginPage;
