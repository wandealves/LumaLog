using LumaLog.Models;

namespace LumaLog.Configuration;

/// <summary>
/// Configuration options for email notifications.
/// </summary>
public class EmailNotifierOptions
{
    /// <summary>
    /// Gets or sets the SMTP host.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP port.
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Gets or sets whether to use SSL.
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the sender email address.
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender display name.
    /// </summary>
    public string? FromName { get; set; }

    /// <summary>
    /// Gets or sets the recipient email addresses.
    /// </summary>
    public List<string> ToAddresses { get; set; } = new();

    /// <summary>
    /// Gets or sets the email subject template.
    /// </summary>
    public string SubjectTemplate { get; set; } = "[LumaLog] {Level}: {Message}";

    /// <summary>
    /// Gets or sets the minimum log level for notifications.
    /// </summary>
    public LogLevel MinLevel { get; set; } = LogLevel.Error;
}

/// <summary>
/// Configuration options for webhook notifications.
/// </summary>
public class WebhookNotifierOptions
{
    /// <summary>
    /// Gets or sets the webhook URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP method (POST, PUT).
    /// </summary>
    public string Method { get; set; } = "POST";

    /// <summary>
    /// Gets or sets custom headers to include.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the minimum log level for notifications.
    /// </summary>
    public LogLevel MinLevel { get; set; } = LogLevel.Error;

    /// <summary>
    /// Gets or sets whether to retry on failure.
    /// </summary>
    public bool RetryOnFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}
