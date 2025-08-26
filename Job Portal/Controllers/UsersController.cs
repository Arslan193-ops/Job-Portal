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
                return RedirectToAction("Login");
            }

            var jobs = _context.Jobs
                .Where(j => j.EmployerId == employer.UserId)
                .ToList();

            return View(jobs);
        }


        public IActionResult JobSeekerDashboard()
        {
            var jobs = _context.Jobs
                .Include(j => j.Employer)
                .ToList();

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
            HttpContext.Session.Clear();   
            return RedirectToAction("Login");
        }


    }
}
