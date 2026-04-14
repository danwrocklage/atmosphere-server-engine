using System.Text.Json;
using System.Text.Json.Nodes;
using AUtils.Configuration.Host.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AUtils.Configuration.Host.Api;

/// <summary>
/// Manage configurations
/// </summary>
[ApiController]
[Route("/api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> mContextFactory;

    /// <summary>
    /// .ctor
    /// </summary>
    public ConfigController(IDbContextFactory<AppDbContext> contextFactory)
    {
        mContextFactory = contextFactory;
    }

    /// <summary>
    /// [Public] Get configuration for cell role and environment configuration
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{role}.{configuration}")]
    public async Task<IActionResult> GetConfiguration([FromRoute] string role, [FromRoute] string configuration,
        CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(configuration))
            return BadRequest();

        var cache = ConfigurationCache.Get(role, configuration);
        if (!string.IsNullOrEmpty(cache))
            return Content(cache);
        
        await using var db = await mContextFactory.CreateDbContextAsync(token);
        var hasConfiguration = true;
        var json = await db.Configurations
            .Where(x => x.Role == role && x.Environment == configuration)
            .Select(x => x.Json)
            .FirstOrDefaultAsync(token);

        if (string.IsNullOrEmpty(json))
        {
            hasConfiguration = false;
            json = await db.Configurations
                .Where(x => x.Role == role && x.Environment == string.Empty)
                .Select(x => x.Json)
                .FirstOrDefaultAsync(token);
            
            if (string.IsNullOrEmpty(json))
                return NotFound();
        }

        try
        {
            var configJsonNode = JsonSerializer.Deserialize<JsonObject>(json);
            if (configJsonNode?["includes"] is not JsonArray configIncludesNode) 
                return Content(json);
            
            var includesNames = configIncludesNode.Deserialize<string[]>() ?? Array.Empty<string>();
            var includes = await db.Configurations
                .Where(x => includesNames.Contains(x.Role) && x.Environment == configuration ||
                            x.Environment == null)
                .Select(x => x.Json)
                .ToArrayAsync(token);

            foreach (var includeJson in includes)
            {
                var includeNode = JsonSerializer.Deserialize<JsonElement>(includeJson);

                foreach (var prop in includeNode.EnumerateObject())
                {
                    if (configJsonNode[prop.Name] is JsonArray array && prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in prop.Value.EnumerateArray())
                            array.Add(item.Deserialize<JsonNode>());
                        continue;
                    }
                    
                    configJsonNode[prop.Name] = prop.Value.Deserialize<JsonNode>();
                }
            }
            
            configJsonNode.Remove("includes");
            var response = ConfigurationCache.Add(role, hasConfiguration ? configuration : null, configJsonNode.ToJsonString(), includes);
            return Ok(response);
        }
        catch (JsonException e)
        {
            return Problem(e.Message, $"{role}.{configuration}");
        }
    }

    /// <summary>
    /// Update configuration json
    /// </summary>
    [HttpPost("{role}.{configuration}")]
    public async Task<IActionResult> UpdateConfiguration([FromRoute] string role, [FromRoute] string configuration, 
        [FromBody] JsonElement json, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(configuration))
            return BadRequest();

        await using var db = await mContextFactory.CreateDbContextAsync(token);
        var updated = await db.Configurations.ExecuteUpdateAsync(x => x
            .SetProperty(p => p.UpdatedAt, DateTime.UtcNow)
            .SetProperty(p => p.Json, json.ToString()), token) > 0;

        if(updated)
        {
            ConfigurationCache.Invalidate(role, configuration);
            return Ok();
        }

        return NotFound();
    }
    
    /// <summary>
    /// Delete configuration
    /// </summary>
    [HttpDelete("{role}.{configuration}")]
    public async Task<IActionResult> DeleteConfiguration([FromRoute] string role, [FromRoute] string configuration,
        CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(configuration))
            return BadRequest();
        
        await using var db = await mContextFactory.CreateDbContextAsync(token);
        var config = new Database.Configuration {Role = role, Environment = configuration};
        db.Entry(config).State = EntityState.Deleted;
        var deleted = await db.SaveChangesAsync(token) > 0;
        
        if (!deleted) return NotFound();
        
        ConfigurationCache.Invalidate(role, configuration);
        return Ok();
    }
}