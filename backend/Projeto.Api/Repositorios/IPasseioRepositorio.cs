public interface IPasseioRepositorio
{
    Passeio Adicionar(Passeio passeio);

    Passeio? ObterPorId(int id);

    // Passeio com o histórico de avaliações dos participantes carregado, para que
    // MediaAvaliacao seja recalculada sobre todas as avaliações recebidas pelo avaliado.
    Passeio? ObterParaAvaliacao(int id);

    IReadOnlyList<Passeio> ListarPorUsuario(int usuarioId);

    // Usado pela exclusão de pet: com passeio associado, a exclusão é bloqueada.
    bool ExisteParaPet(int petId);
}
