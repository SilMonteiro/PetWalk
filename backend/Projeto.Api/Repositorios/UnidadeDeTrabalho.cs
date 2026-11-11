// Implementação sobre o PetWalkDbContext injetado. O EF Core já envolve um único
// SaveChanges em transação quando necessário, então não há transação manual aqui.
public class UnidadeDeTrabalho : IUnidadeDeTrabalho
{
    private readonly PetWalkDbContext _contexto;

    public UnidadeDeTrabalho(PetWalkDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public void Salvar() => _contexto.SaveChanges();
}
