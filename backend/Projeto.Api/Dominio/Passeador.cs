
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

    public void AceitarPasseio(Passeio passeio)
    {
        if (passeio.PasseadorId != this.Id)
        {
            throw new InvalidOperationException("O passeio não pertence a este passeador.");
        }

        if (passeio.Status != StatusPasseio.SOLICITADO)
        {
            throw new InvalidOperationException("O passeio não está em estado de solicitação.");
        }

        passeio.Status = StatusPasseio.ACEITO;
    }
}
