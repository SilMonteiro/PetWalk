import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import '../styles/pet-registration.css';

export function PetRegistrationScreen() {
  const navigate = useNavigate();

  const [formData, setFormData] = useState({
    nome: '',
    especie: 'Cão',
    raca: '',
    idade: '',
    porte: 'Médio',
    observacoes: ''
  });

  const [mensagem, setMensagem] = useState({ texto: '', tipo: '' });

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSubmit = (e) => {
    e.preventDefault();

    if (!formData.nome.trim() || !formData.raca.trim() || !formData.idade) {
      setMensagem({ texto: 'Por favor, preencha todos os campos obrigatórios (*).', tipo: 'erro' });
      return;
    }

    // Persistência simulada no navegador (Mock)
    const petsExistentes = JSON.parse(localStorage.getItem('pets')) || [];
    const novoPet = { id: Date.now(), ...formData };
    
    localStorage.setItem('pets', JSON.stringify([...petsExistentes, novoPet]));

    setMensagem({ texto: 'Pet cadastrado com sucesso!', tipo: 'sucesso' });

    setFormData({
      nome: '',
      especie: 'Cão',
      raca: '',
      idade: '',
      porte: 'Médio',
      observacoes: ''
    });

    setTimeout(() => {
      navigate('/home');
    }, 1500);
  };

  return (
    <div className="pet-registration-container">
      <div className="pet-card">
        <h2>Cadastrar Novo Pet</h2>
        <p className="subtitle">Insira as informações do animal de estimação</p>

        {mensagem.texto && (
          <div className={`alert ${mensagem.tipo}`}>
            {mensagem.texto}
          </div>
        )}

        <form onSubmit={handleSubmit} className="pet-form">
          <div className="form-group">
            <label htmlFor="nome">Nome do Pet *</label>
            <input
              type="text"
              id="nome"
              name="nome"
              value={formData.nome}
              onChange={handleChange}
              placeholder="Ex: Mel, Pipoca, Thor..."
              required
            />
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="especie">Espécie *</label>
              <select id="especie" name="especie" value={formData.especie} onChange={handleChange}>
                <option value="Cão">Cão</option>
                <option value="Gato">Gato</option>
                <option value="Outro">Outro</option>
              </select>
            </div>

            <div className="form-group">
              <label htmlFor="porte">Porte *</label>
              <select id="porte" name="porte" value={formData.porte} onChange={handleChange}>
                <option value="Pequeno">Pequeno</option>
                <option value="Médio">Médio</option>
                <option value="Grande">Grande</option>
              </select>
            </div>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="raca">Raça *</label>
              <input
                type="text"
                id="raca"
                name="raca"
                value={formData.raca}
                onChange={handleChange}
                placeholder="Ex: Poodle, Vira-lata..."
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="idade">Idade (anos) *</label>
              <input
                type="number"
                id="idade"
                name="idade"
                min="0"
                max="30"
                value={formData.idade}
                onChange={handleChange}
                placeholder="Ex: 2"
                required
              />
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="observacoes">Observações / Cuidados Especiais</label>
            <textarea
              id="observacoes"
              name="observacoes"
              value={formData.observacoes}
              onChange={handleChange}
              placeholder="Ex: Sociável com outros cães, necessita de guia curta..."
              rows="3"
            />
          </div>

          <div className="button-group">
            <button type="button" className="btn-secondary" onClick={() => navigate('/home')}>
              Cancelar
            </button>
            <button type="submit" className="btn-primary">
              Cadastrar Pet
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}