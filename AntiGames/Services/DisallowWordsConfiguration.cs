namespace AntiGames.Services;

public class DisallowWordsConfiguration(IConfiguration configuration)
{
    public IList<string> DisallowWords { get; } = configuration.GetSection("DisallowWords").Get<IList<string>>() ?? new List<string>();
}