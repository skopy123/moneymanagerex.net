namespace mmex.net.core.Entities;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Active { get; set; }

    // Navigation
    public ICollection<TagLink> TagLinks { get; set; } = new List<TagLink>();
}
