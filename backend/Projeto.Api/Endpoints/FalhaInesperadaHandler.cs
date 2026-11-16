using Microsoft.AspNetCore.Diagnostics;

// Última barreira do tratamento de erros. O ErroDePersistenciaHandler já converteu as
// falhas de constraint conhecidas em 409/422; o que chega aqui é defeito não previsto.
// O cliente recebe só a mensagem genérica porque, por definição, ele não consegue corrigir
// a falha mudando a requisição; o detalhe (mensagem real e stack trace) fica no log.
public sealed class FalhaInesperadaHandler : IExceptionHandler
{
    private const string MensagemPadrao = "Erro interno. Tente novamente mais tarde.";

    private readonly ILogger<FalhaInesperadaHandler> _logger;

    public FalhaInesperadaHandler(ILogger<FalhaInesperadaHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext contexto, Exception excecao, CancellationToken cancelamento)
    {
        // O log não é opcional: quando um IExceptionHandler trata a exceção, o middleware
        // considera o caso resolvido e não registra nada por conta própria. Sem esta linha,
        // a falha desapareceria sem rastro.
        _logger.LogError(excecao, "Falha inesperada em {Metodo} {Caminho}",
            contexto.Request.Method, contexto.Request.Path);

        contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await contexto.Response.WriteAsJsonAsync(new { erro = MensagemPadrao }, cancelamento);

        return true;
    }
}
