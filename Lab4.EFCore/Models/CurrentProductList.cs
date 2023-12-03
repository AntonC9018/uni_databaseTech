using System;
using System.Collections.Generic;

namespace Lab4.EFCore;

public sealed partial class CurrentProductList
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;
}
