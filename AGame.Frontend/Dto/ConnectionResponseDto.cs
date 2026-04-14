using AUtils.Sil;

namespace AGame.Frontend.Dto;

[Sil(105)]
internal class ConnectionResponseDto
{
    public bool IsError { get; set; }
    
    public string Message { get; set; }
}