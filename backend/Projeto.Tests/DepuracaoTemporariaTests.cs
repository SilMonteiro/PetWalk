// TEMPORÁRIO — experimentos da E03 (registro de depuração). Remover na Fatia 5.
using System.Globalization;
using Xunit.Abstractions;

public class DepuracaoTemporariaTests
{
    private readonly ITestOutputHelper _saida;

    public DepuracaoTemporariaTests(ITestOutputHelper saida) => _saida = saida;

    // Um passeio FINALIZADO do mesmo passeador, com um tutor próprio para avaliar.
    private static (Usuario tutor, Passeio passeio) PasseioFinalizado(Usuario passeador)
    {
        var (tutor, _, passeio) = Fabrica.CriarPasseioSolicitadoPara(passeador);

        passeio.Aceitar(passeador);
        passeio.Iniciar(passeador);
        passeio.Finalizar(passeador);

        return (tutor, passeio);
    }

    private static string Valor(double numero) => numero.ToString("G17", CultureInfo.InvariantCulture);

    // H1: a média é a última nota recebida (5, 1, 3 → 5, 1, 3) ou a média acumulada (5, 3, 3)?
    [Fact]
    public void H1_MediaUsaSoAUltimaNota()
    {
        var passeador = Fabrica.CriarUsuario(TipoPerfil.PASSEADOR);

        foreach (var nota in new[] { 5, 1, 3 })
        {
            var (tutor, passeio) = PasseioFinalizado(passeador);
            passeio.Avaliar(tutor, nota, "experimento", Fabrica.Agora);

            _saida.WriteLine(
                $"H1 nota={nota} | avaliacoes={passeador.AvaliacoesRecebidas.Count} | media={Valor(passeador.MediaAvaliacao)}");
        }

        Assert.Equal(3, passeador.AvaliacoesRecebidas.Count);
    }

    // H2: a coleção de avaliações recebidas não acumula entre passeios do mesmo passeador?
    [Fact]
    public void H2_ColecaoNaoAcumula()
    {
        var passeador = Fabrica.CriarUsuario(TipoPerfil.PASSEADOR);

        for (var passo = 1; passo <= 3; passo++)
        {
            var (tutor, passeio) = PasseioFinalizado(passeador);
            passeio.Avaliar(tutor, 5, "experimento", Fabrica.Agora);

            var notas = string.Join(", ", passeador.AvaliacoesRecebidas.Select(a => a.Nota));

            _saida.WriteLine(
                $"H2 passo={passo} | AvaliacoesRecebidas.Count={passeador.AvaliacoesRecebidas.Count} | notas=[{notas}] | media={Valor(passeador.MediaAvaliacao)}");
        }

        Assert.Equal(3, passeador.AvaliacoesRecebidas.Count);
    }

    // H3: o problema é arredondamento ou tipo de dado? (14/3 = 4,666... não é exato em double)
    [Fact]
    public void H3_ArredondamentoOuTipo()
    {
        var passeador = Fabrica.CriarUsuario(TipoPerfil.PASSEADOR);

        foreach (var nota in new[] { 5, 5, 4 })
        {
            var (tutor, passeio) = PasseioFinalizado(passeador);
            passeio.Avaliar(tutor, nota, "experimento", Fabrica.Agora);
        }

        var exata = 14d / 3d;

        _saida.WriteLine($"H3 notas=[5, 5, 4] | media exata (14/3)={Valor(exata)} | observado={Valor(passeador.MediaAvaliacao)}");
        _saida.WriteLine($"H3 diferenca para a media exata={Valor(passeador.MediaAvaliacao - exata)}");
    }

    // Técnica 1 (log temporário): reproduz o cenário de E02 (notas 5 e 1) e imprime, a cada
    // Avaliar, a nota registrada, a contagem recebida e a MediaAvaliacao resultante.
    [Fact]
    public void T1_LogTemporario_RegistrarAvaliacaoRecebida()
    {
        var passeador = Fabrica.CriarUsuario(TipoPerfil.PASSEADOR);

        var (tutor1, passeio1) = PasseioFinalizado(passeador);
        var primeira = passeio1.Avaliar(tutor1, 5, "Ótimo passeio", Fabrica.Agora);

        _saida.WriteLine(
            $"LOG passo=1 | avaliacao.Nota={primeira.Nota} | AvaliacoesRecebidas.Count={passeador.AvaliacoesRecebidas.Count} | media={Valor(passeador.MediaAvaliacao)}");

        var (tutor2, passeio2) = PasseioFinalizado(passeador);
        var segunda = passeio2.Avaliar(tutor2, 1, "Passeio ruim", Fabrica.Agora);

        _saida.WriteLine(
            $"LOG passo=2 | avaliacao.Nota={segunda.Nota} | AvaliacoesRecebidas.Count={passeador.AvaliacoesRecebidas.Count} | media={Valor(passeador.MediaAvaliacao)}");
        _saida.WriteLine(
            $"LOG notas acumuladas=[{string.Join(", ", passeador.AvaliacoesRecebidas.Select(a => a.Nota))}] | media esperada=(5+1)/2=3");
    }
}
