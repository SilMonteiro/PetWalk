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

    // SolicitarPasseio saiu daqui: o Passeio agora exige referências de Usuario.
    // O comportamento volta em Usuario.SolicitarPasseio() na Etapa 2, quando Tutor e Passeador forem removidos.
}