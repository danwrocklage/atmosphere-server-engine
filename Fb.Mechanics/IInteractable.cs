namespace Fb.Mechanics;

public interface IInteractable
{
    Task Interact(Guid senderId);
}