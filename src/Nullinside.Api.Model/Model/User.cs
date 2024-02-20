using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Model;

public class User : ITableModel
{
    public int Id { get; set; }
    public string? Gmail { get; set; }
    public string? Token { get; set; }
    public DateTime UpdatedOn { get; set; }
    public DateTime CreatedOn { get; set; }
    public IEnumerable<UserRole>? Roles { get; set; }
    
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Gmail)
                .IsUnique();
            entity.HasMany(e => e.Roles);
            entity.Property(e => e.Gmail)
                .HasMaxLength(255);
            entity.Property(e => e.Token)
                .HasMaxLength(255);
        });
    }
}