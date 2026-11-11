public interface IUsuarioRepositorio
{
    Usuario Adicionar(Usuario usuario);

    Usuario? ObterPorId(int id);

    Usuario? ObterPorEmail(string email);

    IReadOnlyList<Usuario> ListarPasseadoresAtivos();

    Pet? ObterPet(int petId);

    Pet RegistrarPet(Usuario tutor, Pet pet);

    // Remove o pet do agregado e, na persistência, apaga o registro correspondente.
    void RemoverPet(Usuario tutor, Pet pet);
}
