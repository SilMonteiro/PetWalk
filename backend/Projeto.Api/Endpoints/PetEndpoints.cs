// FLX-007: CRUD dos pets do tutor. O pet é sempre procurado dentro de usuario.Pets,
// então pet de outro tutor cai no mesmo 404 de pet inexistente (REG-005).
public static class PetEndpoints
{
    public static void MapPetEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/usuarios/{id:int}/pets", (int id, IUsuarioRepositorio repositorio) =>
        {
            var usuario = repositorio.ObterPorId(id);

            if (usuario is null)
            {
                return ErroHttp.NaoEncontrado("Usuário não encontrado");
            }

            return Results.Ok(usuario.Pets.Select(ParaResponse).ToList());
        });

        app.MapGet("/usuarios/{id:int}/pets/{petId:int}", (int id, int petId, IUsuarioRepositorio repositorio) =>
        {
            var usuario = repositorio.ObterPorId(id);

            if (usuario is null)
            {
                return ErroHttp.NaoEncontrado("Usuário não encontrado");
            }

            var pet = LocalizarPet(usuario, petId);

            return pet is null
                ? ErroHttp.NaoEncontrado("Pet não encontrado")
                : Results.Ok(ParaResponse(pet));
        });

        app.MapPost("/usuarios/{id:int}/pets", (int id, PetRequest req, IUsuarioRepositorio repositorio,
            IUnidadeDeTrabalho unidade) =>
        {
            var usuario = repositorio.ObterPorId(id);

            if (usuario is null)
            {
                return ErroHttp.NaoEncontrado("Usuário não encontrado");
            }

            Pet pet;

            try
            {
                // RegistrarPet executa REG-002/REG-005 (AdicionarPet), então permanece no catch.
                pet = repositorio.RegistrarPet(
                    usuario,
                    new Pet(usuario, req.Nome, req.Especie, req.Raca, req.Porte, req.Idade, req.Observacoes));
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return ErroHttp.Erro(ex);
            }

            // O Location usa pet.Id: a gravação precisa acontecer antes da resposta.
            // Falha do EF aqui vira 500, nunca 400/422.
            unidade.Salvar();

            return Results.Created($"/usuarios/{usuario.Id}/pets/{pet.Id}", ParaResponse(pet));
        });

        app.MapPut("/usuarios/{id:int}/pets/{petId:int}", (int id, int petId, PetRequest req,
            IUsuarioRepositorio repositorio, IUnidadeDeTrabalho unidade) =>
        {
            var usuario = repositorio.ObterPorId(id);

            if (usuario is null)
            {
                return ErroHttp.NaoEncontrado("Usuário não encontrado");
            }

            var pet = LocalizarPet(usuario, petId);

            if (pet is null)
            {
                return ErroHttp.NaoEncontrado("Pet não encontrado");
            }

            try
            {
                pet.Atualizar(req.Nome, req.Especie, req.Raca, req.Porte, req.Idade, req.Observacoes);
            }
            catch (ArgumentException ex)
            {
                return ErroHttp.Erro(ex);
            }

            // Fora do catch: falha do EF aqui vira 500, nunca 400.
            unidade.Salvar();

            return Results.Ok(ParaResponse(pet));
        });

        app.MapDelete("/usuarios/{id:int}/pets/{petId:int}", (int id, int petId,
            IUsuarioRepositorio repositorio, IPasseioRepositorio passeios, IUnidadeDeTrabalho unidade) =>
            Excluir(id, petId, repositorio, passeios, unidade));
    }

    // Pet com passeio associado não pode ser excluído: o histórico passaria a apontar para um
    // pet inexistente. A checagem vem antes de qualquer remoção, então o 409 não altera nada.
    internal static IResult Excluir(int usuarioId, int petId, IUsuarioRepositorio usuarios,
        IPasseioRepositorio passeios, IUnidadeDeTrabalho unidade)
    {
        var usuario = usuarios.ObterPorId(usuarioId);

        if (usuario is null)
        {
            return ErroHttp.NaoEncontrado("Usuário não encontrado");
        }

        var pet = LocalizarPet(usuario, petId);

        if (pet is null)
        {
            return ErroHttp.NaoEncontrado("Pet não encontrado");
        }

        if (passeios.ExisteParaPet(petId))
        {
            return Results.Conflict(new { erro = "Pet possui passeios e não pode ser excluído" });
        }

        usuarios.RemoverPet(usuario, pet);

        unidade.Salvar();

        return Results.NoContent();
    }

    // REG-005: a busca é sempre dentro da lista do tutor, logo pet de outro tutor não é encontrado.
    private static Pet? LocalizarPet(Usuario usuario, int petId) =>
        usuario.Pets.FirstOrDefault(p => p.Id == petId);

    private static PetResponse ParaResponse(Pet pet) =>
        new(pet.Id, pet.Nome, pet.Especie, pet.Raca, pet.Porte, pet.Idade, pet.Observacoes,
            pet.Vacinas.ToList(), pet.Tutor.Id);
}
