using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Traduz falhas de constraint do SQLite em respostas HTTP sem comparar mensagens de erro:
// usa o código estendido do SQLite e a entidade que o EF tentou gravar.
public static class ErroDePersistencia
{
    private const int ViolacaoUnica = 2067;             // SQLITE_CONSTRAINT_UNIQUE
    private const int ViolacaoDeChavePrimaria = 1555;   // SQLITE_CONSTRAINT_PRIMARYKEY
    private const int ViolacaoDeChaveEstrangeira = 787; // SQLITE_CONSTRAINT_FOREIGNKEY

    // SQLite implementa ON DELETE RESTRICT internamente como gatilho e reporta 1811
    // (SQLITE_CONSTRAINT_TRIGGER), não 787. O schema não tem gatilhos, então 1811 aqui
    // significa restrição de exclusão por FK.
    private const int ViolacaoDeRestricao = 1811;       // SQLITE_CONSTRAINT_TRIGGER

    // Devolve null quando não é uma falha de constraint conhecida: o chamador deixa a
    // exceção seguir, para o 500 continuar significando defeito e não entrada inválida.
    public static IResult? Mapear(DbUpdateException excecao)
    {
        // Conflito otimista: outra operação alterou a linha entre a leitura e a gravação.
        // Vem antes do provedor porque DbUpdateConcurrencyException não tem SqliteException interna.
        if (excecao is DbUpdateConcurrencyException)
        {
            return Results.Conflict(new
            {
                erro = "O passeio foi alterado por outra operação. Recarregue e tente novamente."
            });
        }

        if (excecao.InnerException is not SqliteException sqlite)
        {
            return null;
        }

        var codigo = sqlite.SqliteExtendedErrorCode != 0
            ? sqlite.SqliteExtendedErrorCode
            : sqlite.SqliteErrorCode;

        return codigo switch
        {
            ViolacaoUnica or ViolacaoDeChavePrimaria => Duplicidade(excecao),
            ViolacaoDeChaveEstrangeira or ViolacaoDeRestricao => ChaveEstrangeira(excecao),
            _ => null
        };
    }

    // Só quem está sendo inserido pode violar um índice único. O estado importa: ao avaliar,
    // o Usuario também vai no mesmo SaveChanges, mas como Modified (média recalculada).
    private static IResult Duplicidade(DbUpdateException excecao)
    {
        var adicionada = excecao.Entries
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Metadata.ClrType)
            .FirstOrDefault(tipo => tipo == typeof(Usuario) || tipo == typeof(Avaliacao));

        // REG-010: o único índice único de Usuario é o do e-mail.
        if (adicionada == typeof(Usuario))
        {
            return Results.Conflict(new { erro = "E-mail já cadastrado" });
        }

        // REG-001: o único índice único de Avaliacao é (PasseioId, AvaliadorId).
        if (adicionada == typeof(Avaliacao))
        {
            return Results.Json(
                new { erro = "Passeio já avaliado por este usuário" },
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        // Índice único novo, ainda sem mensagem própria: conflito genérico.
        return Results.Conflict(new { erro = "Registro duplicado" });
    }

    // A mensagem específica só vale quando o Pet está sendo excluído. Qualquer outra FK
    // envolvendo Pet (inserção, alteração) cai na mensagem genérica.
    private static IResult ChaveEstrangeira(DbUpdateException excecao)
    {
        var excluindoPet = excecao.Entries
            .Any(e => e.State == EntityState.Deleted && e.Metadata.ClrType == typeof(Pet));

        return excluindoPet
            ? Results.Conflict(new { erro = "Pet possui passeios e não pode ser excluído" })
            : Results.Conflict(new { erro = "Operação não permitida: o registro está em uso" });
    }
}
