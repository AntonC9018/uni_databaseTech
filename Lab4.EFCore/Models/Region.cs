using System;
using System.Collections.Generic;

namespace Lab4.EFCore;

public sealed partial class Region
{
    public int RegionId { get; set; }

    public string RegionDescription { get; set; } = null!;

    public List<Territory> Territories { get; set; } = new();
}
