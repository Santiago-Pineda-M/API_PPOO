using ApiPoo2.Domain.Personas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiPoo2.Infrastructure.Persistence.Configurations;

public sealed class PersonaUsuarioConfiguration : IEntityTypeConfiguration<Persona>
{
    public void Configure(EntityTypeBuilder<Persona> builder)
    {
        builder.HasOne(p => p.Usuario)
            .WithOne()
            .HasForeignKey<ApiPoo2.Domain.Users.User>(u => u.IdPersona)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Usuario).IsRequired(false);
    }
}
