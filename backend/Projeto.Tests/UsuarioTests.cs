using System.Reflection;

// Cobre a REG-005 do lado da remoção: só o tutor dono tira o pet do próprio perfil.
public class UsuarioTests
{
    [Fact]
    public void AdicionarPet_ComPerfilDePasseador_LancaErro()
    {
        var passeador = Fabrica.CriarUsuario(TipoPerfil.PASSEADOR);
        var pet = Fabrica.CriarPet(passeador);

        var erro = Assert.Throws<InvalidOperationException>(() => passeador.AdicionarPet(pet));

        Assert.Equal("Usuário não possui perfil de tutor", erro.Message);
        Assert.Empty(passeador.Pets);
    }

    [Fact]
    public void AdicionarPet_ComPerfilAmbos_AdicionaOPet()
    {
        var ambos = Fabrica.CriarUsuario(TipoPerfil.AMBOS);
        var pet = Fabrica.CriarPet(ambos);

        ambos.AdicionarPet(pet);

        Assert.Same(pet, Assert.Single(ambos.Pets));
    }

    [Fact]
    public void RemoverPet_TiraOPetDaListaDoTutor()
    {
        var tutor = Fabrica.CriarUsuario(TipoPerfil.TUTOR);
        var pet = Fabrica.CriarPet(tutor);
        tutor.AdicionarPet(pet);

        tutor.RemoverPet(pet);

        Assert.DoesNotContain(pet, tutor.Pets);
        Assert.Empty(tutor.Pets);
    }

    [Fact]
    public void RemoverPet_DeOutroTutor_LancaErro()
    {
        var tutor = Fabrica.CriarUsuario(TipoPerfil.TUTOR);
        var outroTutor = Fabrica.CriarUsuario(TipoPerfil.TUTOR, "outro@petwalk.com");
        var petDoOutro = Fabrica.CriarPet(outroTutor);

        var erro = Assert.Throws<InvalidOperationException>(() => tutor.RemoverPet(petDoOutro));

        Assert.Equal("O pet informado não pertence ao tutor solicitante", erro.Message);
    }

    [Fact]
    public void Cobranca_ValoresIniciais_SaoZeroEPorHora()
    {
        var passeador = Fabrica.CriarUsuario(TipoPerfil.PASSEADOR);

        Assert.Equal(0m, passeador.ValorHora);
        Assert.Equal(TipoCobranca.POR_HORA, passeador.TipoCobranca);
    }

    [Theory]
    [InlineData(TipoPerfil.PASSEADOR)]
    [InlineData(TipoPerfil.AMBOS)]
    public void DefinirCobranca_ComPerfilDePasseador_AtualizaOsDoisValores(TipoPerfil tipoPerfil)
    {
        var usuario = Fabrica.CriarUsuario(tipoPerfil);

        usuario.DefinirCobranca(45.5m, TipoCobranca.POR_HORA_COM_ADICIONAL_DE_PORTE);

        Assert.Equal(45.5m, usuario.ValorHora);
        Assert.Equal(TipoCobranca.POR_HORA_COM_ADICIONAL_DE_PORTE, usuario.TipoCobranca);
    }

    [Fact]
    public void DefinirCobranca_SemPerfilDePasseador_LancaErro()
    {
        var tutor = Fabrica.CriarUsuario(TipoPerfil.TUTOR);

        var erro = Assert.Throws<InvalidOperationException>(
            () => tutor.DefinirCobranca(30m, TipoCobranca.POR_HORA));

        Assert.Equal("Usuário não possui perfil de passeador", erro.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void DefinirCobranca_ComValorNaoPositivo_LancaErro(int valorHora)
    {
        var passeador = Fabrica.CriarUsuario(TipoPerfil.PASSEADOR);

        var erro = Assert.Throws<ArgumentException>(
            () => passeador.DefinirCobranca(valorHora, TipoCobranca.POR_HORA));

        Assert.Equal("Valor por hora deve ser maior que zero.", erro.Message);
    }

    [Fact]
    public void DefinirCobranca_ComTipoForaDoEnum_LancaErro()
    {
        var passeador = Fabrica.CriarUsuario(TipoPerfil.PASSEADOR);

        var erro = Assert.Throws<ArgumentException>(
            () => passeador.DefinirCobranca(30m, (TipoCobranca)99));

        Assert.Equal("Tipo de cobrança inválido.", erro.Message);
    }

    [Fact]
    public void Usuario_CobrancaNaoMudaPorSetterPublico()
    {
        foreach (var nome in new[] { nameof(Usuario.ValorHora), nameof(Usuario.TipoCobranca) })
        {
            var propriedade = typeof(Usuario).GetProperty(nome, BindingFlags.Public | BindingFlags.Instance);

            Assert.NotNull(propriedade);
            Assert.Null(propriedade!.GetSetMethod());
        }
    }
}
