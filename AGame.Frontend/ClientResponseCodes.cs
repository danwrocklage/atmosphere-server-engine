using System.Diagnostics.CodeAnalysis;

namespace AGame.Frontend;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
internal static class ClientResponseCodes
{
    public static readonly string MaxConnectionsExceeded = "MAX.CONNECTION.EXCEEDED";
    public static readonly string MaxPlayersExceeded = "MAX.PLAYERS.EXCEEDED";
    public static readonly string AppVersionNotSupported = "APP.VERSION.NOTSUPPORTED";
    public static readonly string Unauthorized = "CLIENT.UNAUTHORIZED";
    public static readonly string InvalidConnectionData = "INVALID.CONNECTION.DATA";
}