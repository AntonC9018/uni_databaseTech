using System;
using System.Collections.Generic;

namespace Lab4.EFCore;

public sealed partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public byte[]? Picture { get; set; }

    public List<Product> Products { get; set; } = new();
}
