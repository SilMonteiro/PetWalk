using Microsoft.EntityFrameworkCore;

// Implementação sobre o PetWalkDbContext injetado. Um único SaveChanges já é uma
// transação; quando a operação insere avaliações, a mesma transação também recalcula
// a média derivada de cada avaliado.
public class UnidadeDeTrabalho : IUnidadeDeTrabalho
{
    private readonly PetWalkDbContext _contexto;

    public UnidadeDeTrabalho(PetWalkDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public void Salvar()
    {
        // A Avaliacao nasce na coleção de um Passeio rastreado: sem a detecção explícita
        // ela ainda não aparece no change tracker quando a coleta abaixo acontece.
        _contexto.ChangeTracker.DetectChanges();

        // Referências coletadas antes do SaveChanges (depois ele as marca Unchanged).
        // O Id de cada avaliado é lido somente depois, quando o banco já o atribuiu.
        var avaliados = _contexto.ChangeTracker.Entries<Avaliacao>()
            .Where(entrada => entrada.State == EntityState.Added)
            .Select(entrada => entrada.Entity.Avaliado)
            .Distinct()
            .ToList();

        // Fluxos que não inserem avaliação mantêm exatamente o comportamento anterior.
        if (avaliados.Count == 0)
        {
            _contexto.SaveChanges();
            return;
        }

        // Avaliação e média na MESMA transação: nenhum concorrente grava entre o INSERT e
        // o UPDATE, e nenhum leitor vê a avaliação sem a média correspondente.
        using var transacao = _contexto.Database.BeginTransaction();

        _contexto.SaveChanges();

        foreach (var avaliado in avaliados)
        {
            // Média sempre sobre a tabela — nunca sobre a coleção que veio carregada na
            // requisição, que pode estar desatualizada em relação a outra transação.
            var media = _contexto.Database
                .SqlQuery<double>(
                    $"SELECT COALESCE(AVG(Nota), 0.0) AS Value FROM Avaliacoes WHERE AvaliadoId = {avaliado.Id}")
                .Single();

            _contexto.Database.ExecuteSqlInterpolated(
                $"UPDATE Usuarios SET MediaAvaliacao = {media} WHERE Id = {avaliado.Id}");

            // O valor persistido é a fonte da verdade: sincroniza o objeto rastreado para
            // a resposta da requisição devolver a mesma média que ficou gravada.
            avaliado.AtualizarMediaAvaliacao(media);
        }

        transacao.Commit();
    }
}
