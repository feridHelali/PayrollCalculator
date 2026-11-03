using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayrollCalculator.Domain.Users;

namespace PayrollCalculator.Infrastructure.Persistence;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(u => u.Id);
        b.Ignore(u => u.IsTransient);
        b.Property(u => u.UserName).IsRequired().HasMaxLength(User.MaxUserNameLength);
        b.HasIndex(u => u.UserName).IsUnique();
        b.Property(u => u.DisplayName).IsRequired().HasMaxLength(User.MaxDisplayNameLength);
        b.Property(u => u.PasswordHash).IsRequired().HasMaxLength(200);
    }
}
