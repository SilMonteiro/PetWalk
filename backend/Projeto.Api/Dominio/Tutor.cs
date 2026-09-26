public class Tutor
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Endereco { get; set; } = ""; 
    public TipoPerfil TipoPerfil { get; set; } = TipoPerfil.TUTOR;
    public StatusUsuario Status { get; set; } = StatusUsuario.ATIVO;

    public List<Pet> Pets { get; set; } = new List<Pet>();

    public Passeio SolicitarPasseio(Pet pet, Passeador passeador, DateTime DataHora, TimeSpan Duracao, string Local)
    {
        if (!Pets.Contains(pet))
        {
            throw new InvalidOperationException("O tutor não possui esse pet.");
        }

        if (DataHora <= DateTime.Now)
        {
            throw new InvalidOperationException("A data e hora do passeio devem ser futuras.");
        }

        Passeio passeio = new Passeio(this, passeador, pet, DataHora, Duracao, Local);
        return passeio;
    }

}