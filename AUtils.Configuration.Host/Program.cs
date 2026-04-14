using System.Net;
using System.Reflection;
using AUtils.Configuration.Host.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Configuration API"
    });
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,

            },
            new List<string>()
        }
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});
builder.Services
    .AddPooledDbContextFactory<AppDbContext>(optionsBuilder => 
        optionsBuilder.UseSqlite("Data Source=app.db"));
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});

app.Use(async (context, next) =>
{
    var token = context.Request.Headers.Authorization.ToString();
    if (string.IsNullOrEmpty(token))
    {
        context.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
        await context.Response.CompleteAsync();
        return;
    }

    await using var db = await app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync();
    var userType = await db.Users.Where(x => x.Token == token).Select(x => (UserType?) x.Type)
        .FirstOrDefaultAsync(context.RequestAborted);
    if(userType == null)
    {
        context.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
        await context.Response.CompleteAsync();
        return;
    }

    var isAnonymous = context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null;
    if(!isAnonymous && userType == UserType.Cell)
    {
        context.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
        await context.Response.CompleteAsync();
        return;
    }
    
    await next.Invoke();
});

app.MapControllers();

await using (var db = await app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync())
{
    await db.Database.MigrateAsync();

    if (!await db.Users.AnyAsync())
    {
        var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();

        var token = BCrypt.Net.BCrypt.EnhancedHashPassword(Guid.NewGuid().ToString());
        db.Users.Add(new User
        {
            Type = UserType.Administrator,
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Name = "Admin",
            Token = token
        });
        await db.SaveChangesAsync();
        logger.LogInformation("User: Admin Token: {Token}", token);

        var configsPath = app.Configuration["Configurations"];
        if (!string.IsNullOrEmpty(configsPath))
        {
            var files = Directory.GetFiles(configsPath, "*.json", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file).Split('.');
                await db.Configurations.AddAsync(new Configuration
                {
                    Role = name[0],
                    Environment = name.Length > 1 ? name[1] : string.Empty,
                    Json = await File.ReadAllTextAsync(file),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            var count = await db.SaveChangesAsync();
            logger.LogInformation("Added {Count} configurations", count);
        }
    }
}

app.Run();