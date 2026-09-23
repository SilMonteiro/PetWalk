import React, { useState } from 'react';
import './login.css';

const LoginScreen = () => {
    const [email, setEmail] = useState('');
    const [senha, setSenha] = useState('');

    const handleSubmit = (e) => {
        e.preventDefault();
        // Handle login logic here
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
                <button type="submit" className="botao-login">Entrar</button>
            </form>
        </div>
    );
};

export default LoginScreen;