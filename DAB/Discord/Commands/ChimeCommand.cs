using DAB.Configuration;
using DAB.Data.Interfaces;
using DAB.Discord.Enums;
using Discord;

namespace DAB.Discord.Commands;

internal class ChimeCommand
{
    private readonly string? _chimeSourceUrlRaw = null;
    private Uri? _chimeSourceUrl = null;

    internal enum Mode { Clear, Set }

    internal Mode RequestedMode { get; }

    internal SlashCommand Context { get; }    

#pragma warning disable format
    internal ChimeCommand(SlashCommand cmd)
    {
        if (cmd.CommandType is not SlashCommandType.chime)
            throw new ArgumentException($"expected {SlashCommandType.chime}", nameof(cmd));

        Context = cmd;

        var clearOrSet = Context.Data.Options.First();

        switch (clearOrSet)
        {
            case { Name: "clear", Options.Count: 0 }: {
                    RequestedMode = Mode.Clear;
                } break;

            case { Name: "set", Options.Count: 1 }: {
                    RequestedMode = Mode.Set;

                    object source = clearOrSet.Options.First().Options.First().Value;

                    _chimeSourceUrlRaw = source switch
                    {
                        IAttachment fileAttachment => fileAttachment.Url,
                        string url => url,
                        _ => throw new ArgumentException("Unknown value!")
                    };

                } break;

            default:
                throw new ArgumentException("parsing error", nameof(cmd));
        }
    }
#pragma warning restore format

    internal async Task<bool> ValidateRequestedModeAsync()
    {
        if (RequestedMode is Mode.Clear)
        {
            var sink = GlobalServiceProvider.GetService<IUserDataSink>();

            await sink.ClearAsync(Context.User.Id);

            await Context.FollowupAsync("Success! I won't greet you anymore :(");
            return true;
        }

        await Context.FollowupAsync("Thinking...", ephemeral: true);

        if (!Uri.TryCreate(_chimeSourceUrlRaw, UriKind.Absolute, out _chimeSourceUrl))
        {
            Log.Write(WRN, "User requested bad url {url}", _chimeSourceUrlRaw);
            await Context.FollowupAsync("Bad url!", ephemeral: true);
            return false;
        }

        var httpClient = GlobalServiceProvider.GetService<HttpClient>();
        var maxSizeB = GlobalServiceProvider.GetService<ConfigRoot>().Bot.ChimeFilesizeMaxKb * 1_000L;

        /* https://stackoverflow.com/a/24350338/15884439 */

        var response = await httpClient.GetAsync(_chimeSourceUrl, HttpCompletionOption.ResponseHeadersRead);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException hre)
        {
            Log.Write(ERR, hre, "Request for headers of {url} failed", _chimeSourceUrl);
            await Context.FollowupAsync("Hm, this url does not work for me!", ephemeral: true);
            return false;
        }

        var contentLengthStr = response.Content.Headers.SingleOrDefault(h => h.Key.Equals("Content-Length")).Value?.FirstOrDefault();

        if (!int.TryParse(contentLengthStr, out int contentLength))
        {
            Log.Write(ERR, "Could not determine filesize of {uri} | {headers}", _chimeSourceUrl, response.Content.Headers);
            await Context.FollowupAsync("Hm, I could not determine the filesize!", ephemeral: true);
            return false;
        }

        if (contentLength > maxSizeB)
        {
            await Context.FollowupAsync("Whoa, this is a big file! Too big for me!", ephemeral: true);
            return false;
        }

        return true;
    }

    internal async Task<Stream> GetAudioFileStreamAsync()
    {
        if (RequestedMode is Mode.Clear || _chimeSourceUrl is null)
            throw new InvalidOperationException();

        var httpClient = GlobalServiceProvider.GetService<HttpClient>();
        return await httpClient.GetStreamAsync(_chimeSourceUrl);
    }

    internal static SlashCommandProperties CreateCommand()
    {
        // this is why I hate this super-verbose oop-stuff x(
        return new SlashCommandBuilder().WithName(nameof(SlashCommandType.chime))
                                        .WithDescription("modify your chime")
                                        .AddOptions(new SlashCommandOptionBuilder().WithName("clear")
                                                                                   .WithDescription("clear your chime")
                                                                                   .WithType(ApplicationCommandOptionType.SubCommand),
                                                    new SlashCommandOptionBuilder().WithName("set")
                                                                                   .WithDescription("set your chime")
                                                                                   .WithType(ApplicationCommandOptionType.SubCommandGroup)
                                                                                   .AddOption(new SlashCommandOptionBuilder().WithName("url")
                                                                                                                             .WithDescription("set your chime with an url")
                                                                                                                             .WithType(ApplicationCommandOptionType.SubCommand)
                                                                                                                             .AddOption(new SlashCommandOptionBuilder().WithName("link")
                                                                                                                                                                       .WithDescription("link to audio-file")
                                                                                                                                                                       .WithType(ApplicationCommandOptionType.String)
                                                                                                                                                                       .WithRequired(true)))
                                                                                   .AddOption(new SlashCommandOptionBuilder().WithName("file")
                                                                                                                             .WithDescription("set your chime with a file")
                                                                                                                             .WithType(ApplicationCommandOptionType.SubCommand)
                                                                                                                             .AddOption(new SlashCommandOptionBuilder().WithName("attachment")
                                                                                                                                                                       .WithDescription("file with audio")
                                                                                                                                                                       .WithType(ApplicationCommandOptionType.Attachment)
                                                                                                                                                                       .WithRequired(true))))
                                        .Build();
    }

}
