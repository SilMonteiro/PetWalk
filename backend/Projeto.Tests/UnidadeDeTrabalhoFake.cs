// Fake de IUnidadeDeTrabalho para os testes que chamam os endpoints diretamente e usam
// repositórios em memória: não grava nada, apenas conta as chamadas a Salvar().
// O contador prova que caminhos de erro não disparam gravação; o callback opcional permite
// inspecionar o estado no momento exato da gravação (ordem das operações).
public sealed class UnidadeDeTrabalhoFake : IUnidadeDeTrabalho
{
    private readonly Action? _aoSalvar;

    public UnidadeDeTrabalhoFake(Action? aoSalvar = null) => _aoSalvar = aoSalvar;

    public int Chamadas { get; private set; }

    public void Salvar()
    {
        Chamadas++;
        _aoSalvar?.Invoke();
    }
}
