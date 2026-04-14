using AUtils.Sil;

namespace AGame.Frontend.Dto;

[Sil(107)]
internal class CompleteConnectionDto
{
    public byte[] PublicKey { get; set; }
    
    public byte[] IV { get; set; }
}