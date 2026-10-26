// Garante que o vocabulário do código segue o que está descrito em docs/proposta.md.
public class VocabularioDaPropostaTests
{
    [Fact]
    public void StatusPasseio_TemExatamenteOsEstadosDoCicloDeVida()
    {
        var esperado = new[] { "SOLICITADO", "ACEITO", "EM_ANDAMENTO", "FINALIZADO", "RECUSADO", "CANCELADO" };

        Assert.Equal(esperado, Enum.GetNames<StatusPasseio>());
    }

    [Fact]
    public void TipoPerfil_TemTutorPasseadorEAmbos()
    {
        Assert.Equal(new[] { "TUTOR", "PASSEADOR", "AMBOS" }, Enum.GetNames<TipoPerfil>());
    }

    [Fact]
    public void TipoNotificacao_TemUmTipoParaCadaEventoDoPasseio()
    {
        var esperado = new[]
        {
            "PASSEIO_SOLICITADO", "PASSEIO_ACEITO", "PASSEIO_RECUSADO",
            "PASSEIO_INICIADO", "PASSEIO_FINALIZADO", "PASSEIO_CANCELADO"
        };

        Assert.Equal(esperado, Enum.GetNames<TipoNotificacao>());
    }
}
