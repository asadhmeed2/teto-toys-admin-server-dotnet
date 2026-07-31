namespace AdmineTetoToys.Application.DTOs;

// ponytail: keep DTO models simple, clean, and direct
public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Language { get; set; }
}

/// <summary>
/// Edits a category's name for one language. Language defaults to 'en'.
/// The slug is not editable — it is a stable URL identifier.
/// </summary>
public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Language { get; set; }
}

public class CreateSubcategoryRequest
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Language { get; set; }
}
