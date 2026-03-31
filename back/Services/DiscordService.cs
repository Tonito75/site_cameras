namespace PortalCameras.Services;

public class DiscordService
{
    private readonly ILogger<DiscordService> _logger;
    private readonly string _webHookUrl;
    private readonly HttpClient _client = new();

    public DiscordService(IConfiguration config, ILogger<DiscordService> logger)
    {
        _logger = logger;
        _webHookUrl = config["WebHookUrl"] ?? string.Empty;
    }

    public async Task SendAsync(string message)
    {
        if (string.IsNullOrEmpty(_webHookUrl))
        {
            _logger.LogDebug("Discord — WebHookUrl non configurée, message ignoré : {Message}", message);
            return;
        }

        try
        {
            _logger.LogDebug("Discord — envoi : {Message}", message);
            var payload = System.Text.Json.JsonSerializer.Serialize(new { content = message });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(_webHookUrl, content);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Discord — réponse inattendue : {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError("Discord — exception : {Message}", ex.Message);
        }
    }
}
