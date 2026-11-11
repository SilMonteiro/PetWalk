using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Falha de persistência simulada na Unidade de Trabalho: os endpoints com catch de domínio não
// podem convertê-la em 400/409/422. Os testes usam a PetWalkApiFactory (SQLite em memória).
public class EndpointsFalhaDePersistenciaTests
{
    private static readonly UsuarioRequest CadastroDeTutor =
        new("Novo Usuário", "novo@petwalk.com", "11999999999", "Rua A, 10", TipoPerfil.TUTOR, "senha123");

    private static readonly PetRequest PetValido =
        new("Rex", "Cachorro", "Vira-lata", Porte.MEDIO, 3, "Dócil");

    [Fact]
    public async Task Cadastro_FalhaAoSalvar_Devolve500()
    {
        using var fabrica = new FabricaComFalhaAoSalvar();
        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/cadastro", CadastroDeTutor);

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
    }

    [Fact]
    public async Task Cadastro_EmailDuplicado_Continua409()
    {
        using var fabrica = new FabricaComFalhaAoSalvar();

        using (var escopo = fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<PetWalkDbContext>();
            contexto.Usuarios.Add(Fabrica.CriarUsuario(TipoPerfil.TUTOR, CadastroDeTutor.Email));
            contexto.SaveChanges();
        }

        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/cadastro", CadastroDeTutor);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarPet_FalhaAoSalvar_Devolve500()
    {
        using var fabrica = new FabricaComFalhaAoSalvar();
        var tutorId = SemearUsuario(fabrica, TipoPerfil.TUTOR);

        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.PostAsJsonAsync($"/usuarios/{tutorId}/pets", PetValido);

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarPet_UsuarioSemPerfilTutor_Continua422()
    {
        using var fabrica = new FabricaComFalhaAoSalvar();
        var passeadorId = SemearUsuario(fabrica, TipoPerfil.PASSEADOR);

        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.PostAsJsonAsync($"/usuarios/{passeadorId}/pets", PetValido);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task AtualizarPet_FalhaAoSalvar_Devolve500()
    {
        using var fabrica = new FabricaComFalhaAoSalvar();
        var (tutorId, petId) = SemearTutorComPet(fabrica);

        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.PutAsJsonAsync(
            $"/usuarios/{tutorId}/pets/{petId}",
            new PetRequest("Rex II", "Cachorro", "Vira-lata", Porte.GRANDE, 4, "Dócil"));

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
    }

    [Fact]
    public async Task AtualizarPet_DadosInvalidos_Continua400()
    {
        using var fabrica = new FabricaComFalhaAoSalvar();
        var (tutorId, petId) = SemearTutorComPet(fabrica);

        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.PutAsJsonAsync(
            $"/usuarios/{tutorId}/pets/{petId}",
            new PetRequest("", "Cachorro", "Vira-lata", Porte.MEDIO, 3, ""));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    private static int SemearUsuario(PetWalkApiFactory fabrica, TipoPerfil perfil)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<PetWalkDbContext>();

        var usuario = Fabrica.CriarUsuario(perfil);
        contexto.Usuarios.Add(usuario);
        contexto.SaveChanges();

        return usuario.Id;
    }

    private static (int TutorId, int PetId) SemearTutorComPet(PetWalkApiFactory fabrica)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<PetWalkDbContext>();

        var tutor = Fabrica.CriarUsuario(TipoPerfil.TUTOR);
        var pet = Fabrica.CriarPet(tutor);
        tutor.AdicionarPet(pet);

        contexto.Usuarios.Add(tutor);
        contexto.Pets.Add(pet);
        contexto.SaveChanges();

        return (tutor.Id, pet.Id);
    }

    // Só a Unidade de Trabalho falha: o restante do host permanece o da PetWalkApiFactory.
    private sealed class FabricaComFalhaAoSalvar : PetWalkApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(servicos =>
            {
                servicos.RemoveAll<IUnidadeDeTrabalho>();
                servicos.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalhoQueFalha>();
            });
        }
    }
}
