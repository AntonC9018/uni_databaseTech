using System;
using System.Collections.Generic;

namespace Lab4.EFCore;

public sealed partial class ProductsAboveAveragePrice
{
    public string ProductName { get; set; } = null!;

    public decimal? UnitPrice { get; set; }
}
