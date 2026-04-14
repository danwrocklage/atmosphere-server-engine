namespace Fb.Mechanics.PlayerCharacter;

public interface ICharacterCreationService
{
    Task<bool> CanAccountCreateCharacter(Guid accountId, CancellationToken token = default);
    Task CreateCharacter(Guid accountId, string name, Dictionary<string, float> morphTargets);
    Task<bool> IsNameAlreadyUsed(string characterName);
    bool ValidateName(string characterName);
}