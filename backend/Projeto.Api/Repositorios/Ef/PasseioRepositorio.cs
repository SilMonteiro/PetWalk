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

    // PasseioResponse lê Tutor, Passeador e Pet; a leitura de um passeio também expõe
    // as avaliações e o avaliador de cada uma. Avaliado é resolvido pelo fixup do EF,
    // porque o tutor e o passeador já vêm carregados por esta consulta.
    public Passeio? ObterPorId(int id) =>
        _contexto.Passeios
            .Include(p => p.Tutor)
            .Include(p => p.Passeador)
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
