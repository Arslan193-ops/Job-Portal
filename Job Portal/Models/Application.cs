using System;
using System.Collections.Generic;

namespace Job_Portal.Models;

public partial class Application
{
    public int ApplicationId { get; set; }

    public int JobId { get; set; }

    public int UserId { get; set; }

    public DateTime AppliedDate { get; set; }

    public string? CvFilePath { get; set; }
    public virtual Job Job { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
