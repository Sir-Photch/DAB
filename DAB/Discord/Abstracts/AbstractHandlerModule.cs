namespace DAB.Discord.Abstracts;

internal abstract class AbstractHandlerModule<T>
{
    internal T? Context { get; set; }

    internal abstract Task HandleAsync();
}
