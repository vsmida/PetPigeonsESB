namespace Shared
{
    public interface ICommandHandler<in T> :IMessageHandler<T> where T : ICommand
    {
      
    }
}