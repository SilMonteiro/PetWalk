// Fake de IUnidadeDeTrabalho para os testes que chamam os endpoints diretamente e usam
// repositórios em memória: não grava nada, apenas conta as chamadas a Salvar().
// O contador permite provar que caminhos de erro não disparam gravação.
public sealed class UnidadeDeTrabalhoFake : IUnidadeDeTrabalho
{
    public int Chamadas { get; private set; }

    public void Salvar() => Chamadas++;
}
