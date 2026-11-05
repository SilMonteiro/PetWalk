// Cobre a REG-005 do lado da remoção: só o tutor dono tira o pet do próprio perfil.
public class UsuarioTests
{
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
}
