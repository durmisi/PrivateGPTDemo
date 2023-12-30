namespace PrivateGPTDemo.Server.Tools
{
    public interface IChatMessageHandler
    {
        Task HandleMessage(string message, CancellationToken ct = default);
    }
}
