using System.Reflection;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

using Nullinside.Api.Common;
using Nullinside.Api.Common.AspNetCore.Middleware;
using Nullinside.Api.Common.Docker;
using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Shared;

using WebApplicationBuilder = Microsoft.AspNetCore.Builder.WebApplicationBuilder;

const string corsKey = "_customAllowedSpecificOrigins";
string[] domains = [
  "https://www.nullinside.com",
  "https://nullinside.com",
#if DEBUG
  "http://localhost:4200",
  "http://127.0.0.1:4200"
#endif
];

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddLog4Net();

// Secrets are mounted into the container.
string? server = Environment.GetEnvironmentVariable("MYSQL_SERVER");
string? username = Environment.GetEnvironmentVariable("MYSQL_USERNAME");
string? password = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");
builder.Services.AddDbContext<INullinsideContext, NullinsideContext>(optionsBuilder =>
  optionsBuilder.UseMySQL(
    $"server={server};database=nullinside;user={username};password={password};AllowUserVariables=true;",
    builder => {
      builder.CommandTimeout(60 * 5);
      builder.EnableRetryOnFailure(3);
    }));
builder.Services.AddScoped<IAuthorizationHandler, BasicAuthorizationHandler>();
builder.Services.AddTransient<ITwitchApiProxy, TwitchApiProxy>();
builder.Services.AddAuthentication()
  .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Bearer", _ => { });
builder.Services.AddScoped<IDockerProxy, DockerProxy>();
builder.Services.AddSingleton<IWebSocketPersister, WebSocketPersister>();

builder.Services.AddAuthorization(options => {
  // Dynamically add all of the user roles that exist in the application.
  foreach (object? role in Enum.GetValues(typeof(UserRoles))) {
    string? roleName = role?.ToString();
    if (null == roleName) {
      continue;
    }

    options.AddPolicy(roleName, policy => policy.Requirements.Add(new BasicAuthorizationRequirement(roleName)));
  }

  options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireRole(nameof(UserRoles.USER))
    .RequireAuthenticatedUser()
    .Build();
});

builder.Services.AddSwaggerGen(c => {
  c.SwaggerDoc("v1", new OpenApiInfo { Title = "nullinside", Version = "v1" });
  c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
    Description = """
                  JWT Authorization header using the Bearer scheme. \r\n\r\n
                                        Enter 'Bearer' [space] and then your token in the text input below.
                                        \r\n\r\nExample: 'Bearer 12345abcdef'
                  """,
    Name = "Authorization",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer"
  });

  c.AddSecurityRequirement(new OpenApiSecurityRequirement {
    {
      new OpenApiSecurityScheme {
        Reference = new OpenApiReference {
          Type = ReferenceType.SecurityScheme,
          Id = "Bearer"
        },
        Scheme = "oauth2",
        Name = "Bearer",
        In = ParameterLocation.Header
      },
      new List<string>()
    }
  });

  string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
  string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
  c.IncludeXmlComments(xmlPath);
});

// Add services to the container.
builder.Services.AddCors(options => {
  options.AddPolicy(corsKey,
    policyBuilder => {
      policyBuilder.WithOrigins(domains)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();
app.UsePathBase("/api/v1");
app.UseAuthentication();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(corsKey);
app.UseAuthorization();

var webSocketOptions = new WebSocketOptions {
  KeepAliveInterval = TimeSpan.FromMinutes(2)
};

foreach (string domain in domains) {
  webSocketOptions.AllowedOrigins.Add(domain);
}

app.UseWebSockets(webSocketOptions);

app.MapControllers();

app.Run();