using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

// Mapeamento persistente de Usuario. Os relacionamentos com Pet e Avaliacao
// ficam nas configuracoes dessas entidades.
public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedOnAdd();

        builder.Property(u => u.Nome).IsRequired();
        builder.Property(u => u.Email).IsRequired().UseCollation("NOCASE");
        builder.Property(u => u.Telefone).IsRequired();
        builder.Property(u => u.SenhaHash).IsRequired();

        // Enums persistidos como texto para preservar legibilidade e estabilidade do valor.
        builder.Property(u => u.TipoPerfil).HasConversion<string>().IsRequired();
        builder.Property(u => u.Status).HasConversion<string>().IsRequired();
        builder.Property(u => u.TipoCobranca).HasConversion<string>().IsRequired();

        // Tipos CLR reais: decimal e double seguem o mapeamento padrao do provider SQLite.
        builder.Property(u => u.ValorHora);
        builder.Property(u => u.MediaAvaliacao);

        // REG-007: o endereco continua privado; o EF acessa apenas o backing field.
        builder.Property<string>("_endereco")
            .HasColumnName("Endereco")
            .HasField("_endereco")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();
    }
}
