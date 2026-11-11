using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

// Ponto único de tradução de DbUpdateException para HTTP. Os endpoints continuam sem
// try/catch de persistência: a exceção sobe e é tratada aqui.
public sealed class ErroDePersistenciaHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext contexto, Exception excecao, CancellationToken cancelamento)
    {
        if (excecao is not DbUpdateException falha || ErroDePersistencia.Mapear(falha) is not { } resposta)
        {
            // Não é falha de constraint conhecida: segue para o 500 padrão.
            return false;
        }

        await resposta.ExecuteAsync(contexto);

        return true;
    }
}
