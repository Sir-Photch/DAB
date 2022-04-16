namespace DAB.Discord.Commands;

[AttributeUsage(AttributeTargets.Field)]
internal class SlashCommandDetailsAttribute : Attribute
{
    public string CommandName { get; }

    public string Description { get; }

    internal SlashCommandDetailsAttribute(string command, string description) 
    { 
        CommandName = command;
        Description = description;
    }
}
