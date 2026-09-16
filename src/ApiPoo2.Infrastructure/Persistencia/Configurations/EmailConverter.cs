using ApiPoo2.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ApiPoo2.Infrastructure.Persistencia.Configurations;

public sealed class EmailConverter : ValueConverter<Email, string>
{
    public EmailConverter()
        : base(email => email.Value, value => Email.From(value))
    {
    }
}