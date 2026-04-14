using AUtils.Sil;

namespace AGame.Frontend.Dto;

[Sil(104)]
internal class InitializeConnectionDto
{
    public string Jwt { get; set; }
    
    public byte[] PublicKey { get; set; }
    
    public string ApplicationVersion { get; set; }
}