namespace ArenaService.BackOffice.Options;

public class BasicAuthOptions
{
    public const string SectionName = "BasicAuth";
    
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
} 