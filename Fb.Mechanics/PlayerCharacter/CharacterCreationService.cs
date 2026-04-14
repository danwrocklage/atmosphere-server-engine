using System.Text;
using ACore.Abstractions;
using ACore.Abstractions.Database;
using ACore.Abstractions.Logging;
using AGame.Core.Account;
using AGame.Core.Journal;
using Fb.Mechanics.Stats;

namespace Fb.Mechanics.PlayerCharacter;

public class CharacterCreationService : ICharacterCreationService
{
    private const int DEFAULT_MAX_CHAR_NAME_LEN = 30;
    
    private readonly IRepository<CharacterEntity> mCharacterRepository;
    private readonly IJournalService mJournalService;
    private readonly ILogger<CharacterCreationService> mLogger;
    private readonly IDatabase mDatabase;
    private readonly int mCharacterNameMaxLen;

    public CharacterCreationService(ILogger<CharacterCreationService> logger, 
        IConfiguration configuration, IDatabase database, IJournalService journalService)
    {
        mLogger = logger;
        mDatabase = database;
        mJournalService = journalService;
        mCharacterNameMaxLen = configuration.Get("CharacterNameLen", () => DEFAULT_MAX_CHAR_NAME_LEN);
        mCharacterRepository = database.Repository<CharacterEntity>();
    }

    public async Task<bool> CanAccountCreateCharacter(Guid accountId, CancellationToken token = default)
    {
        var currentCount = await mCharacterRepository.Select()
            .CountAsync(x => x.AccountId == accountId, token);

        var maxCount = await mDatabase.Select<AccountEntity>()
            .Where(x => x.Id == accountId)
            .Select(x => x.CharacterMaxCount)
            .FirstOrDefaultAsync(token);

        return currentCount < maxCount;
    }

    public async Task CreateCharacter(Guid accountId, string name, Dictionary<string, float> morphTargets)
    {
        if (name == null) 
            throw new ArgumentNullException(nameof(name));
        
        if (morphTargets == null) 
            throw new ArgumentNullException(nameof(morphTargets));
        
        mLogger.Info($"Create new character '{name}' for account: {accountId}");
        
        var characterId = Guid.NewGuid();
        
        var entity = CreateDefaultCharacter();
        entity.AccountId = accountId;
        entity.Name = name;
        entity.MorphTargets = morphTargets.Select(x =>
        {
            var builder = new StringBuilder();
            builder.Append(x.Key);
            builder.Append('_');
            builder.Append(x.Value);
            return builder.ToString();
        }).ToArray();

        await mDatabase.Repository<CharacterEntity>().Insert(entity);
        
        await mJournalService.Write($"A character '{name}' for account was created", nameof(CharacterEntity), 
            JournalLink.Create<CharacterEntity>(characterId), 
            JournalLink.Create<AccountEntity>(accountId));
    }

    private CharacterEntity CreateDefaultCharacter()
    {
        return new CharacterEntity
        {
            Mesh = GetDefaultMesh(),
            Stats = GetDefaultStats(),
            Position = GetDefaultPosition(),
            CreatedAt = DateTime.UtcNow,
            LastSeenOnline = null,
            IsOnline = false
        };
    }

    private Dictionary<StatType,int> GetDefaultStats()
    {
        return new Dictionary<StatType, int>();
    }

    private string GetDefaultMesh()
    {
        return "";
    }

    private float[] GetDefaultPosition()
    {
        return new [] {Random.Shared.NextSingle() * 1000, Random.Shared.NextSingle() * 1000, Random.Shared.NextSingle() * 1000};
    }

    public async Task<bool> IsNameAlreadyUsed(string characterName)
    {
        if (string.IsNullOrEmpty(characterName)) 
            throw new ArgumentNullException(nameof(characterName));

        return await mDatabase.Select<CharacterEntity>()
            .AnyAsync(x => x.Name == characterName);
    }

    public bool ValidateName(string characterName)
    {
        if (characterName == null) throw new ArgumentNullException(nameof(characterName));
        return 
            characterName.Length <= mCharacterNameMaxLen && 
            characterName.All(c => char.IsLetterOrDigit(c));
    }
}