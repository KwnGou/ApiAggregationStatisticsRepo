using System;
using System.Collections.Generic;

namespace ApiAggregation.Models;

public partial class Statistic
{
    public int Id { get; set; }

    public string Api { get; set; } = null!;

    public int ResponseFast { get; set; }

    public int RespsonseAverage { get; set; }

    public int ResponseSlow { get; set; }

    public DateOnly RequestDate { get; set; }

    public int TimedOut { get; set; }

    public int Cached { get; set; }

    public int Failed { get; set; }
}
