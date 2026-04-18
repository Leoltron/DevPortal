namespace DevPortal.Models;

public record Link(string Title, string Url, string? Icon = null, string? IconColor = null, string? IconBg = null)
{
    public string? Icon { get; set; } = Icon;
}