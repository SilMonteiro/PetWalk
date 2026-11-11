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

        app.MapPost("/usuarios/{id:int}/pets", (int id, PetRequest req, IUsuarioRepositorio repositorio) =>
        {
            var usuario = repositorio.ObterPorId(id);

            if (usuario is null)
            {
                return ErroHttp.NaoEncontrado("Usuário não encontrado");
            }

            try
            {
                // RegistrarPet vincula o pet ao perfil (REG-002) e atribui o Id.
                var pet = repositorio.RegistrarPet(
                    usuario,
                    new Pet(usuario, req.Nome, req.Especie, req.Raca, req.Porte, req.Idade, req.Observacoes));

                return Results.Created($"/usuarios/{usuario.Id}/pets/{pet.Id}", ParaResponse(pet));
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return ErroHttp.Erro(ex);
            }
        });

        app.MapPut("/usuarios/{id:int}/pets/{petId:int}", (int id, int petId, PetRequest req, IUsuarioRepositorio repositorio) =>
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

                return Results.Ok(ParaResponse(pet));
            }
            catch (ArgumentException ex)
            {
                return ErroHttp.Erro(ex);
            }
        });

        app.MapDelete("/usuarios/{id:int}/pets/{petId:int}", (int id, int petId,
            IUsuarioRepositorio repositorio, IPasseioRepositorio passeios) =>
            Excluir(id, petId, repositorio, passeios));
    }

    // Pet com passeio associado não pode ser excluído: o histórico passaria a apontar para um
    // pet inexistente. A checagem vem antes de qualquer remoção, então o 409 não altera nada.
    internal static IResult Excluir(int usuarioId, int petId, IUsuarioRepositorio usuarios,
        IPasseioRepositorio passeios)
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

        return Results.NoContent();
    }

    // REG-005: a busca é sempre dentro da lista do tutor, logo pet de outro tutor não é encontrado.
    private static Pet? LocalizarPet(Usuario usuario, int petId) =>
        usuario.Pets.FirstOrDefault(p => p.Id == petId);

    private static PetResponse ParaResponse(Pet pet) =>
        new(pet.Id, pet.Nome, pet.Especie, pet.Raca, pet.Porte, pet.Idade, pet.Observacoes,
            pet.Vacinas.ToList(), pet.Tutor.Id);
}
