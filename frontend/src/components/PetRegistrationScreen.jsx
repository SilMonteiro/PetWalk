import React, { useState } from 'react';
import '../styles/pet-registration.css';

const PetRegistrationScreen = () => {
    const [nomePet, setNomePet] = useState('');
    const [tipo, setTipo] = useState('');
    const [idade, setIdade] = useState('');

    const handleSubmit = (e) => {
        e.preventDefault();
        console.log({ nomePet, tipo, idade });
    };

    return (
        <div className="container-pet-registration">
            <h1>Cadastro de Pet</h1>
            <form onSubmit={handleSubmit} className="form-pet-registration">
                <div className="campo">
                    <label htmlFor="nomePet">Nome do Pet:</label>
                    <input
                        type="text"
                        id="nomePet"
                        value={nomePet}
                        onChange={(e) => setNomePet(e.target.value)}
                        required
                    />
                </div>
                <div className="campo">
                    <label htmlFor="tipo">Tipo:</label>
                    <input
                        type="text"
                        id="tipo"
                        value={tipo}
                        onChange={(e) => setTipo(e.target.value)}
                        required
                    />
                </div>
                <div className="campo">
                    <label htmlFor="idade">Idade:</label>
                    <input
                        type="number"
                        id="idade"
                        value={idade}
                        onChange={(e) => setIdade(e.target.value)}
                        required
                    />
                </div>
                <button type="submit" className="botao-cadastrar">Cadastrar</button>
            </form>
        </div>
    );
};

export default PetRegistrationScreen;