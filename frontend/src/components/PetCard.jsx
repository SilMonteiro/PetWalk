import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import AvatarIniciais from './AvatarIniciais';
import Icon from './Icon';
import { excluirPet } from '../services/petService';
import { ROTULO_PORTE } from '../constants/enums';

export default function PetCard({ pet, usuarioId, onExcluido }) {
  const navigate = useNavigate();
  const [confirmando, setConfirmando] = useState(false);
  const [excluindo, setExcluindo] = useState(false);
  const [erro, setErro] = useState('');

  const handleConfirmar = async () => {
    setErro('');
    setExcluindo(true);

    try {
      await excluirPet(usuarioId, pet.id);
      onExcluido(pet.id);
    } catch (falha) {
      setErro(falha.message);
      setExcluindo(false);
    }
  };

  const cancelar = () => {
    setConfirmando(false);
    setErro('');
  };

  if (confirmando) {
    return (
      <li className="pet-card pet-card--confirmacao">
        <span className="pet-confirmacao-icone">
          <Icon nome="trash" />
        </span>
        <p className="pet-confirmacao-texto">Excluir {pet.nome}?</p>
        <p className="pet-confirmacao-ajuda">Esta ação não pode ser desfeita.</p>

        {erro && (
          <p className="alerta alerta--erro" role="alert">
            <Icon nome="alert" />
            {erro}
          </p>
        )}

        <div className="acoes-linha">
          <button
            type="button"
            className="btn btn--ghost btn--sm"
            onClick={cancelar}
            disabled={excluindo}
          >
            Cancelar
          </button>
          <button
            type="button"
            className="btn btn--perigo btn--sm"
            onClick={handleConfirmar}
            disabled={excluindo}
          >
            {excluindo ? <span className="spinner" /> : <Icon nome="trash" />}
            Excluir
          </button>
        </div>
      </li>
    );
  }

  return (
    <li className="pet-card">
      <div className="pet-topo">
        <AvatarIniciais nome={pet.nome} tamanho="lg" />
        <div>
          <h2 className="pet-nome">{pet.nome}</h2>
          <p className="pet-raca">
            {pet.especie} · {pet.raca}
          </p>
        </div>
      </div>

      <div className="pet-tags">
        <span className="pill">Porte {ROTULO_PORTE[pet.porte].toLowerCase()}</span>
        <span className="pill pill--verde">
          {pet.idade} {pet.idade === 1 ? 'ano' : 'anos'}
        </span>
      </div>

      {pet.observacoes && <p className="pet-observacoes">{pet.observacoes}</p>}

      <div className="pet-acoes">
        <button
          type="button"
          className="btn btn--outline btn--sm"
          onClick={() => navigate(`/pets/${pet.id}/editar`)}
        >
          <Icon nome="pencil" />
          Editar
        </button>
        <button
          type="button"
          className="btn btn--perigo-suave btn--sm"
          onClick={() => setConfirmando(true)}
        >
          <Icon nome="trash" />
          Excluir
        </button>
      </div>
    </li>
  );
}
