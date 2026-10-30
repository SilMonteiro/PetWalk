
public class Passeador
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Endereco { get; set; } = ""; // privado
    public double Avaliacao { get; set; }
    public double ValorHora { get; set; }
    public TipoPerfil TipoPerfil { get; set; } = TipoPerfil.PASSEADOR;
    public StatusUsuario Status { get; set; } = StatusUsuario.ATIVO;

    // AceitarPasseio saiu daqui: o Status do Passeio só muda pela própria classe agora.
    // O comportamento volta em Passeio.Aceitar(Usuario) na Etapa 3, quando Tutor e Passeador forem removidos.
}
