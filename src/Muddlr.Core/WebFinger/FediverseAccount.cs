namespace Muddlr.WebFinger;

public record FediverseAccount(string FediverseServer, string Username)
{
    public string Key { get; } = $"{FediverseServer.Replace(".", "_dot_")}_{Username}";
}

public class FediverseAccountWithLocators
{
    public List<string> Locators { get; set; } = new List<string>();
    public FediverseAccount Account { get; init; }
}