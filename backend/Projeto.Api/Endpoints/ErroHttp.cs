// Mapeamento único das exceções do domínio para respostas HTTP.
//
// Escolhas e por quê:
// - ArgumentException → 400: a entrada enviada é inválida em si (ex.: "Nota deve estar entre 1 e 5",
//   "Valor por hora deve ser maior que zero."). Corrigir o corpo da requisição resolve.
// - InvalidOperationException → 422: a entrada é válida, mas a regra de negócio impede a operação
//   (ex.: "Passeio não finalizado", "Transição não permitida a partir do estado atual"). Repetir a
//   mesma requisição sem mudar o estado não resolve, por isso não é 400.
// - Corpo de erro sempre no formato { "erro": "<mensagem da proposta>" }, o mesmo que o frontend já lê.
// - /cadastro (409 e-mail duplicado, REG-010) e /login (403 bloqueado, REG-011) mantêm os status
//   próprios em UsuarioEndpoints, porque a proposta exige 409 e 403 nesses casos.
// - NaoEncontrado cobre recursos ausentes (404) com o mesmo formato de corpo.
// - Exceções não previstas continuam propagando: o pipeline devolve 500, nunca um 4xx inventado.
public static class ErroHttp
{
    public static IResult Erro(Exception ex) => ex switch
    {
        ArgumentException => Results.BadRequest(new { erro = ex.Message }),
        InvalidOperationException => Results.Json(new { erro = ex.Message }, statusCode: StatusCodes.Status422UnprocessableEntity),
        _ => throw ex
    };

    public static IResult NaoEncontrado(string mensagem) =>
        Results.NotFound(new { erro = mensagem });
}
