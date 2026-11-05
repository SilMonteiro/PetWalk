// Contrato do IUsuarioRepositorio: Ids, busca por e-mail, passeadores ativos e pets do agregado.
public class UsuarioRepositorioTests
{
    [Fact]
    public void Adicionar_AtribuiIdsSequenciaisComecandoEmUm()
    {
        var repositorio = new UsuarioRepositorioEmMemoria();

        var primeiro = repositorio.Adicionar(Fabrica.CriarUsuario(TipoPerfil.TUTOR, "primeiro@petwalk.com"));
        var segundo = repositorio.Adicionar(Fabrica.CriarUsuario(TipoPerfil.PASSEADOR, "segundo@petwalk.com"));

        Assert.Equal(1, primeiro.Id);
        Assert.Equal(2, segundo.Id);
        Assert.Same(segundo, repositorio.ObterPorId(2));
    }

    [Theory]
    [InlineData("ANA@PETWALK.COM")]
    [InlineData("Ana@PetWalk.com")]
    public void ObterPorEmail_IgnoraMaiusculasEMinusculas(string email)
    {
        var repositorio = new UsuarioRepositorioEmMemoria();
        var usuario = repositorio.Adicionar(Fabrica.CriarUsuario(TipoPerfil.TUTOR, "ana@petwalk.com"));

        Assert.Same(usuario, repositorio.ObterPorEmail(email));
    }

    [Fact]
    public void ObterPorEmail_Inexistente_DevolveNulo()
    {
        var repositorio = new UsuarioRepositorioEmMemoria();

        Assert.Null(repositorio.ObterPorEmail("ninguem@petwalk.com"));
    }

    [Fact]
    public void ListarPasseadoresAtivos_DevolvePasseadorEAmbos_ExcluiTutorEBloqueado()
    {
        var repositorio = new UsuarioRepositorioEmMemoria();
        var passeador = repositorio.Adicionar(Fabrica.CriarUsuario(TipoPerfil.PASSEADOR, "passeador@petwalk.com"));
        var ambos = repositorio.Adicionar(Fabrica.CriarUsuario(TipoPerfil.AMBOS, "ambos@petwalk.com"));
        var tutor = repositorio.Adicionar(Fabrica.CriarUsuario(TipoPerfil.TUTOR, "tutor@petwalk.com"));
        var bloqueado = repositorio.Adicionar(Fabrica.CriarUsuario(TipoPerfil.PASSEADOR, "bloqueado@petwalk.com"));
        bloqueado.Bloquear();

        var ativos = repositorio.ListarPasseadoresAtivos();

        Assert.Equal(2, ativos.Count);
        Assert.Contains(passeador, ativos);
        Assert.Contains(ambos, ativos);
        Assert.DoesNotContain(tutor, ativos);
        Assert.DoesNotContain(bloqueado, ativos);
    }

    [Fact]
    public void RegistrarPet_VinculaAoTutor_AtribuiIdEPermiteObterPet()
    {
        var repositorio = new UsuarioRepositorioEmMemoria();
        var tutor = repositorio.Adicionar(Fabrica.CriarUsuario(TipoPerfil.TUTOR, "tutor@petwalk.com"));
        var primeiro = Fabrica.CriarPet(tutor);
        var segundo = Fabrica.CriarPet(tutor);

        repositorio.RegistrarPet(tutor, primeiro);
        repositorio.RegistrarPet(tutor, segundo);

        Assert.Contains(primeiro, tutor.Pets);
        Assert.Contains(segundo, tutor.Pets);
        Assert.Equal(1, primeiro.Id);
        Assert.Equal(2, segundo.Id);
        Assert.Same(primeiro, repositorio.ObterPet(1));
        Assert.Same(segundo, repositorio.ObterPet(2));
    }

    [Fact]
    public void ObterPet_Inexistente_DevolveNulo()
    {
        var repositorio = new UsuarioRepositorioEmMemoria();

        Assert.Null(repositorio.ObterPet(99));
    }
}
