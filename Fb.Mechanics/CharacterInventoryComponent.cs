using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;
using AGame.Actors;
using Fb.Mechanics.PlayerCharacter;

namespace Fb.Mechanics;

[Table("character.inventory")]
public class CharacterInventory : IDbEntity
{
    public Guid Id { get; set; }
    
    public Guid CharacterId { get; set; }
    
    public short Size { get; set; }
    
    public Item[] Items { get; set; }

    public class Item
    {
        public Guid ItemId { get; set; }
        
        public byte Count { get; set; }
    }
}

public class CharacterInventoryComponent : ActorComponent
{
    private readonly IDatabase mDatabase;

    public CharacterInventoryComponent(IDatabase database)
    {
        mDatabase = database;
    }

    protected override async Task Attach()
    {
        if (Owner is PlayerCharacterActor player)
        {
            
        }
    }
}