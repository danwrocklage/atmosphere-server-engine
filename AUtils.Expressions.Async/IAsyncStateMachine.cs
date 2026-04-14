using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace AUtils.Expressions.Async;

/// <summary>
/// This interface here for design purposes only
/// to ensure that state machine class is written correctly.
/// </summary>
/// <typeparam name="TState">Type of internal state.</typeparam>
internal interface IAsyncStateMachine<out TState> : IAsyncStateMachine
{
    /// <summary>
    /// Represents final state identifier of async state machine.
    /// </summary>
    internal const uint FINAL_STATE = 0U;

    TState State { get; }

    uint StateId { get; }

    bool MoveNext<TAwaiter>(ref TAwaiter awaiter, uint stateId)
        where TAwaiter : INotifyCompletion;

    void Rethrow();

    bool HasNoException { get; }

    void EnterGuardedCode(uint newState);

    void ExitGuardedCode(uint previousState, bool suspendException);

    bool TryRecover<TException>([NotNullWhen(true)] out TException? exception)
        where TException : Exception;
}