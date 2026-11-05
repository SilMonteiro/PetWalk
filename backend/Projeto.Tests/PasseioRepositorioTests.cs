// IPasseioRepositorio: Ids sequenciais e listagem por participante.
public class PasseioRepositorioTests
{
    [Fact]
    public void Adicionar_AtribuiIdsSequenciais()
    {
        var repositorio = new PasseioRepositorioEmMemoria();

        var primeiro = repositorio.Adicionar(Fabrica.CriarPasseioSolicitado().passeio);
        var segundo = repositorio.Adicionar(Fabrica.CriarPasseioSolicitado().passeio);

        Assert.Equal(1, primeiro.Id);
        Assert.Equal(2, segundo.Id);
        Assert.Same(segundo, repositorio.ObterPorId(2));
    }

    [Fact]
    public void ListarPorUsuario_DevolveParaTutorEPasseador_MasNaoParaTerceiro()
    {
        var (tutor, passeador, _, passeio) = Fabrica.CriarPasseioSolicitado();
        tutor.DefinirId(1);
        passeador.DefinirId(2);

        var terceiro = Fabrica.CriarUsuario(TipoPerfil.TUTOR, "terceiro@petwalk.com");
        terceiro.DefinirId(3);

        var repositorio = new PasseioRepositorioEmMemoria();
        repositorio.Adicionar(passeio);

        Assert.Same(passeio, Assert.Single(repositorio.ListarPorUsuario(1)));
        Assert.Same(passeio, Assert.Single(repositorio.ListarPorUsuario(2)));
        Assert.Empty(repositorio.ListarPorUsuario(3));
    }
}
