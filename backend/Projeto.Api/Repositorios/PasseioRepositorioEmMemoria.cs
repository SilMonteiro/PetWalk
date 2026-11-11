// Repositório em memória dos passeios; o Id é atribuído aqui, nunca pela entidade.
public class PasseioRepositorioEmMemoria : IPasseioRepositorio
{
    private readonly List<Passeio> _passeios = new();

    public Passeio Adicionar(Passeio passeio)
    {
        passeio.DefinirId(_passeios.Count + 1);
        _passeios.Add(passeio);

        return passeio;
    }

    public Passeio? ObterPorId(int id) => _passeios.FirstOrDefault(p => p.Id == id);

    // Em memória o grafo já guarda o histórico completo: a mesma instância serve.
    public Passeio? ObterParaAvaliacao(int id) => ObterPorId(id);

    public IReadOnlyList<Passeio> ListarPorUsuario(int usuarioId) =>
        _passeios.Where(p => p.Tutor.Id == usuarioId || p.Passeador.Id == usuarioId).ToList();

    public bool ExisteParaPet(int petId) => _passeios.Any(p => p.Pet.Id == petId);
}
