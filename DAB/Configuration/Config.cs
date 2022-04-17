using DAB.Configuration.Exceptions;
using Newtonsoft.Json;

namespace DAB.Configuration;

internal class Config
{
    internal static string Path { get; } = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    internal static void EnsureConfigExists(bool @throw = false)
    {
        bool configExists = File.Exists(Path);

        if (!configExists)
        {
            using TextWriter writer = File.CreateText(Path);

            ConfigRoot defaultConfig = new()
            {
                Bot = new(),
                Discord = new()
            };

            JsonSerializer.CreateDefault(_settings).Serialize(writer, defaultConfig);

            if (@throw)
                throw new NotConfiguredException("Default config was created");
        }
    }

    internal static ConfigRoot GetRoot()
    {
        ConfigRoot? retval = null;
        try
        {
            retval = JsonConvert.DeserializeObject<ConfigRoot>(File.ReadAllText(Path), _settings);
        }
        catch (Exception e)
        {
            throw new NotConfiguredException("Could not deserialize config!", e);
        }

        if (retval is null) throw new NotConfiguredException("Deserialization returned null!");

        return retval;
    }

    private static readonly JsonSerializerSettings _settings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Include,
        DefaultValueHandling = DefaultValueHandling.Populate
    };
}
