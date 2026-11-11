public interface IPasseioRepositorio
{
    Passeio Adicionar(Passeio passeio);

    Passeio? ObterPorId(int id);

    IReadOnlyList<Passeio> ListarPorUsuario(int usuarioId);

    // Usado pela exclusão de pet: com passeio associado, a exclusão é bloqueada.
    bool ExisteParaPet(int petId);
}
