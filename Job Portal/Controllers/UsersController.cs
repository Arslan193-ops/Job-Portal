using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Job_Portal.Models;
using Microsoft.AspNetCore.Http;

namespace Job_Portal.Controllers
{
    public class UsersController : Controller
    {
        private readonly JobPortalContext _context;

        public UsersController(JobPortalContext context)
        {
            _context = context;
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            if (ModelState.IsValid)
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["msg"] = "Registration successful!";
                return RedirectToAction("Login");
            }
            return View(user);
        }


        public IActionResult Login()
        {
            // Check if user is already logged in
            var email = HttpContext.Session.GetString("UserEmail");
            var role = HttpContext.Session.GetString("UserRole");

            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(role))
            {
                if (role == "Employer")
                    return RedirectToAction("EmployerDashboard", "Users");
                else if (role == "JobSeeker")
                    return RedirectToAction("JobSeekerDashboard", "Users");
            }

            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Email == email && u.Password == password);

            if (user != null)
            {
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserRole", user.Role);

                if (user.Role == "Employer")
                    return RedirectToAction("EmployerDashboard", "Users");
                else
                    return RedirectToAction("JobSeekerDashboard", "Users");
            }

            ViewBag.Error = "Invalid email or password";
            return View();
        }


        public IActionResult EmployerDashboard()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            var employer = _context.Users.FirstOrDefault(u => u.Email == email);

            if (employer == null)
            {
                return RedirectToAction("Login", "Users");
            }

            var jobs = _context.Jobs
                .Where(j => j.EmployerId == employer.UserId)
                .ToList();

            return View(jobs);
        }



        public async Task<IActionResult> JobSeekerDashboard()
        {
            var jobs = await _context.Jobs
    .Include(j => j.Applications)
        .ThenInclude(a => a.User)
    .ToListAsync();

            return View(jobs);

        }


        public IActionResult Dashboard()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (email == null)
            {
                return RedirectToAction("Login");
            }

            ViewBag.Email = email;
            return View();
        }

        public IActionResult Logout()
        {
            // Clear session
            HttpContext.Session.Clear();

            // Prevent cached pages after logout
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToAction("Login");
        }



    }
}
