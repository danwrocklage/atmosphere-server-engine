namespace ACore.VisualScript;

public interface INodeProcessor
{
    void Run(NodeContext context);
}
    
public interface IAsyncNodeProcessor
{
    Task RunAsync(NodeContext context);
}