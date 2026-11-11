using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

// Verificação executada uma única vez na inicialização: o schema pertence às migrations, nunca
// ao startup. Cada falha interrompe antes da primeira requisição e descreve o estado encontrado
// com a ação manual correspondente.
public static class VerificacaoDeSchema
{
    // Comando único para os estados em que aplicar migrations resolve o problema: banco ausente
    // e migrations pendentes. Não é usado quando o histórico já está aplicado.
    private const string InstrucaoDeMigracao =
        "Aplique as migrations manualmente e reinicie a API:\n" +
        "  dotnet tool restore\n" +
        "  dotnet ef database update --project backend/Projeto.Api --startup-project backend/Projeto.Api";

    private const string MensagemArquivoInexistente =
        "O arquivo do banco SQLite da API não existe ou ainda não foi criado.\n" + InstrucaoDeMigracao;

    private const string MensagemArquivoInacessivel =
        "O arquivo do banco SQLite da API não pôde ser acessado (permissões ou caminho inválido).\n" +
        "Verifique as permissões do arquivo e do diretório configurados e reinicie a API.";

    private const string MensagemBancoIlegivel =
        "Não foi possível ler o schema do banco SQLite da API.\n" +
        "Verifique se o arquivo configurado é um banco SQLite válido e se está acessível.";

    // Divergência: o banco tem schema, mas não tem as migrations que o explicariam. Repetir
    // database update falharia nas tabelas já existentes, então a orientação é outra.
    private const string OrientacaoDivergencia =
        "Verifique se o arquivo apontado é o banco correto, restaure um backup compatível "
        + "ou recrie o banco a partir das migrations. A API não altera o schema automaticamente.";

    private const string MensagemDivergenciaSemHistorico =
        "O banco SQLite da API já contém tabelas do modelo, mas nenhuma migration aplicada está registrada.\n"
        + "O schema atual não corresponde ao histórico de migrations.\n"
        + OrientacaoDivergencia;

    // As mensagens citam apenas migrations, tabelas e colunas — nunca a connection string, o
    // caminho do arquivo ou qualquer outro dado de conexão.
    public static void GarantirSchemaAplicado(PetWalkDbContext contexto)
    {
        // A connection string é lida das opções, sem abrir conexão. Ainda assim, acessar
        // DatabaseFacade inicializa os serviços do provedor SQLite: a verificação pressupõe o
        // SQLite nativo disponível, como qualquer uso do contexto. A PetWalkApiFactory configura
        // o contexto com uma DbConnection já aberta; ali GetConnectionString() é nulo.
        var construtorDeConexao = new SqliteConnectionStringBuilder(
            contexto.Database.GetConnectionString()
            ?? contexto.Database.GetDbConnection().ConnectionString);

        // Banco em memória vive apenas enquanto a conexão existir e é criado por quem o abriu
        // (nos testes, EnsureCreated). Migrations não se aplicam a ele: não há arquivo nem
        // histórico, e validá-lo quebraria a fábrica de testes sem proteger nada.
        if (construtorDeConexao.Mode == SqliteOpenMode.Memory
            || construtorDeConexao.DataSource == ":memory:")
        {
            return;
        }

        GarantirArquivoAcessivel(construtorDeConexao.DataSource);

        var pendentes = LerDoBanco(() => contexto.Database.GetPendingMigrations().ToList());
        var existentes = LerDoBanco(() => TabelasExistentes(contexto));
        var esperadas = ColunasEsperadas(contexto);

        if (pendentes.Count > 0)
        {
            var aplicadas = LerDoBanco(() => contexto.Database.GetAppliedMigrations().ToList());

            // Tabelas do modelo sem nenhuma migration registrada: o schema não veio das
            // migrations (por exemplo, EnsureCreated). Aplicar migrations falharia porque as
            // tabelas já existem; o caminho é verificar/restaurar ou recriar o banco.
            if (aplicadas.Count == 0 && existentes.Any(tabela => esperadas.ContainsKey(tabela)))
            {
                throw new InvalidOperationException(MensagemDivergenciaSemHistorico);
            }

            // Upgrade normal: alguma migration já foi aplicada e há novas a aplicar.
            throw new InvalidOperationException(
                "O banco SQLite da API está com migrations pendentes "
                + $"({string.Join(", ", pendentes)}).\n" + InstrucaoDeMigracao);
        }

        var (tabelasAusentes, colunasAusentes) =
            LerDoBanco(() => CompararComModelo(contexto, esperadas, existentes));

        if (tabelasAusentes.Count > 0 || colunasAusentes.Count > 0)
        {
            throw new InvalidOperationException(
                MensagemSchemaIncompleto(tabelasAusentes, colunasAusentes));
        }
    }

    private static void GarantirArquivoAcessivel(string dataSource)
    {
        string caminho;
        try
        {
            caminho = Path.GetFullPath(dataSource);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException(MensagemArquivoInacessivel);
        }

        FileAttributes atributos;
        try
        {
            atributos = File.GetAttributes(caminho);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new InvalidOperationException(MensagemArquivoInexistente);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // Acesso negado e falhas de IO não podem ser confundidos com arquivo inexistente.
            throw new InvalidOperationException(MensagemArquivoInacessivel);
        }

        // O caminho configurado aponta para um diretório: não é um arquivo de banco utilizável.
        if ((atributos & FileAttributes.Directory) != 0)
        {
            throw new InvalidOperationException(MensagemArquivoInacessivel);
        }
    }

    // Erros do provedor trazem detalhes do arquivo; o operador recebe só a orientação sanitizada.
    private static T LerDoBanco<T>(Func<T> leitura)
    {
        try
        {
            return leitura();
        }
        catch (SqliteException)
        {
            throw new InvalidOperationException(MensagemBancoIlegivel);
        }
    }

    private static List<string> TabelasExistentes(PetWalkDbContext contexto) =>
        contexto.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'")
            .ToList();

    // Tabela e coluna vêm do modelo EF Core, nunca de entrada do usuário: a consulta usa
    // exatamente o nome definido no mapeamento (HasColumnName incluído).
    private static (List<string> Tabelas, List<string> Colunas) CompararComModelo(
        PetWalkDbContext contexto,
        Dictionary<string, HashSet<string>> esperadas,
        List<string> tabelasExistentes)
    {
        var existentes = new HashSet<string>(tabelasExistentes, StringComparer.OrdinalIgnoreCase);
        var tabelasAusentes = new List<string>();
        var colunasAusentes = new List<string>();

        foreach (var (tabela, colunas) in esperadas.OrderBy(
            par => par.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!existentes.Contains(tabela))
            {
                tabelasAusentes.Add(tabela);
                continue;
            }

            var atuais = ColunasExistentes(contexto, tabela);

            colunasAusentes.AddRange(colunas
                .Where(coluna => !atuais.Contains(coluna))
                .OrderBy(coluna => coluna, StringComparer.OrdinalIgnoreCase)
                .Select(coluna => $"{tabela}.{coluna}"));
        }

        return (tabelasAusentes, colunasAusentes);
    }

    private static Dictionary<string, HashSet<string>> ColunasEsperadas(PetWalkDbContext contexto)
    {
        var porTabela = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var tipo in contexto.Model.GetEntityTypes())
        {
            var objeto = StoreObjectIdentifier.Create(tipo, StoreObjectType.Table);
            if (objeto is null)
            {
                continue;
            }

            if (!porTabela.TryGetValue(objeto.Value.Name, out var colunas))
            {
                colunas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                porTabela[objeto.Value.Name] = colunas;
            }

            foreach (var propriedade in tipo.GetProperties())
            {
                var coluna = propriedade.GetColumnName(objeto.Value);
                if (!string.IsNullOrWhiteSpace(coluna))
                {
                    colunas.Add(coluna);
                }
            }
        }

        return porTabela;
    }

    private static HashSet<string> ColunasExistentes(PetWalkDbContext contexto, string tabela)
    {
        // O nome da tabela vem do modelo e é enviado como parâmetro: nenhum valor externo é
        // interpolado no SQL.
        return new HashSet<string>(
            contexto.Database
                .SqlQuery<string>($"SELECT name AS Value FROM pragma_table_info({tabela})")
                .ToList(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static string MensagemSchemaIncompleto(
        IReadOnlyCollection<string> tabelasAusentes,
        IReadOnlyCollection<string> colunasAusentes)
    {
        var faltas = new List<string>();

        if (tabelasAusentes.Count > 0)
        {
            faltas.Add($"faltam tabelas: {string.Join(", ", tabelasAusentes)}");
        }

        if (colunasAusentes.Count > 0)
        {
            faltas.Add($"faltam colunas: {string.Join(", ", colunasAusentes)}");
        }

        // Histórico já aplicado: repetir "database update" não corrige o banco, então a
        // orientação é verificar/restaurar ou recriar a partir das migrations.
        return "O schema do banco SQLite da API não corresponde às migrations registradas "
            + $"({string.Join("; ", faltas)}).\n" + OrientacaoDivergencia;
    }
}
