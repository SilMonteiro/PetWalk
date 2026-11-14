import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import PageShell, { PageHeading } from '../components/PageShell';
import Icon from '../components/Icon';
import { useAuth } from '../context/AuthContext';
import { atualizarPet, criarPet, obterPet } from '../services/petService';
import { PORTE, ROTULO_PORTE } from '../constants/enums';
import '../styles/pet-form.css';

const FORM_VAZIO = {
  nome: '',
  especie: '',
  raca: '',
  porte: PORTE.MEDIO,
  idade: '',
  observacoes: ''
};

export default function PetFormPage() {
  const { usuario } = useAuth();
  const navigate = useNavigate();
  const { id } = useParams();

  const petId = id ? Number(id) : null;
  const editando = petId !== null;

  const [form, setForm] = useState(FORM_VAZIO);
  const [carregando, setCarregando] = useState(editando);
  const [erroCarga, setErroCarga] = useState('');
  const [erro, setErro] = useState('');
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    if (!editando) {
      // Ao trocar de "editar" para "novo", o formulário não pode manter os dados antigos.
      setForm(FORM_VAZIO);
      setCarregando(false);
      setErroCarga('');
      return;
    }

    let ativo = true;
    setCarregando(true);
    setErroCarga('');

    obterPet(usuario.id, petId)
      .then((pet) => {
        if (!ativo) {
          return;
        }

        setForm({
          nome: pet.nome,
          especie: pet.especie,
          raca: pet.raca,
          porte: pet.porte,
          idade: String(pet.idade),
          observacoes: pet.observacoes ?? ''
        });
      })
      .catch((falha) => {
        if (ativo) {
          setErroCarga(falha.message);
        }
      })
      .finally(() => {
        if (ativo) {
          setCarregando(false);
        }
      });

    return () => {
      ativo = false;
    };
  }, [editando, petId, usuario.id]);

  const handleChange = (evento) => {
    const { name, value } = evento.target;
    setForm((atual) => ({ ...atual, [name]: value }));
  };

  const handlePorte = (evento) => {
    const porte = Number(evento.target.value);
    setForm((atual) => ({ ...atual, porte }));
  };

  const handleSubmit = async (evento) => {
    evento.preventDefault();

    if (enviando) {
      return;
    }

    setErro('');
    setEnviando(true);

    const dados = {
      nome: form.nome.trim(),
      especie: form.especie.trim(),
      raca: form.raca.trim(),
      porte: form.porte,
      idade: Number(form.idade),
      observacoes: form.observacoes.trim()
    };

    try {
      if (editando) {
        await atualizarPet(usuario.id, petId, dados);
      } else {
        await criarPet(usuario.id, dados);
      }

      navigate('/pets');
    } catch (falha) {
      setErro(falha.message);
      setEnviando(false);
    }
  };

  return (
    <PageShell largura="estreita">
      <PageHeading
        icone={<Icon nome="pata" />}
        titulo={editando ? 'Editar pet' : 'Novo pet'}
        subtitulo={
          editando ? 'Atualize os dados do seu pet.' : 'Conte um pouco sobre o seu companheiro.'
        }
      />

      {carregando && (
        <div className="carregando" role="status">
          <span className="spinner" />
          Carregando pet...
        </div>
      )}

      {!carregando && erroCarga && (
        <div className="bloco-erro">
          <p className="alerta alerta--erro" role="alert">
            <Icon nome="alert" />
            {erroCarga}
          </p>
          <Link className="btn btn--outline" to="/pets">
            Voltar para Meus pets
          </Link>
        </div>
      )}

      {!carregando && !erroCarga && (
        <form className="form-card" onSubmit={handleSubmit}>
          <div className="campo">
            <label htmlFor="nome">Nome do pet</label>
            <input
              type="text"
              id="nome"
              name="nome"
              className="input"
              value={form.nome}
              onChange={handleChange}
              placeholder="Ex.: Mel, Thor, Pipoca"
              required
            />
          </div>

          <div className="linha-campos">
            <div className="campo">
              <label htmlFor="especie">Espécie</label>
              <input
                type="text"
                id="especie"
                name="especie"
                className="input"
                value={form.especie}
                onChange={handleChange}
                placeholder="Ex.: Cão, Gato"
                required
              />
            </div>

            <div className="campo">
              <label htmlFor="raca">Raça</label>
              <input
                type="text"
                id="raca"
                name="raca"
                className="input"
                value={form.raca}
                onChange={handleChange}
                placeholder="Ex.: Poodle, Vira-lata"
                required
              />
            </div>
          </div>

          <div className="linha-campos">
            <div className="campo">
              <label htmlFor="porte">Porte</label>
              <select
                id="porte"
                name="porte"
                className="select"
                value={form.porte}
                onChange={handlePorte}
              >
                {Object.entries(ROTULO_PORTE).map(([valor, rotulo]) => (
                  <option key={valor} value={Number(valor)}>
                    {rotulo}
                  </option>
                ))}
              </select>
            </div>

            <div className="campo">
              <label htmlFor="idade">Idade (anos)</label>
              <input
                type="number"
                id="idade"
                name="idade"
                className="input"
                min="0"
                max="30"
                step="1"
                value={form.idade}
                onChange={handleChange}
                placeholder="Ex.: 2"
                required
              />
            </div>
          </div>

          <div className="campo">
            <label htmlFor="observacoes">Observações</label>
            <textarea
              id="observacoes"
              name="observacoes"
              className="textarea"
              value={form.observacoes}
              onChange={handleChange}
              placeholder="Ex.: Sociável com outros cães, precisa de guia curta"
              rows="3"
            />
            <span className="campo-ajuda">
              Informações úteis para quem vai passear com o seu pet.
            </span>
          </div>

          {erro && (
            <p className="alerta alerta--erro" role="alert">
              <Icon nome="alert" />
              {erro}
            </p>
          )}

          <div className="acoes-linha">
            <button
              type="button"
              className="btn btn--ghost"
              onClick={() => navigate('/pets')}
              disabled={enviando}
            >
              Cancelar
            </button>
            <button type="submit" className="btn" disabled={enviando}>
              {enviando ? <span className="spinner" /> : <Icon nome="check" />}
              Salvar pet
            </button>
          </div>
        </form>
      )}
    </PageShell>
  );
}
