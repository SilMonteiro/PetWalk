public class Pet
{
    private readonly List<string> _vacinas = new();

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Especie { get; private set; } = string.Empty;
    public string Raca { get; private set; } = string.Empty;
    public Porte Porte { get; private set; }
    public int Idade { get; private set; }
    public string Observacoes { get; private set; } = string.Empty;
    public Usuario Tutor { get; }
    public IReadOnlyList<string> Vacinas => _vacinas;

    public Pet(Usuario tutor, string nome, string especie, string raca, Porte porte, int idade, string observacoes)
    {
        Tutor = tutor ?? throw new ArgumentNullException(nameof(tutor));

        Atualizar(nome, especie, raca, porte, idade, observacoes);
    }

    public void Atualizar(string nome, string especie, string raca, Porte porte, int idade, string observacoes)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ArgumentException("Nome do pet é obrigatório.");
        }

        if (idade < 0 || idade > 30)
        {
            throw new ArgumentException("Idade inválida.");
        }

        Nome = nome;
        Especie = especie ?? string.Empty;
        Raca = raca ?? string.Empty;
        Porte = porte;
        Idade = idade;
        Observacoes = observacoes ?? string.Empty;
    }

    public void RegistrarVacina(string vacina)
    {
        if (string.IsNullOrWhiteSpace(vacina))
        {
            throw new ArgumentException("Vacina é obrigatória.");
        }

        _vacinas.Add(vacina);
    }

    internal void DefinirId(int id) => Id = id;
}
