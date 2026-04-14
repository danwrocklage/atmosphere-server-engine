namespace AGame.Actors;

public class ActorException : ApplicationException
{
    public ActorException() {}
    public ActorException(string message) : base(message) {}
    public ActorException(string message, Exception innerException) : base(message, innerException) {}
}