using Microsoft.Extensions.Configuration;

namespace DAB.Util;

internal static class Config
{
    internal const string CONFIG_FILENAME = "config.json";

    internal class DiscordKeys // waiting for records to be supported...
    {
        public string? ApiKey { get; init; }
    }

    /// <exception cref="InvalidOperationException"/>
    internal static SectionType Get<SectionType>() where SectionType : class
        => _config.GetRequiredSection(typeof(SectionType).Name).Get<SectionType>();

    #region private

    private static readonly IConfiguration _config;

    private static readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILENAME);

    static Config()
    {
        _config = new ConfigurationBuilder()
                     .AddJsonFile(_configPath, optional: true)
                     .Build();
    }

    #endregion
}
