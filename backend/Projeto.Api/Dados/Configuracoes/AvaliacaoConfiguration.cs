using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

// Mapeamento persistente de Avaliacao. Duas relacoes com Usuario e uma com Passeio:
// todas recebem FK e inversa explicitas para nao depender da convencao.
public class AvaliacaoConfiguration : IEntityTypeConfiguration<Avaliacao>
{
    // SQLite nao preserva DateTimeKind: normaliza para UTC na escrita e devolve Utc na leitura.
    private static readonly ValueConverter<DateTime, DateTime> ConversorDataHoraUtc = new(
        valor => valor.ToUniversalTime(),
        valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));

    public void Configure(EntityTypeBuilder<Avaliacao> builder)
    {
        builder.ToTable("Avaliacoes");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        builder.Property(a => a.Nota);
        builder.Property(a => a.Comentario).IsRequired();
        builder.Property(a => a.Data).HasConversion(ConversorDataHoraUtc);

        // Avaliador nao possui colecao inversa no dominio: a inversa e nula.
        builder.HasOne(a => a.Avaliador)
            .WithMany()
            .HasForeignKey("AvaliadorId")
            .OnDelete(DeleteBehavior.Restrict);

        // Avaliado fecha a inversa Usuario.AvaliacoesRecebidas, que alimenta MediaAvaliacao.
        // O EF resolve o backing field _avaliacoesRecebidas pela convencao _camelCase,
        // sem setter publico e sem uma segunda colecao paralela.
        builder.HasOne(a => a.Avaliado)
            .WithMany(u => u.AvaliacoesRecebidas)
            .HasForeignKey("AvaliadoId")
            .OnDelete(DeleteBehavior.Restrict);

        // Passeio fecha a inversa Passeio.Avaliacoes.
        builder.HasOne(a => a.Passeio)
            .WithMany(p => p.Avaliacoes)
            .HasForeignKey("PasseioId")
            .OnDelete(DeleteBehavior.Restrict);

        // REG-001 no banco: o mesmo usuario nao avalia o mesmo passeio duas vezes.
        builder.HasIndex("PasseioId", "AvaliadorId").IsUnique();

        builder.HasIndex("AvaliadorId");
        builder.HasIndex("AvaliadoId");
    }
}
