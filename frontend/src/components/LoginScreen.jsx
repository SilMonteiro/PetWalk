import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import '../styles/login.css';

// Credenciais mockadas para acesso
const MOCK_EMAIL = 'admin@petwalk.com';
const MOCK_SENHA = '123456';

const LoginScreen = () => {
    const [email, setEmail] = useState('');
    const [senha, setSenha] = useState('');
    const [erro, setErro] = useState('');
    const navigate = useNavigate();

    const handleSubmit = (e) => {
        e.preventDefault();
        setErro('');

        // Validação com credenciais mockadas
        if (email === MOCK_EMAIL && senha === MOCK_SENHA) {
            navigate('/home');
        } else {
            setErro('Email ou senha inválidos. Use: admin@petwalk.com / 123456');
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
                <button type="submit" className="botao-login">Entrar</button>
                <p className="dica-login">
                    Credenciais de teste: <strong>admin@petwalk.com</strong> / <strong>123456</strong>
                </p>
            </form>
        </div>
    );
};

export default LoginScreen;