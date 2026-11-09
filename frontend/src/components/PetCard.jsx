import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
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
      <li className="pet-card pet-card-confirmacao">
        <p className="pet-confirmacao-texto">Excluir {pet.nome}?</p>

        {erro && <p className="pet-erro">{erro}</p>}

        <div className="pet-acoes">
          <button
            type="button"
            className="pet-botao pet-botao-secundario"
            onClick={cancelar}
            disabled={excluindo}
          >
            Cancelar
          </button>
          <button
            type="button"
            className="pet-botao pet-botao-perigo"
            onClick={handleConfirmar}
            disabled={excluindo}
          >
            Confirmar
          </button>
        </div>
      </li>
    );
  }

  return (
    <li className="pet-card">
      <h2 className="pet-nome">{pet.nome}</h2>
      <p className="pet-detalhe">Espécie: {pet.especie}</p>
      <p className="pet-detalhe">Raça: {pet.raca}</p>
      <p className="pet-detalhe">Porte: {ROTULO_PORTE[pet.porte]}</p>
      <p className="pet-detalhe">
        Idade: {pet.idade} {pet.idade === 1 ? 'ano' : 'anos'}
      </p>
      {pet.observacoes && <p className="pet-observacoes">{pet.observacoes}</p>}

      <div className="pet-acoes">
        <button
          type="button"
          className="pet-botao"
          onClick={() => navigate(`/pets/${pet.id}/editar`)}
        >
          Editar
        </button>
        <button
          type="button"
          className="pet-botao pet-botao-perigo"
          onClick={() => setConfirmando(true)}
        >
          Excluir
        </button>
      </div>
    </li>
  );
}
