import React from 'react';
import { useNavigate } from 'react-router-dom';
import '../styles/home.css';

const HomeScreen = () => {
    const navigate = useNavigate();

    return (
        <div className="container-home">
            <header className="header-home">
                <h1>🐾 PetWalk</h1>
                <button className="botao-sair" onClick={() => navigate('/')}>
                    Sair
                </button>
            </header>

            <main className="main-home">
                <h2>Bem-vindo ao PetWalk!</h2>
                <p>Plataforma de intermediação de passeios com pets</p>

                <div className="cards-home">
                    <div className="card" onClick={() => navigate('/registro-pet')}>
                        <span className="card-icon">🐶</span>
                        <h3>Cadastrar Pet</h3>
                        <p>Registre seu animal de estimação</p>
                    </div>

                    <div className="card card-desabilitado">
                        <span className="card-icon">🚶</span>
                        <h3>Agendar Passeio</h3>
                        <p>Em breve</p>
                    </div>

                    <div className="card card-desabilitado">
                        <span className="card-icon">⭐</span>
                        <h3>Avaliações</h3>
                        <p>Em breve</p>
                    </div>
                </div>
            </main>
        </div>
    );
};

export default HomeScreen;
