using AUtils.Configuration.Host.Database;
using AUtils.Configuration.Host.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AUtils.Configuration.Host.Api;

/// <summary>
///     Manage users
/// </summary>
[ApiController]
[Route("/api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> mContextFactory;

    /// <summary>
    /// .ctor
    /// </summary>
    public UserController(IDbContextFactory<AppDbContext> contextFactory)
    {
        mContextFactory = contextFactory;
    }

    /// <summary>
    /// Get users list
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken token = default)
    {
        await using var db = await mContextFactory.CreateDbContextAsync(token);
        var users = await db.Users
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Type,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToArrayAsync(token);

        return Ok(users);
    }

    /// <summary>
    /// Create new user
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] EditUser model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(model?.Name))
            return BadRequest();
        
        await using var db = await mContextFactory.CreateDbContextAsync(cancellationToken);
        var alreadyExisted = await db.Users.AnyAsync(x => x.Name == model.Name && x.Type == model.Type, cancellationToken);
        if (alreadyExisted)
            return BadRequest();
        
        var token = BCrypt.Net.BCrypt.EnhancedHashPassword(Guid.NewGuid().ToString());
        var id = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = id,
            Name = model.Name,
            Type = model.Type,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            Token = token,
            Id = id
        });
    }
    
    /// <summary>
    /// Update user
    /// </summary>
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateUser([FromRoute] Guid id, [FromBody] EditUser? model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(model?.Name))
            return BadRequest();
        
        await using var db = await mContextFactory.CreateDbContextAsync(cancellationToken);
        var alreadyExisted = await db.Users.AnyAsync(x => x.Name == model.Name && x.Type == model.Type, cancellationToken);
        if (alreadyExisted)
            return BadRequest();

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user == null)
            return NotFound();
        
        user.Name = model.Name;
        user.Type = model.Type;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Ok();
    }
    
    /// <summary>
    /// Delete user
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await mContextFactory.CreateDbContextAsync(cancellationToken);
     
        db.Users.Remove(new User {Id = id});
        var deleted = await db.SaveChangesAsync(cancellationToken) > 0;
        return deleted ? Ok() : NotFound();
    }
}