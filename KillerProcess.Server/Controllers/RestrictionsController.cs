using KillerProcess.Shared.Configs;
using Microsoft.AspNetCore.Mvc;

namespace KillerProcess.Server.Controllers;

[ApiController]
[Route("restrictions")]
public class RestrictionsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RestrictionsController> _logger;

    public RestrictionsController(ILogger<RestrictionsController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet("getRestrictions")]
    public ConfigurationResponse Get()
    {
        var response = new ConfigurationResponse
        {
            DisallowedWords = _configuration.GetSection("ConfigWatcher:DisallowWords").Get<List<string>>() ?? new List<string>(),
            DisallowedProcesses = _configuration.GetSection("ConfigWatcher:DisallowProcesses").Get<List<string>>() ?? new List<string>(),
            RestrictedUsers = _configuration.GetSection("ConfigWatcher:RestrictedNTUsers").Get<List<string>>() ?? new List<string>(),
        };

        return response;
    }
}