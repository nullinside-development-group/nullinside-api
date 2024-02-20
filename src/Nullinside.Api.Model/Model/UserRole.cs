using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Model;

public class UserRole : ITableModel {
  public int Id { get; set; }
  public int UserId { get; set; }
  public UserRoles Role { get; set; }
  public DateTime RoleAdded { get; set; }

  public void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<UserRole>(entity => {
      entity.HasKey(e => e.Id);
    });
  }
}