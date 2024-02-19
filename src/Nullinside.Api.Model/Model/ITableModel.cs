using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Model;

public interface ITableModel
{
    public void OnModelCreating(ModelBuilder modelBuilder);
}