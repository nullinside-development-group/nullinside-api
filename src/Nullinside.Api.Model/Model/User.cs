using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Model;

public class User : ITableModel
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? Token { get; set; }
    public IEnumerable<UserRole>? Roles { get; set; }
    
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasMany(e => e.Roles);
            entity.Property(e => e.Email)
                .HasMaxLength(255);
            entity.Property(e => e.Token)
                .HasMaxLength(255);
        });
    }
}