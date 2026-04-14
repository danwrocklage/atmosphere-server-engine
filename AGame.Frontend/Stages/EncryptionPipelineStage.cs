using ACore.Abstractions.Transport;

namespace AGame.Frontend.Stages;

/// <summary>
/// Step for AES encrypt/decrypt message
/// </summary>
internal class EncryptionPipelineStage : PipelineStage
{
    private readonly DiffieHellman mAlgorithm;

    public EncryptionPipelineStage(DiffieHellman algorithm)
    {
        mAlgorithm = algorithm;
    }

    /// <inheritdoc />
    protected override bool CanProcess(ReadOnlyMemory<byte> input, PipelineDirection direction) => true;

    /// <inheritdoc />
    protected override int GetMaxOutputBufferSize(ReadOnlyMemory<byte> input, PipelineDirection direction) =>
        mAlgorithm.GetCiphertextLength(input.Length);

    /// <inheritdoc />
    protected override int Process(ReadOnlyMemory<byte> input, Span<byte> output, PipelineDirection direction) =>
        direction switch
        {
            PipelineDirection.Sending => mAlgorithm.Encrypt(input.Span, output),
            PipelineDirection.Receiving => mAlgorithm.Decrypt(input.Span, output),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
}
