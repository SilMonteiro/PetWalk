// Fábrica de cenários compartilhada pelos testes: relógio fixo e entidades básicas.
public static class Fabrica
{
    // Relógio fixo: as operações que dependem da data recebem "agora" explicitamente.
    public static readonly DateTime Agora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static Usuario CriarUsuario(TipoPerfil tipoPerfil, string? email = null) =>
        new("Usuário de teste",
            email ?? $"{tipoPerfil.ToString().ToLowerInvariant()}@petwalk.com",
            "11999999999",
            "Rua A, 10",
            tipoPerfil,
            "hash-irrelevante");

    public static Pet CriarPet(Usuario tutor) =>
        new(tutor, "Rex", "Cachorro", "Vira-lata", Porte.MEDIO, 3, "Muito dócil");

    public static (Usuario tutor, Usuario passeador, Pet pet, Passeio passeio) CriarPasseioSolicitado()
    {
        var tutor = CriarUsuario(TipoPerfil.TUTOR);
        var passeador = CriarUsuario(TipoPerfil.PASSEADOR);
        var pet = CriarPet(tutor);
        tutor.AdicionarPet(pet);

        var passeio = tutor.SolicitarPasseio(
            pet, passeador, Agora.AddDays(1), TimeSpan.FromHours(1), "Rua A, 10", Agora);

        return (tutor, passeador, pet, passeio);
    }
}
