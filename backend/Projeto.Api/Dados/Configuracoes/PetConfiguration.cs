using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

// Mapeamento persistente de Pet. O relacionamento com Passeio fica na configuracao de Passeio.
public class PetConfiguration : IEntityTypeConfiguration<Pet>
{
    public void Configure(EntityTypeBuilder<Pet> builder)
    {
        builder.ToTable("Pets");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.Nome).IsRequired();
        builder.Property(p => p.Especie).IsRequired();
        builder.Property(p => p.Raca).IsRequired();
        builder.Property(p => p.Observacoes).IsRequired();
        builder.Property(p => p.Idade);

        // Enum persistido como texto, igual as demais entidades.
        builder.Property(p => p.Porte).HasConversion<string>().IsRequired();

        // A convencao mapeia colecoes primitivas apenas com getter e setter. Vacinas e
        // somente leitura, entao o mapeamento e feito no backing field _vacinas e a
        // propriedade publica e ignorada para nao gerar coluna duplicada ou conflitante.
        builder.Ignore(p => p.Vacinas);

        builder.PrimitiveCollection<List<string>>("_vacinas")
            .HasColumnName("Vacinas")
            .HasField("_vacinas")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Pet nao possui TutorId no dominio: a chave estrangeira e uma shadow property.
        // Restrict impede excluir um usuario que ainda possua pets.
        builder.HasOne(p => p.Tutor)
            .WithMany(u => u.Pets)
            .HasForeignKey("TutorId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("TutorId");
    }
}
