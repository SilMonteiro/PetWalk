using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

// Mapeamento persistente de Passeio. O relacionamento com Avaliacao fica na
// configuracao de Avaliacao, unica dona da navegacao Passeio.Avaliacoes.
public class PasseioConfiguration : IEntityTypeConfiguration<Passeio>
{
    // SQLite nao preserva DateTimeKind: normaliza para UTC na escrita e devolve Utc na leitura.
    private static readonly ValueConverter<DateTime, DateTime> ConversorDataHoraUtc = new(
        valor => valor.ToUniversalTime(),
        valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));

    // Ticks preservam a precisao total de TimeSpan e permitem ordenacao numerica no SQLite.
    private static readonly ValueConverter<TimeSpan, long> ConversorDuracaoTicks = new(
        duracao => duracao.Ticks,
        ticks => TimeSpan.FromTicks(ticks));

    public void Configure(EntityTypeBuilder<Passeio> builder)
    {
        builder.ToTable("Passeios");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.Local).IsRequired();
        builder.Property(p => p.DataHora).HasConversion(ConversorDataHoraUtc);
        builder.Property(p => p.Duracao).HasConversion(ConversorDuracaoTicks);

        // Enum persistido como texto, igual as demais entidades.
        builder.Property(p => p.Status).HasConversion<string>().IsRequired();

        // Valor e get-only e pertence ao dominio: materializado pelo backing field,
        // sem setter publico e sem recalcular a politica de preco.
        builder.Property(p => p.Valor).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Duas relacoes distintas com Usuario exigem inversa explicita e nula:
        // sem isso a convencao nao distingue Tutor de Passeador.
        builder.HasOne(p => p.Tutor)
            .WithMany()
            .HasForeignKey("TutorId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Passeador)
            .WithMany()
            .HasForeignKey("PasseadorId")
            .OnDelete(DeleteBehavior.Restrict);

        // Pet nao possui colecao de Passeios: a inversa tambem e nula.
        builder.HasOne(p => p.Pet)
            .WithMany()
            .HasForeignKey("PetId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("TutorId");
        builder.HasIndex("PasseadorId");
        builder.HasIndex("PetId");
    }
}
