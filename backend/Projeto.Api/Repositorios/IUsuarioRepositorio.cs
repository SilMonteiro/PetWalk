public interface IUsuarioRepositorio
{
    Usuario Adicionar(Usuario usuario);

    Usuario? ObterPorId(int id);

    Usuario? ObterPorEmail(string email);

    IReadOnlyList<Usuario> ListarPasseadoresAtivos();

    Pet? ObterPet(int petId);

    Pet RegistrarPet(Usuario tutor, Pet pet);
}
