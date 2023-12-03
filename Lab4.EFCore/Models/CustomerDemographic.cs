using System;
using System.Collections.Generic;

namespace Lab4.EFCore;

public sealed partial class CustomerDemographic
{
    public string CustomerTypeId { get; set; } = null!;

    public string? CustomerDesc { get; set; }

    public List<Customer> Customers { get; set; } = new();
}
