namespace SmartHome.Domain.Entities;

public class Room
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid UserId { get; set; } // A room belongs to the user
    public void Rename(string? newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("Room name cannot be empty.");
        }
        Name = newName;
    }
}