﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using Nullinside.Api.Model;

namespace Nullinside.Api.Null.Model;

/// <summary>
///   This exists to support the CLI's migration creation tools. The CLI's migration tools don't work if you don't have a
///   parameterless constructor. We CAN'T have one because you can't use a parameterless constructor if your project
///   references more than once solution with a DbContext in it. This factory is lazy loaded by the CLI automatically
///   simply by implementing the IDesignTimeDbContextFactory interface.
/// </summary>
public class NullinsideContextFactory : IDesignTimeDbContextFactory<NullinsideContext> {
  public NullinsideContext CreateDbContext(string[] args) {
    var optionsBuilder = new DbContextOptionsBuilder<NullinsideContext>();

    string? server = Environment.GetEnvironmentVariable("MYSQL_SERVER");
    string? username = Environment.GetEnvironmentVariable("MYSQL_USERNAME");
    string? password = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");
    optionsBuilder.UseMySQL($"server={server};database=nullinside-null;user={username};password={password}");

    return new NullinsideContext(optionsBuilder.Options);
  }
}