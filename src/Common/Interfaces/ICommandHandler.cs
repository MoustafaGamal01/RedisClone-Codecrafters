internal interface ICommandHandler
{
    CommandsName CommandName { get; }

    Task Handle(NetworkStream stream, List<string> parts, ClientContext context);
}
