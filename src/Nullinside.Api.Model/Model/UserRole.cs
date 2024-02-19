using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Model;

public class UserRole : ITableModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; }
    
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role)
                .HasMaxLength(10);
        });
    }
}