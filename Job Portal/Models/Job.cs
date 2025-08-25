using System;
using System.Collections.Generic;

namespace Job_Portal.Models;

public partial class Job
{
    public int JobId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Location { get; set; } = null!;

    public DateTime PostedDate { get; set; }

    public int EmployerId { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual User Employer { get; set; } = null!;
}
