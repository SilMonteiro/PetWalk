using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Validação real do modelo de persistência: constrói o modelo EF Core, cria o schema
// SQLite e prova por metadados e por operações reais o que as configurações declaram.
public class ModeloPersistenciaTests
{
    // SQLite em memória só existe enquanto a conexão estiver aberta: cada teste abre a sua.
    private static SqliteConnection AbrirConexao()
    {
        var conexao = new SqliteConnection("Data Source=:memory:");
        conexao.Open();

        return conexao;
    }

    private static PetWalkDbContext CriarContexto(SqliteConnection conexao) =>
        new(new DbContextOptionsBuilder<PetWalkDbContext>().UseSqlite(conexao).Options);

    private static IReadOnlyList<string> LerNomesDeTabelas(PetWalkDbContext contexto)
    {
        var nomes = new List<string>();

        using var comando = contexto.Database.GetDbConnection().CreateCommand();
        comando.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";

        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
        {
            nomes.Add(leitor.GetString(0));
        }

        return nomes;
    }

    // ---------- Construção do modelo ----------

    [Fact]
    public void Modelo_CarregaAsCincoEntidadesSemExcecao()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContexto(conexao);

        var entidades = contexto.Model.GetEntityTypes()
            .Select(t => t.ClrType.Name)
            .OrderBy(n => n)
            .ToArray();

        Assert.Equal(new[] { "Avaliacao", "Notificacao", "Passeio", "Pet", "Usuario" }, entidades);
    }

    [Fact]
    public void Modelo_MapeiaAsCincoTabelas()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContexto(conexao);

        Assert.Equal("Usuarios", contexto.Model.FindEntityType(typeof(Usuario))!.GetTableName());
        Assert.Equal("Pets", contexto.Model.FindEntityType(typeof(Pet))!.GetTableName());
        Assert.Equal("Passeios", contexto.Model.FindEntityType(typeof(Passeio))!.GetTableName());
        Assert.Equal("Avaliacoes", contexto.Model.FindEntityType(typeof(Avaliacao))!.GetTableName());
        Assert.Equal("Notificacoes", contexto.Model.FindEntityType(typeof(Notificacao))!.GetTableName());
    }

    // ---------- Criação real do schema ----------

    [Fact]
    public void Schema_EnsureCreated_CriaAsCincoTabelas()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContexto(conexao);

        Assert.True(contexto.Database.EnsureCreated());

        var tabelas = LerNomesDeTabelas(contexto);

        Assert.Contains("Usuarios", tabelas);
        Assert.Contains("Pets", tabelas);
        Assert.Contains("Passeios", tabelas);
        Assert.Contains("Avaliacoes", tabelas);
        Assert.Contains("Notificacoes", tabelas);
    }

    // ---------- Relacionamentos ----------

    [Theory]
    [InlineData(typeof(Pet), "TutorId", "Usuario")]
    [InlineData(typeof(Passeio), "TutorId", "Usuario")]
    [InlineData(typeof(Passeio), "PasseadorId", "Usuario")]
    [InlineData(typeof(Passeio), "PetId", "Pet")]
    [InlineData(typeof(Avaliacao), "AvaliadorId", "Usuario")]
    [InlineData(typeof(Avaliacao), "AvaliadoId", "Usuario")]
    [InlineData(typeof(Notificacao), "DestinatarioId", "Usuario")]
    [InlineData(typeof(Notificacao), "PasseioId", "Passeio")]
    public void Relacionamento_UsaFkShadowComDeleteRestrict(
        Type tipoEntidade,
        string nomeDaFk,
        string tipoPrincipal)
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContexto(conexao);

        var entidade = contexto.Model.FindEntityType(tipoEntidade)!;

        var relacionamento = entidade.GetForeignKeys()
            .Single(f => f.Properties.Count == 1 && f.Properties[0].Name == nomeDaFk);

        Assert.Equal(tipoPrincipal, relacionamento.PrincipalEntityType.ClrType.Name);
        Assert.Equal(DeleteBehavior.Restrict, relacionamento.DeleteBehavior);
        Assert.True(relacionamento.Properties[0].IsShadowProperty());
        Assert.True(relacionamento.IsRequired);
    }

    [Fact]
    public void Indice_CompostoDePasseioEAvaliador_EhUnico()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContexto(conexao);

        var avaliacao = contexto.Model.FindEntityType(typeof(Avaliacao))!;

        var indice = avaliacao.GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { "PasseioId", "AvaliadorId" }));

        Assert.True(indice.IsUnique);
    }

    // ---------- Coleção primitiva e propriedade get-only ----------

    [Fact]
    public void Vacinas_MapeadaComoColecaoPrimitiva_SemColunaDuplicada()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContexto(conexao);

        var pet = contexto.Model.FindEntityType(typeof(Pet))!;

        var vacinas = pet.FindProperty("_vacinas");

        Assert.NotNull(vacinas);
        Assert.True(vacinas!.IsPrimitiveCollection);
        Assert.Equal("Vacinas", vacinas.GetColumnName());

        // A propriedade pública é somente leitura e foi ignorada: não gera segunda coluna.
        Assert.Null(pet.FindProperty(nameof(Pet.Vacinas)));
        Assert.Single(pet.GetProperties(), p => p.GetColumnName() == "Vacinas");
    }

    [Fact]
    public void Valor_MapeadoPorBackingField_SemSetterPublico()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContexto(conexao);

        var passeio = contexto.Model.FindEntityType(typeof(Passeio))!;

        var valor = passeio.FindProperty(nameof(Passeio.Valor));

        Assert.NotNull(valor);
        Assert.Equal("Valor", valor!.GetColumnName());
        Assert.False(valor.IsNullable);

        // O encapsulamento do domínio continua intacto.
        Assert.Null(typeof(Passeio).GetProperty(nameof(Passeio.Valor))!.SetMethod);
    }

    // ---------- Conversões ----------

    [Fact]
    public void Duracao_ConvertidaParaTicks()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContexto(conexao);

        var duracao = contexto.Model.FindEntityType(typeof(Passeio))!.FindProperty(nameof(Passeio.Duracao))!;
        var conversor = duracao.GetValueConverter();

        Assert.NotNull(conversor);
        Assert.Equal(typeof(long), conversor!.ProviderClrType);

        var original = TimeSpan.FromMinutes(90);
        Assert.Equal(original.Ticks, conversor.ConvertToProvider(original));
        Assert.Equal(original, conversor.ConvertFromProvider(original.Ticks));
    }

    [Theory]
    [InlineData(typeof(Passeio), nameof(Passeio.DataHora))]
    [InlineData(typeof(Avaliacao), nameof(Avaliacao.Data))]
    [InlineData(typeof(Notificacao), nameof(Notificacao.DataEnvio))]
    public void Datas_SaoNormalizadasParaUtcNasDuasDirecoes(Type tipoEntidade, string nomeDaPropriedade)
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContexto(conexao);

        var data = contexto.Model.FindEntityType(tipoEntidade)!.FindProperty(nomeDaPropriedade)!;
        var conversor = data.GetValueConverter();

        Assert.NotNull(conversor);

        var local = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var gravada = (DateTime)conversor!.ConvertToProvider(local)!;

        Assert.Equal(local.ToUniversalTime(), gravada);
        Assert.Equal(DateTimeKind.Utc, gravada.Kind);

        // O SQLite devolve Unspecified: o conversor recompõe o Kind sem deslocar o instante.
        var devolvida = (DateTime)conversor.ConvertFromProvider(
            new DateTime(2026, 1, 1, 15, 0, 0, DateTimeKind.Unspecified))!;

        Assert.Equal(DateTimeKind.Utc, devolvida.Kind);
        Assert.Equal(new DateTime(2026, 1, 1, 15, 0, 0, DateTimeKind.Utc), devolvida);
    }

    // ---------- Persistência real ----------

    [Fact]
    public void Persistencia_RestauraPasseioComValorDuracaoDataUtcEOsTresRelacionamentos()
    {
        using var conexao = AbrirConexao();

        int passeioId;
        int tutorId;
        int passeadorId;
        int petId;
        decimal valorEsperado;

        using (var escrita = CriarContexto(conexao))
        {
            escrita.Database.EnsureCreated();

            var (tutor, passeador, pet, passeio) =
                Fabrica.CriarPasseioSolicitado(duracao: TimeSpan.FromMinutes(90), valorHora: 40m);

            escrita.AddRange(tutor, passeador, pet, passeio);
            escrita.SaveChanges();

            passeioId = passeio.Id;
            tutorId = tutor.Id;
            passeadorId = passeador.Id;
            petId = pet.Id;
            valorEsperado = passeio.Valor;
        }

        using var leitura = CriarContexto(conexao);
        var recarregado = leitura.Passeios
            .Include(p => p.Tutor)
            .Include(p => p.Passeador)
            .Include(p => p.Pet)
            .Single(p => p.Id == passeioId);

        Assert.Equal(TimeSpan.FromMinutes(90), recarregado.Duracao);
        Assert.Equal(valorEsperado, recarregado.Valor);
        Assert.Equal(Fabrica.Agora.AddDays(1), recarregado.DataHora);
        Assert.Equal(DateTimeKind.Utc, recarregado.DataHora.Kind);
        Assert.Equal(StatusPasseio.SOLICITADO, recarregado.Status);

        Assert.Equal(tutorId, recarregado.Tutor.Id);
        Assert.Equal(passeadorId, recarregado.Passeador.Id);
        Assert.Equal(petId, recarregado.Pet.Id);
        Assert.NotEqual(recarregado.Tutor.Id, recarregado.Passeador.Id);
    }

    [Fact]
    public void Persistencia_RestauraVacinasEEnderecoPrivado()
    {
        using var conexao = AbrirConexao();

        int tutorId;
        int petId;

        using (var escrita = CriarContexto(conexao))
        {
            escrita.Database.EnsureCreated();

            var tutor = Fabrica.CriarUsuario(TipoPerfil.TUTOR);
            var pet = Fabrica.CriarPet(tutor);
            pet.RegistrarVacina("V8");
            pet.RegistrarVacina("Antirrabica");

            escrita.AddRange(tutor, pet);
            escrita.SaveChanges();

            tutorId = tutor.Id;
            petId = pet.Id;
        }

        using var leitura = CriarContexto(conexao);

        var petRecarregado = leitura.Pets.Include(p => p.Tutor).Single(p => p.Id == petId);
        Assert.Equal(new[] { "V8", "Antirrabica" }, petRecarregado.Vacinas);

        var tutorRecarregado = leitura.Usuarios.Single(u => u.Id == tutorId);
        Assert.Equal("Rua A, 10", tutorRecarregado.ObterEnderecoPara(tutorRecarregado));
        Assert.Contains(petRecarregado, tutorRecarregado.Pets);
    }

    [Fact]
    public void Persistencia_RestauraAvaliacaoComAvaliadorEAvaliadoDistintos()
    {
        using var conexao = AbrirConexao();

        int avaliacaoId;
        int tutorId;
        int passeadorId;

        using (var escrita = CriarContexto(conexao))
        {
            escrita.Database.EnsureCreated();

            var (tutor, passeador, pet, passeio) = Fabrica.CriarPasseioSolicitado();

            passeio.Aceitar(passeador);
            passeio.Iniciar(passeador);
            passeio.Finalizar(passeador);

            var avaliacao = passeio.Avaliar(tutor, 5, "Otimo passeio", Fabrica.Agora);

            escrita.AddRange(tutor, passeador, pet, passeio);
            escrita.SaveChanges();

            avaliacaoId = avaliacao.Id;
            tutorId = tutor.Id;
            passeadorId = passeador.Id;
        }

        using var leitura = CriarContexto(conexao);

        var recarregada = leitura.Avaliacoes
            .Include(a => a.Avaliador)
            .Include(a => a.Avaliado)
            .Include(a => a.Passeio)
            .Single(a => a.Id == avaliacaoId);

        Assert.Equal(5, recarregada.Nota);
        Assert.Equal("Otimo passeio", recarregada.Comentario);
        Assert.Equal(DateTimeKind.Utc, recarregada.Data.Kind);

        Assert.Equal(tutorId, recarregada.Avaliador.Id);
        Assert.Equal(passeadorId, recarregada.Avaliado.Id);
        Assert.NotEqual(recarregada.Avaliador.Id, recarregada.Avaliado.Id);

        // REG-001: a média persistida do avaliado sobrevive à recarga.
        var avaliado = leitura.Usuarios.Single(u => u.Id == passeadorId);
        Assert.Equal(5d, avaliado.MediaAvaliacao);
    }

    [Fact]
    public void Persistencia_RestauraNotificacaoSemRegenerarMensagem()
    {
        using var conexao = AbrirConexao();

        int notificacaoId;

        using (var escrita = CriarContexto(conexao))
        {
            escrita.Database.EnsureCreated();

            var (tutor, passeador, pet, passeio) = Fabrica.CriarPasseioSolicitado();
            var notificacao = new Notificacao(passeador, passeio, TipoNotificacao.PASSEIO_SOLICITADO, Fabrica.Agora);
            notificacao.MarcarComoLida();

            escrita.AddRange(tutor, passeador, pet, passeio, notificacao);
            escrita.SaveChanges();

            notificacaoId = notificacao.Id;
        }

        using var leitura = CriarContexto(conexao);

        var recarregada = leitura.Notificacoes
            .Include(n => n.Destinatario)
            .Include(n => n.Passeio)
            .Single(n => n.Id == notificacaoId);

        Assert.Equal(TipoNotificacao.PASSEIO_SOLICITADO, recarregada.Tipo);
        Assert.Equal("Novo passeio solicitado.", recarregada.Mensagem);
        Assert.True(recarregada.Lida);
        Assert.Equal(DateTimeKind.Utc, recarregada.DataEnvio.Kind);
        Assert.NotNull(recarregada.Destinatario);
        Assert.NotNull(recarregada.Passeio);
    }
}
