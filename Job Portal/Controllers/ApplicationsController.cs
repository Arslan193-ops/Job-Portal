using Job_Portal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Job_Portal.Controllers
{
    public class ApplicationsController : Controller
    {
        private readonly JobPortalContext _context;
        private readonly IWebHostEnvironment _environment;

        public ApplicationsController(JobPortalContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Applications
        public async Task<IActionResult> Index()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                return Unauthorized();
            }

            IQueryable<Application> applications = _context.Applications
                .Include(a => a.Job)
                .Include(a => a.User);

            if (user.Role == "Employer")
            {
                // Show applications only for jobs posted by this employer
                applications = applications.Where(a => a.Job.EmployerId == user.UserId);
            }
            else if (user.Role == "JobSeeker")
            {
                // Show only this user's applications
                applications = applications.Where(a => a.UserId == user.UserId);
            }
            else if (user.Role == "Admin")
            {
                // Admin sees all applications (no filter)
            }

            return View(await applications.ToListAsync());
        }


        // GET: Applications/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var application = await _context.Applications
                .Include(a => a.Job)
                .ThenInclude(j => j.Employer)
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.ApplicationId == id);

            if (application == null) return NotFound();

            return View(application);
        }

        // GET: Applications/Create
        // GET: Applications/Create
        public IActionResult Create(int jobId)
        {
            var job = _context.Jobs.FirstOrDefault(j => j.JobId == jobId);
            if (job == null)
            {
                return NotFound();
            }

            ViewData["JobTitle"] = job.Title;
            ViewData["JobId"] = job.JobId;

            return View();
        }

        [HttpPost, ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateConfirmed(int jobId, IFormFile CvFile)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null || user.Role != "JobSeeker")
            {
                return Unauthorized();
            }

            // ✅ Check if this user already applied for this job
            var existingApp = await _context.Applications
                .FirstOrDefaultAsync(a => a.JobId == jobId && a.UserId == user.UserId);

            if (existingApp != null)
            {
                TempData["Error"] = "You have already applied for this job.";
                return RedirectToAction("Details", "Jobs", new { id = jobId });
            }

            string cvPath = null;

            // ✅ Handle CV Upload
            if (CvFile != null && CvFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads/cvs");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(CvFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await CvFile.CopyToAsync(stream);
                }

                // store relative path in DB
                cvPath = "/uploads/cvs/" + uniqueFileName;
            }

            var application = new Application
            {
                JobId = jobId,
                UserId = user.UserId,
                AppliedDate = DateTime.Now,
                CvFilePath = cvPath
            };

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Application submitted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Applications/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var application = await _context.Applications.FindAsync(id);
            if (application == null) return NotFound();

            var email = HttpContext.Session.GetString("UserEmail");
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null) return Unauthorized();

            // Only allow job seekers to edit their own application
            if (user.Role == "JobSeeker" && application.UserId != user.UserId)
            {
                return Unauthorized();
            }

            ViewData["JobId"] = new SelectList(_context.Jobs, "JobId", "Title", application.JobId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Email", application.UserId);
            return View(application);
        }

        // POST: Applications/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ApplicationId,JobId,UserId,AppliedDate")] Application application)
        {
            if (id != application.ApplicationId) return NotFound();

            var email = HttpContext.Session.GetString("UserEmail");
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null) return Unauthorized();
            // Restrict editing to the owner only
            if (user.Role == "JobSeeker" && application.UserId != user.UserId)
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(application);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Application updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ApplicationExists(application.ApplicationId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            return View(application);
        }

        // GET: Applications/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var application = await _context.Applications
                .Include(a => a.Job)
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.ApplicationId == id);

            if (application == null) return NotFound();

            var email = HttpContext.Session.GetString("UserEmail");
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null) return NotFound();
            // Only allow job seekers to delete their own applications
            if (user.Role == "JobSeeker" && application.UserId != user.UserId)
            {
                return Unauthorized();
            }

            return View(application);
        }

        // POST: Applications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var application = await _context.Applications.FindAsync(id);

            if (application != null)
            {
                var email = HttpContext.Session.GetString("UserEmail");
                var user = _context.Users.FirstOrDefault(u => u.Email == email);

                if (user == null) return NotFound();
                if (user.Role == "JobSeeker" && application.UserId != user.UserId)
                {
                    return Unauthorized();
                }

                _context.Applications.Remove(application);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Application deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ApplicationExists(int id)
        {
            return _context.Applications.Any(e => e.ApplicationId == id);
        }
    }
}
