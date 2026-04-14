using AUtils.Sil;

namespace Fb.Frontend.Character;

[Sil(138)]
public struct CreateCharacterResultDto
{
    public bool IsSuccess { get; set; }

    public static readonly object Success = new CreateCharacterResultDto {IsSuccess = true};
    public static readonly object Fail = new CreateCharacterResultDto {IsSuccess = false};
}