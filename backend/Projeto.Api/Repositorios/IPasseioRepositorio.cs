public interface IPasseioRepositorio
{
    Passeio Adicionar(Passeio passeio);

    Passeio? ObterPorId(int id);

    IReadOnlyList<Passeio> ListarPorUsuario(int usuarioId);
}
