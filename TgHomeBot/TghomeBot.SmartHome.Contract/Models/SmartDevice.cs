namespace TgHomeBot.SmartHome.Contract.Models;

public class SmartDevice
{
    public required string Id { get; set; }
    
    public required string Name { get; set; }

    public required string State { get; set; }
}