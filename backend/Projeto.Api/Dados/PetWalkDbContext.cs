using Microsoft.EntityFrameworkCore;

// Contexto único da aplicação. Os mapeamentos ficam em configurações separadas,
// descobertas por assembly quando forem criadas nas próximas etapas.
public class PetWalkDbContext : DbContext
{
    public PetWalkDbContext(DbContextOptions<PetWalkDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Pet> Pets => Set<Pet>();

    public DbSet<Passeio> Passeios => Set<Passeio>();

    public DbSet<Avaliacao> Avaliacoes => Set<Avaliacao>();

    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PetWalkDbContext).Assembly);
    }
}
