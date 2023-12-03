using System;
using System.Collections.Generic;

namespace Lab4.EFCore;

public sealed partial class SummaryOfSalesByYear
{
    public DateTime? ShippedDate { get; set; }

    public int OrderId { get; set; }

    public decimal? Subtotal { get; set; }
}
