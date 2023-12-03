using System;
using System.Collections.Generic;

namespace Lab4.EFCore;

public sealed partial class Shipper
{
    public int ShipperId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string? Phone { get; set; }

    public List<Order> Orders { get; set; } = new();
}
