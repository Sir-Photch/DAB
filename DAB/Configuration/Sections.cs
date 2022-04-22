using Newtonsoft.Json;

namespace DAB.Configuration;

internal class ConfigRoot
{
    [JsonProperty(nameof(DiscordParams))]
    internal DiscordParams Discord { get; set; } = new();

    [JsonProperty(nameof(BotParams))]
    internal BotParams Bot { get; set; } = new();
}

internal class DiscordParams
{
    [JsonProperty(nameof(ApiKey))]
    internal string ApiKey { get; set; } = string.Empty;

    [JsonProperty(nameof(AudioBufferMs))]
    internal int AudioBufferMs { get; set; } = 25;
}

internal class BotParams
{
    [JsonProperty(nameof(ChimeDurationMaxMs))]
    internal int ChimeDurationMaxMs { get; set; } = 10_000;

    [JsonProperty(nameof(ChimeFilesizeMaxKb))]
    internal int ChimeFilesizeMaxKb { get; set; } = 5_000;

    [JsonProperty(nameof(ChimePlaybackTimeoutMs))]
    internal int ChimePlaybackTimeoutMs { get; set; } = 10_000;
}
