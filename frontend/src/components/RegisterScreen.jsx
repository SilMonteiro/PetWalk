import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import '../styles/login.css';

const RegisterScreen = () => {
    const navigate = useNavigate();

    const [nome, setNome] = useState('');
    const [email, setEmail] = useState('');
    const [telefone, setTelefone] = useState('');
    const [endereco, setEndereco] = useState('');
    const [tipoPerfil, setTipoPerfil] = useState(0); // 0 = TUTOR, 1 = PASSEADOR, 2 = AMBOS
    const [senha, setSenha] = useState('');

    const [erro, setErro] = useState('');
    const [sucesso, setSucesso] = useState('');

    const handleSubmit = async (e) => {
        e.preventDefault();
        setErro('');
        setSucesso('');

        const dadosCadastro = {
            nome,
            email,
            telefone,
            endereco,
            tipoPerfil: Number(tipoPerfil),
            senha
        };

        try {
            const listaUsuarios = JSON.parse(localStorage.getItem('usuarios_petwalk')) || [];
            listaUsuarios.push({ ...dadosCadastro, id: Date.now() });
            localStorage.setItem('usuarios_petwalk', JSON.stringify(listaUsuarios));
        } catch (errCache) {
            console.error('Erro ao salvar no cache:', errCache);
        }

        try {
            const resposta = await fetch('http://localhost:5229/cadastro', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(dadosCadastro)
            });

            if (resposta.ok) {
                const dados = await resposta.json();
                setSucesso(dados.mensagem || 'Usuário cadastrado com sucesso!');
                
                setTimeout(() => {
                    navigate('/home');
                }, 1500);
            } else {
                const textoErro = await resposta.text();
                setErro(textoErro || 'Erro ao realizar cadastro.');
            }
        } catch (err) {
            console.error('Erro de conexão:', err);
            setErro('Não foi possível conectar ao servidor C#.');
        }
    };

    return (
        <div className="container-registration">
            <h1>Cadastro de Usuário</h1>
            <form onSubmit={handleSubmit} className="form-registration">
                <div className="campo">
                    <label htmlFor="nome">Nome:</label>
                    <input
                        type="text"
                        id="nome"
                        value={nome}
                        onChange={(e) => setNome(e.target.value)}
                        required
                    />
                </div>
                <div className="campo">
                    <label htmlFor="e-mail">E-mail:</label>
                    <input
                        type="email"
                        id="e-mail"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        required
                    />
                </div>
                <div className="campo">
                    <label htmlFor="telefone">Telefone:</label>
                    <input
                        type="number"
                        id="telefone"
                        value={telefone}
                        onChange={(e) => setTelefone(e.target.value)}
                        required
                    />
                </div>
                <div className="campo">
                    <label htmlFor="endereco">Endereço:</label>
                    <input
                        type="text"
                        id="endereco"
                        value={endereco}
                        onChange={(e) => setEndereco(e.target.value)}
                        required
                    />
                </div>
                <div className="campo">
                    <label htmlFor="Tipo">Tipo de Perfil:</label>
                    <select
                        id="Tipo"
                        value={tipoPerfil}
                        onChange={(e) => setTipoPerfil(e.target.value)}
                        required
                    >
                        <option value="0">Tutor</option>
                        <option value="1">Passeador</option>
                        <option value="2">Ambos</option>
                    </select>
                </div>
                <div className="campo">
                    <label htmlFor="senha">Senha:</label>
                    <input
                        type="password"
                        id="senha"
                        value={senha}
                        onChange={(e) => setSenha(e.target.value)}
                        required
                    />
                </div>
                {erro && <p className="mensagem-erro">{erro}</p>}
                {sucesso && <p className="mensagem-sucesso">{sucesso}</p>}

                <button type="submit" className="botao-cadastrar">Cadastrar</button>
            </form>
        </div>
    );
};

export default RegisterScreen;