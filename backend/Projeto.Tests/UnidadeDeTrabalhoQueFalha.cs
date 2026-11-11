// Simula uma falha de infraestrutura no momento da gravação (EF/SQLite). Não é regra de
// negócio: os endpoints não podem convertê-la em resposta 4xx.
public sealed class UnidadeDeTrabalhoQueFalha : IUnidadeDeTrabalho
{
    public int Chamadas { get; private set; }

    public void Salvar()
    {
        Chamadas++;
        throw new InvalidOperationException("falha de persistência simulada");
    }
}
