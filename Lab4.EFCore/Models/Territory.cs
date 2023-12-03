using System;
using System.Collections.Generic;

namespace Lab4.EFCore;

public sealed partial class Territory
{
    public string TerritoryId { get; set; } = null!;

    public string TerritoryDescription { get; set; } = null!;

    public int RegionId { get; set; }

    public Region Region { get; set; } = null!;

    public List<Employee> Employees { get; set; } = new();
}
