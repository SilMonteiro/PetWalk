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

}