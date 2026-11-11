using Microsoft.AspNetCore.Http;

// FLX-007: exclusão de pet — 204 sem passeio, 409 com passeio, 404 para tutor ou pet inexistente.
public class PetEndpointsTests
{
    // Fake sem efeito: os repositórios são em memória, então a mutação já é observável.
    private readonly UnidadeDeTrabalhoFake _unidade = new();

    private static (IUsuarioRepositorio usuarios, IPasseioRepositorio passeios, Usuario tutor, Pet pet) CriarCenario()
    {
        var usuarios = new UsuarioRepositorioEmMemoria();
        var passeios = new PasseioRepositorioEmMemoria();

        var tutor = usuarios.Adicionar(Fabrica.CriarUsuario(TipoPerfil.TUTOR));
        var pet = usuarios.RegistrarPet(tutor, Fabrica.CriarPet(tutor));

        return (usuarios, passeios, tutor, pet);
    }

    [Fact]
    public void Excluir_PetSemPasseio_Devolve204ERemoveOPet()
    {
        var (usuarios, passeios, tutor, pet) = CriarCenario();

        var resultado = PetEndpoints.Excluir(tutor.Id, pet.Id, usuarios, passeios, _unidade);

        Assert.Equal(204, StatusDe(resultado));
        Assert.Empty(tutor.Pets);
        Assert.Null(usuarios.ObterPet(pet.Id));

        Assert.Equal(1, _unidade.Chamadas);
    }

    [Fact]
    public void Excluir_PetComPasseio_Devolve409EMantemOPet()
    {
        var (usuarios, passeios, tutor, pet) = CriarCenario();
        var passeador = usuarios.Adicionar(Fabrica.CriarUsuario(TipoPerfil.PASSEADOR, "passeador@petwalk.com"));
        var passeio = tutor.SolicitarPasseio(
            pet, passeador, Fabrica.Agora.AddDays(1), TimeSpan.FromHours(1), "Rua A, 10", Fabrica.Agora);
        passeios.Adicionar(passeio);

        var resultado = PetEndpoints.Excluir(tutor.Id, pet.Id, usuarios, passeios, _unidade);

        Assert.Equal(409, StatusDe(resultado));
        Assert.Equal("Pet possui passeios e não pode ser excluído", MensagemDe(resultado));

        // O 409 não pode alterar nada: o pet continua no agregado e acessível pelo repositório.
        Assert.Contains(pet, tutor.Pets);
        Assert.Same(pet, usuarios.ObterPet(pet.Id));

        Assert.Equal(0, _unidade.Chamadas);
    }

    [Fact]
    public void Excluir_TutorInexistente_Devolve404()
    {
        var (usuarios, passeios, _, pet) = CriarCenario();

        var resultado = PetEndpoints.Excluir(999, pet.Id, usuarios, passeios, _unidade);

        Assert.Equal(404, StatusDe(resultado));
        Assert.Equal("Usuário não encontrado", MensagemDe(resultado));

        Assert.Equal(0, _unidade.Chamadas);
    }

    [Fact]
    public void Excluir_PetInexistente_Devolve404()
    {
        var (usuarios, passeios, tutor, _) = CriarCenario();

        var resultado = PetEndpoints.Excluir(tutor.Id, 999, usuarios, passeios, _unidade);

        Assert.Equal(404, StatusDe(resultado));
        Assert.Equal("Pet não encontrado", MensagemDe(resultado));
    }

    [Fact]
    public void Excluir_PetDeOutroTutor_Devolve404SemRemover()
    {
        var (usuarios, passeios, tutor, _) = CriarCenario();
        var outroTutor = usuarios.Adicionar(Fabrica.CriarUsuario(TipoPerfil.TUTOR, "outro@petwalk.com"));
        var petDoOutro = usuarios.RegistrarPet(outroTutor, Fabrica.CriarPet(outroTutor));

        var resultado = PetEndpoints.Excluir(tutor.Id, petDoOutro.Id, usuarios, passeios, _unidade);

        Assert.Equal(404, StatusDe(resultado));
        Assert.Equal("Pet não encontrado", MensagemDe(resultado));
        Assert.Contains(petDoOutro, outroTutor.Pets);
    }

    private static int StatusDe(IResult resultado) =>
        Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode!.Value;

    private static string? MensagemDe(IResult resultado)
    {
        var corpo = Assert.IsAssignableFrom<IValueHttpResult>(resultado).Value;

        return corpo?.GetType().GetProperty("erro")?.GetValue(corpo) as string;
    }
}
