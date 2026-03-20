namespace TriStrike.Models;

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Score { get; set; }

    public Player(int id, string name)
    {
        Id = id;
        Name = name;
        Score = 0;
    }
}
