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

    public static Pet CriarPet(Usuario tutor, Porte porte = Porte.MEDIO) =>
        new(tutor, "Rex", "Cachorro", "Vira-lata", porte, 3, "Muito dócil");

    public static (Usuario tutor, Usuario passeador, Pet pet, Passeio passeio) CriarPasseioSolicitado(
        TimeSpan? duracao = null,
        Porte porte = Porte.MEDIO,
        decimal? valorHora = null)
    {
        var passeador = CriarUsuario(TipoPerfil.PASSEADOR);

        if (valorHora is not null)
        {
            passeador.DefinirCobranca(valorHora.Value, TipoCobranca.POR_HORA);
        }

        var (tutor, pet, passeio) = CriarPasseioSolicitadoPara(passeador, porte, duracao);

        return (tutor, passeador, pet, passeio);
    }

    // Permite acumular passeios no mesmo passeador, base para a média de avaliações.
    public static (Usuario tutor, Pet pet, Passeio passeio) CriarPasseioSolicitadoPara(
        Usuario passeador,
        Porte porte = Porte.MEDIO,
        TimeSpan? duracao = null)
    {
        var tutor = CriarUsuario(TipoPerfil.TUTOR);
        var pet = CriarPet(tutor, porte);
        tutor.AdicionarPet(pet);

        var passeio = tutor.SolicitarPasseio(
            pet, passeador, Agora.AddDays(1), duracao ?? TimeSpan.FromHours(1), "Rua A, 10", Agora);

        return (tutor, pet, passeio);
    }
}
