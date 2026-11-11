using Microsoft.EntityFrameworkCore;

// Repositório EF dos passeios. Não chama SaveChanges: a persistência é responsabilidade
// da IUnidadeDeTrabalho. Sem AsNoTracking: o domínio altera o passeio carregado
// (Aceitar, Iniciar, Finalizar, Avaliar...) e essas mudanças precisam ser persistidas.
public class PasseioRepositorio : IPasseioRepositorio
{
    private readonly PetWalkDbContext _contexto;

    public PasseioRepositorio(PetWalkDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    // O Id nasce no banco: é gerado pelo SQLite quando a Unidade de Trabalho salvar.
    // Tutor, passeador e pet devem estar rastreados no mesmo contexto, como ficam ao ser
    // carregados pelos repositórios de usuário antes da solicitação.
    public Passeio Adicionar(Passeio passeio)
    {
        _contexto.Passeios.Add(passeio);

        return passeio;
    }

    // PasseioResponse lê apenas Tutor, Passeador e Pet. As avaliações têm um método próprio
    // (ObterParaAvaliacao), para não carregar coleção que nenhum fluxo de leitura usa aqui.
    public Passeio? ObterPorId(int id) =>
        _contexto.Passeios
            .Include(p => p.Tutor)
            .Include(p => p.Passeador)
            .Include(p => p.Pet)
            .FirstOrDefault(p => p.Id == id);

    // REG-001: a média do avaliado considera TODAS as avaliações que ele já recebeu, não só
    // as deste passeio. As coleções dos dois participantes vêm carregadas porque só se sabe
    // quem avalia quem em tempo de execução. Passeio.Avaliacoes também é necessária: é nela
    // que o domínio verifica a avaliação duplicada, comparando o Avaliador por referência.
    public Passeio? ObterParaAvaliacao(int id) =>
        _contexto.Passeios
            .Include(p => p.Tutor)
                .ThenInclude(u => u.AvaliacoesRecebidas)
            .Include(p => p.Passeador)
                .ThenInclude(u => u.AvaliacoesRecebidas)
            .Include(p => p.Pet)
            .Include(p => p.Avaliacoes)
                .ThenInclude(a => a.Avaliador)
            .FirstOrDefault(p => p.Id == id);

    // A listagem alimenta PasseioResponse, que lê as três navegações de cada passeio.
    public IReadOnlyList<Passeio> ListarPorUsuario(int usuarioId) =>
        _contexto.Passeios
            .Include(p => p.Tutor)
            .Include(p => p.Passeador)
            .Include(p => p.Pet)
            .Where(p => p.Tutor.Id == usuarioId || p.Passeador.Id == usuarioId)
            .ToList();

    public bool ExisteParaPet(int petId) =>
        _contexto.Passeios.Any(p => p.Pet.Id == petId);
}
