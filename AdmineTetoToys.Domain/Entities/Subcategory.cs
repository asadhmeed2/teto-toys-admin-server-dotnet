namespace AdmineTetoToys.Domain.Entities;

public class Subcategory
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
}
