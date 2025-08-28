using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Job_Portal.Models;

namespace Job_Portal.Controllers
{
    public class JobsController : Controller
    {
        private readonly JobPortalContext _context;

        public JobsController(JobPortalContext context)
        {
            _context = context;
        }

        // GET: Jobs
        public async Task<IActionResult> Index()
        {
            var jobPortalContext = _context.Jobs
                .Include(j => j.Employer)
                .Include(j => j.Applications)      // load applications
                    .ThenInclude(a => a.User);     // load the applicant user
            foreach (var job in jobPortalContext)
            {
                Console.WriteLine($"{job.Title} has {job.Applications.Count} applications");
            }
            return View(await jobPortalContext.ToListAsync());
        }



        // GET: Jobs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.Jobs
                .Include(j => j.Applications)
                    .ThenInclude(a => a.User)   
                .Include(j => j.Employer)
                .FirstOrDefaultAsync(m => m.JobId == id);

            if (job == null) return NotFound();

            return View(job);
        }


        // GET: Jobs/Create (Employers only)
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Employer") return Unauthorized();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Location")] Job job)
        {
            if (!ModelState.IsValid) return View(job);

            var email = HttpContext.Session.GetString("UserEmail");
            var employer = _context.Users.FirstOrDefault(u => u.Email == email);

            if (employer == null || employer.Role != "Employer") return Unauthorized();

            job.EmployerId = employer.UserId;
            job.PostedDate = DateTime.Now;

            _context.Add(job);
            await _context.SaveChangesAsync();

            // Redirect employer to their jobs, not global Index
            return RedirectToAction("EmployerDashboard","Users");
        }

        // GET: Jobs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return NotFound();

            // Security: Only employer who posted it can edit
            var email = HttpContext.Session.GetString("UserEmail");
            var employer = _context.Users.FirstOrDefault(u => u.Email == email);
            if (employer == null || job.EmployerId != employer.UserId) return Unauthorized();

            return View(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("JobId,Title,Description,Location")] Job job)
        {
            if (id != job.JobId) return NotFound();

            var email = HttpContext.Session.GetString("UserEmail");
            var employer = _context.Users.FirstOrDefault(u => u.Email == email);
            if (employer == null) return Unauthorized();

            var existingJob = await _context.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.JobId == id);
            if (existingJob == null || existingJob.EmployerId != employer.UserId) return Unauthorized();

            if (ModelState.IsValid)
            {
                try
                {
                    job.EmployerId = employer.UserId;
                    job.PostedDate = existingJob.PostedDate;

                    _context.Update(job);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JobExists(job.JobId)) return NotFound();
                    else throw;
                }
                return RedirectToAction("EmployerDashBoard","Users");
            }
            return View(job);
        }


        // GET: Jobs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.Jobs
                .Include(j => j.Employer)
                .FirstOrDefaultAsync(m => m.JobId == id);

            if (job == null) return NotFound();

            var email = HttpContext.Session.GetString("UserEmail");
            var employer = _context.Users.FirstOrDefault(u => u.Email == email);
            if (employer == null || job.EmployerId != employer.UserId) return Unauthorized();

            return View(job);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var job = await _context.Jobs.FindAsync(id);

            if (job != null)
            {
                var email = HttpContext.Session.GetString("UserEmail");
                var employer = _context.Users.FirstOrDefault(u => u.Email == email);
                if (employer == null || job.EmployerId != employer.UserId) return Unauthorized();

                _context.Jobs.Remove(job);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(MyJobs));
        }

        private bool JobExists(int id)
        {
            return _context.Jobs.Any(e => e.JobId == id);
        }

        // Employer’s jobs
        public IActionResult MyJobs()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            var employer = _context.Users.FirstOrDefault(u => u.Email == email);

            if (employer == null || employer.Role != "Employer") return Unauthorized();

            var jobs = _context.Jobs
                .Where(j => j.EmployerId == employer.UserId)
                .ToList();

            return View(jobs);
        }

        // Jobseeker: browse all jobs
        //public IActionResult BrowseJobs()
        //{
        //    var role = HttpContext.Session.GetString("UserRole");
        //    if (role != "Jobseeker") return Unauthorized();

        //    var jobs = _context.Jobs
        //        .Include(j => j.Employer) // so seekers can see who posted
        //        .ToList();

        //    return View(jobs);
        //}

        public async Task<IActionResult> Applicants(int jobId)
        {
            // Get current employer from session
            var email = HttpContext.Session.GetString("UserEmail");
            var employer = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (employer == null || employer.Role != "Employer")
                return Unauthorized();

            // Get job with applications and applicants
            var job = await _context.Jobs
                .Include(j => j.Applications)
                    .ThenInclude(a => a.User) // applicant info
                .FirstOrDefaultAsync(j => j.JobId == jobId && j.EmployerId == employer.UserId);

            if (job == null)
                return NotFound();

            return View(job);
        }


    }
}
