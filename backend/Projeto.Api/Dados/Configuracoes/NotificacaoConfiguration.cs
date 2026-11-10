using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

// Mapeamento persistente de Notificacao. Nenhuma das duas navegacoes possui
// colecao inversa no dominio, entao a inversa e sempre nula.
public class NotificacaoConfiguration : IEntityTypeConfiguration<Notificacao>
{
    // SQLite nao preserva DateTimeKind: normaliza para UTC na escrita e devolve Utc na leitura.
    private static readonly ValueConverter<DateTime, DateTime> ConversorDataHoraUtc = new(
        valor => valor.ToUniversalTime(),
        valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));

    public void Configure(EntityTypeBuilder<Notificacao> builder)
    {
        builder.ToTable("Notificacoes");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedOnAdd();

        // A mensagem persistida e a do momento do envio e nunca e regenerada na leitura.
        builder.Property(n => n.Mensagem).IsRequired();
        builder.Property(n => n.Lida);
        builder.Property(n => n.DataEnvio).HasConversion(ConversorDataHoraUtc);

        // Enum persistido como texto, igual as demais entidades.
        builder.Property(n => n.Tipo).HasConversion<string>().IsRequired();

        // Nenhuma das duas FKs existe no dominio: ambas sao shadow properties.
        builder.HasOne(n => n.Destinatario)
            .WithMany()
            .HasForeignKey("DestinatarioId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Passeio)
            .WithMany()
            .HasForeignKey("PasseioId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("DestinatarioId");
        builder.HasIndex("PasseioId");
    }
}
