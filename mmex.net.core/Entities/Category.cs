namespace mmex.net.core.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Active { get; set; }
    /// <summary>-1 = root category (no parent).</summary>
    public int? ParentId { get; set; }

    // Navigation
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
}
