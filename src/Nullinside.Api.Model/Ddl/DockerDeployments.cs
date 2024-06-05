using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Ddl;

/// <summary>
/// Represents docker deployments both in container form as well as docker compose project form.
/// </summary>
public class DockerDeployments : ITableModel {
  /// <summary>
  /// Gets or sets the unique identifier of the row.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  /// True if the row is in reference to a docker compose project, false if it is just a docker container.
  /// </summary>
  public bool IsDockerComposeProject { get; set; }

  /// <summary>
  /// The directory on the server where the project is.
  /// </summary>
  public string? ServerDir { get; set; }

  /// <summary>
  /// Gets or sets the docker compose project name if <seealso cref="IsDockerComposeProject" /> is true, else the
  /// container name.
  /// </summary>
  public required string Name { get; set; }

  /// <summary>
  /// Gets or sets the display name.
  /// </summary>
  public required string DisplayName { get; set; }

  /// <summary>
  /// Gets or sets the comment that should be shown on the screen in reference to the docker project/container.
  /// </summary>
  public string? Notes { get; set; }

  /// <summary>
  /// The method used to configure the POCOs of the table.
  /// </summary>
  /// <param name="modelBuilder">The model builder.</param>
  public void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<DockerDeployments>(entity => {
      entity.HasKey(e => e.Id);

      // Docker compose projects and docker containers should have unique names from one another.
      entity.HasIndex(e => new {
        e.IsDockerComposeProject,
        e.Name
      });

      // Display names should be unique
      entity.HasIndex(e => new { e.DisplayName });
      entity.Property(e => e.Name)
        .HasMaxLength(128)
        .IsRequired();
      entity.Property(e => e.DisplayName)
        .HasMaxLength(50)
        .IsRequired();
      entity.Property(e => e.Notes)
        .HasMaxLength(255);
      entity.Property(e => e.ServerDir)
        .HasMaxLength(255);
    });
  }
}