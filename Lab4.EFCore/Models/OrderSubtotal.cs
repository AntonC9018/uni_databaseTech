using System;
using System.Collections.Generic;

namespace Lab4.EFCore;

public sealed partial class OrderSubtotal
{
    public int OrderId { get; set; }

    public decimal? Subtotal { get; set; }
}
