﻿using System.Net.Http.Json;
using System.Text.Json;
using KillerProcess.Shared.Configs;

namespace KillerProcess.Services;

public class DisallowWordsConfiguration
{
    public ConfigurationResponse Configuration { get; private set; } = new ConfigurationResponse();
    private readonly IConfiguration _configuration;
    private ILogger<DisallowWordsConfiguration> _logger;
    private readonly string _url = "restrictions/getRestrictions";
    private readonly string _fileName = "restrictions.json";

    public DisallowWordsConfiguration(IConfiguration configuration, ILogger<DisallowWordsConfiguration> logger)
    {
        _configuration = configuration;
        _logger = logger;
        Task.Run(InitializeAsync);
    }

    public async Task InitializeAsync()
    {
        // Загружаем последний конфиг
        Configuration = await GetFromFile();

        // ожидаем когда все процессы компа очнутся
#if DEBUG
#else
        await Task.Delay(15000);
#endif

        // загружаем конфиг с сервера
        var actualConfig = await GetConfigFromUrl();

        if (actualConfig is null) return;

        Configuration = actualConfig;
        await SaveToFile(actualConfig);
    }

    private async Task SaveToFile(ConfigurationResponse actualConfig)
    {
        try
        {
            FileStream fileStream = new FileStream(_fileName, FileMode.OpenOrCreate, FileAccess.Write);
            await JsonSerializer.SerializeAsync(fileStream, actualConfig);
            fileStream.Close();
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }
    }

    private async Task<ConfigurationResponse?> GetConfigFromUrl()
    {
        using HttpClient client = new HttpClient();
        try
        {
            return await client.GetFromJsonAsync<ConfigurationResponse>($"{_configuration["UrlServer"]}/{_url}");
        }
        catch
        {
            return null;
        }
    }

    private async Task<ConfigurationResponse> GetFromFile()
    {
        try
        {
            FileStream fileStream = new FileStream(_fileName, FileMode.Open);
            return await JsonSerializer.DeserializeAsync<ConfigurationResponse>(fileStream) ??
                   new ConfigurationResponse();
        }
        catch
        {
            return new ConfigurationResponse();
        }
    }
}