using ACore.Abstractions.Rpc;

namespace ACore.Tests.Shared;

internal class FakeRpc : IRpc
{
    public Task<TReply> Call<TRequest, TReply>(TRequest request, CancellationToken token = default)
    {
        return Task.FromResult<TReply>(default);
    }

    public Task<TReply> Call<TRequest, TReply>(string topic, TRequest request, CancellationToken token = default)
    {
        return Task.FromResult<TReply>(default);
    }

    public Task Call<TRequest>(TRequest request, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }

    public Task Call<TRequest>(string topic, TRequest request, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }
}