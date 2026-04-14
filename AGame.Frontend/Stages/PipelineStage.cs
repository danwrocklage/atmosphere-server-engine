using System.Buffers;
using ACore.Abstractions.Transport;

namespace AGame.Frontend.Stages;

/// <summary>
/// Step in message processing 
/// </summary>
public abstract class PipelineStage
{
    /// <summary>
    /// Check that this pipeline stage should be processed
    /// </summary>
    protected abstract bool CanProcess(ReadOnlyMemory<byte> input, PipelineDirection direction);

    /// <summary>
    /// Receive a max length of buffer which need to process message in this stage
    /// </summary>
    protected abstract int GetMaxOutputBufferSize(ReadOnlyMemory<byte> input, PipelineDirection direction);

    /// <summary>
    /// Run message processing. Result needs to store in <paramref name="output"/>.
    ///
    /// DON'T INITIALIZE NEW BYTE ARRAY FOR <paramref name="output"/>
    /// </summary>
    protected abstract int Process(ReadOnlyMemory<byte> input, Span<byte> output, PipelineDirection direction);

    /// <summary>
    /// Run message processing with some internal memory stuff
    /// </summary>
    internal Packet InternalProcess(Packet input, PipelineDirection direction)
    {
        if (!CanProcess(input.Data, direction))
            return input;
        
        var maxSize = GetMaxOutputBufferSize(input.Data, direction);
        var output = MemoryPool<byte>.Shared.Rent(maxSize);

        var outputSize = Process(input.Data, output.Memory.Span, direction);
        input.Dispose();

        return outputSize < 1 ? Packet.Empty : new Packet(output).Slice(0, outputSize);
    }
}