using K4os.Compression.LZ4;

namespace AGame.Frontend.Stages;

/// <summary>
/// Step for LZ4 compression message
/// </summary>
internal class CompressionPipelineStage : PipelineStage
{
    /// <inheritdoc />
    protected override bool CanProcess(ReadOnlyMemory<byte> input, PipelineDirection direction) => 
        input.Length > 10;

    /// <inheritdoc />
    protected override int GetMaxOutputBufferSize(ReadOnlyMemory<byte> input, PipelineDirection direction)
    {
        if (direction == PipelineDirection.Receiving)
            return BitConverter.ToInt32(input[..4].Span);
        
        return LZ4Codec.MaximumOutputSize(input.Length) + 4;
    }

    /// <inheritdoc />
    protected override int Process(ReadOnlyMemory<byte> input, Span<byte> output, PipelineDirection direction)
    {
        switch (direction)
        {
            case PipelineDirection.Sending:
                var sizeBuffer = BitConverter.GetBytes(input.Length);
                output[0] = sizeBuffer[0];
                output[1] = sizeBuffer[1];
                output[2] = sizeBuffer[2];
                output[3] = sizeBuffer[3];
                return LZ4Codec.Encode(input.Span, output[4..]) + 4;
            case PipelineDirection.Receiving:
                return LZ4Codec.Decode(input[4..].Span, output);
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }
}