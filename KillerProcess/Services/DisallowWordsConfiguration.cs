using System.Text.Json;

namespace KillerProcess.Services;

public class DisallowWordsConfiguration
{
    public IList<string> DisallowWords { get; private set; }
    private readonly IConfiguration _configuration;

    private readonly string _url =
        "https://raw.githubusercontent.com/TheCrazyWolf/AntiGames/refs/heads/master/AntiGames/appsettings.json";

    public DisallowWordsConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
        DisallowWords = GetOnlineDisallowWords() ?? GetDefaultDisallowWords();
    }

    private IList<string>? GetOnlineDisallowWords()
    {
        using HttpClient client = new HttpClient();
        try
        {
            return TryDeserializeDisallowWordsOrGetNull(client.GetStringAsync(_url).GetAwaiter().GetResult());
        }
        catch
        {
            return null;
        }
    }

    private IList<string> GetDefaultDisallowWords()
    {
        return _configuration.GetSection("DisallowWords").Get<IList<string>>() ?? new List<string>();
    }

    private IList<string>? TryDeserializeDisallowWordsOrGetNull(string jsonContent)
    {
        try
        {
            return JsonSerializer.Deserialize<ResultDisallow>(jsonContent)?.DisallowWords;
        }
        catch
        {
            return null;
        }
    }

    internal class ResultDisallow
    {
        public List<string> DisallowWords { get; set; } = default!;
    }
}