using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace AGame.Frontend;

/// <summary>
/// Cross platform wrapper on a Diffie-Hellman algorithm without using streams
/// </summary>
internal class DiffieHellman : IDisposable
{
    private readonly ECDiffieHellman mAlgorithm;
    private readonly Aes mAes;

    public DiffieHellman()
    {
        mAes = Aes.Create();
        mAes.Mode = CipherMode.CBC;
        mAes.Padding = PaddingMode.PKCS7;
        
        mAlgorithm = Create();
    }
    
    public ICryptoTransform Encryptor { get; private set; }
    public ICryptoTransform Decryptor { get; private set; }
    
    /// <summary>
    /// Salt
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public byte[] IV { get; set; }

    /// <summary>
    /// Export public information as byte array
    /// </summary>
    public byte[] Export() => mAlgorithm.ExportSubjectPublicKeyInfo();

    /// <summary>
    /// Import other public information
    /// </summary>
    public void Import(byte[] subjectPublicKeyInfo)
    {
        var dh = Create();
        dh.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out _);
        
        mAes.Key = mAlgorithm.DeriveKeyMaterial(dh.PublicKey);
        if (IV != null)
            mAes.IV = IV;
        IV = mAes.IV;
        Decryptor = mAes.CreateDecryptor();
        Encryptor = mAes.CreateEncryptor();
    }

    public int GetCiphertextLength(int plaintextLength) => 
        mAes.GetCiphertextLengthCbc(plaintextLength, mAes.Padding);

    /// <summary>
    /// Encrypt byte message
    /// </summary>
    public int Encrypt(ReadOnlySpan<byte> input, Span<byte> output) => 
        !mAes.TryEncryptCbc(input, IV, output, out var written, mAes.Padding) ? 0 : written;

    /// <summary>
    /// Decrypt byte message
    /// </summary>
    public int Decrypt(ReadOnlySpan<byte> input, Span<byte> output) =>
        !mAes.TryDecryptCbc(input, IV, output, out var written, mAes.Padding) ? 0 : written;
    
    /// <summary>
    /// Encrypt byte message
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is null</exception>
    /// <exception cref="InvalidOperationException"><see cref="Import"/> isn't setup</exception>
    public byte[] Encrypt(byte[] message)
    {
        if (message == null) 
            throw new ArgumentNullException(nameof(message));
        
        if (Encryptor == null)
            throw new InvalidOperationException();
        
        return Encryptor.TransformFinalBlock(message, 0, message.Length);
    }

    /// <summary>
    /// Decrypt byte message
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="encryptedMessage"/> is null</exception>
    /// <exception cref="InvalidOperationException"><see cref="Import"/> isn't setup</exception>
    public byte[] Decrypt(byte[] encryptedMessage)
    {
        if (encryptedMessage == null) 
            throw new ArgumentNullException(nameof(encryptedMessage));
        
        if (Encryptor == null)
            throw new InvalidOperationException();
        
        return Decryptor.TransformFinalBlock(encryptedMessage, 0, encryptedMessage.Length);
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        mAlgorithm.Dispose();
    }
    
    private static ECDiffieHellman Create() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new ECDiffieHellmanCng
            {
                KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash,
                HashAlgorithm = CngAlgorithm.Sha256
            }
            : new ECDiffieHellmanOpenSsl();
}