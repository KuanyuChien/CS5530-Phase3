using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using LMS.Models.LMSModels;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
[assembly: InternalsVisibleTo( "LMSControllerTests" )]
namespace LMS.Controllers
{
    public class AdministratorController : Controller
    {
        private readonly LMSContext db;

        public AdministratorController(LMSContext _db)
        {
            db = _db;
        }

        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Department(string subject)
        {
            ViewData["subject"] = subject;
            return View();
        }

        public IActionResult Course(string subject, string num)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            return View();
        }

        /*******Begin code to modify********/

        /// <summary>
        /// Create a department which is uniquely identified by it's subject code
        /// </summary>
        /// <param name="subject">the subject code</param>
        /// <param name="name">the full name of the department</param>
        /// <returns>A JSON object containing {success = true/false}.
        /// false if the department already exists, true otherwise.</returns>
        public IActionResult CreateDepartment(string subject, string name)
        {
            bool exists = db.Departments.Any(d => d.Subject == subject);
            if (exists)
                return Json(new { success = false });

            db.Departments.Add(new Department { Subject = subject, Name = name });
            db.SaveChanges();
            return Json(new { success = true });
        }


        /// <summary>
        /// Returns a JSON array of all the courses in the given department.
        /// Each object in the array should have the following fields:
        /// "number" - The course number (as in 5530)
        /// "name" - The course name (as in "Database Systems")
        /// </summary>
        /// <param name="subjCode">The department subject abbreviation (as in "CS")</param>
        /// <returns>The JSON result</returns>
        public IActionResult GetCourses(string subject)
        {
            var courses = from c in db.Courses
                          where c.Department == subject
                          select new { number = c.Number, name = c.Name };
            return Json(courses.ToArray());
        }

        /// <summary>
        /// Returns a JSON array of all the professors working in a given department.
        /// Each object in the array should have the following fields:
        /// "lname" - The professor's last name
        /// "fname" - The professor's first name
        /// "uid" - The professor's uid
        /// </summary>
        /// <param name="subject">The department subject abbreviation</param>
        /// <returns>The JSON result</returns>
        public IActionResult GetProfessors(string subject)
        {
            var professors = from p in db.Professors
                             where p.WorksIn == subject
                             select new { lname = p.LName, fname = p.FName, uid = p.UId };
            return Json(professors.ToArray());
        }



        /// <summary>
        /// Creates a course.
        /// A course is uniquely identified by its number + the subject to which it belongs
        /// </summary>
        /// <param name="subject">The subject abbreviation for the department in which the course will be added</param>
        /// <param name="number">The course number</param>
        /// <param name="name">The course name</param>
        /// <returns>A JSON object containing {success = true/false}.
        /// false if the course already exists, true otherwise.</returns>
        public IActionResult CreateCourse(string subject, int number, string name)
        {
            bool exists = db.Courses.Any(c => c.Department == subject && c.Number == (uint)number);
            if (exists)
                return Json(new { success = false });

            db.Courses.Add(new Course { Number = (uint)number, Name = name, Department = subject });
            db.SaveChanges();
            return Json(new { success = true });
        }



        /// <summary>
        /// Creates a class offering of a given course.
        /// </summary>
        /// <param name="subject">The department subject abbreviation</param>
        /// <param name="number">The course number</param>
        /// <param name="season">The season part of the semester</param>
        /// <param name="year">The year part of the semester</param>
        /// <param name="start">The start time</param>
        /// <param name="end">The end time</param>
        /// <param name="location">The location</param>
        /// <param name="instructor">The uid of the professor</param>
        /// <returns>A JSON object containing {success = true/false}. 
        /// false if another class occupies the same location during any time 
        /// within the start-end range in the same semester, or if there is already
        /// a Class offering of the same Course in the same Semester,
        /// true otherwise.</returns>
        public IActionResult CreateClass(string subject, int number, string season, int year, DateTime start, DateTime end, string location, string instructor)
        {
            // Find the course to get its CatalogId
            Course? course = db.Courses.FirstOrDefault(c => c.Department == subject && c.Number == (uint)number);
            if (course == null)
                return Json(new { success = false });

            // Check: same course already has a class in this semester
            bool sameOffering = db.Classes.Any(cl =>
                cl.Listing == course.CatalogId &&
                cl.Season == season &&
                cl.Year == (uint)year);
            if (sameOffering)
                return Json(new { success = false });

            TimeOnly startTime = TimeOnly.FromDateTime(start);
            TimeOnly endTime   = TimeOnly.FromDateTime(end);

            // Check: location conflict (any class in same location/semester with overlapping time)
            bool locationConflict = db.Classes.Any(cl =>
                cl.Location == location &&
                cl.Season == season &&
                cl.Year == (uint)year &&
                cl.StartTime < endTime &&
                cl.EndTime > startTime);
            if (locationConflict)
                return Json(new { success = false });

            db.Classes.Add(new Class
            {
                Season    = season,
                Year      = (uint)year,
                Location  = location,
                StartTime = startTime,
                EndTime   = endTime,
                Listing   = course.CatalogId,
                TaughtBy  = instructor
            });
            db.SaveChanges();
            return Json(new { success = true });
        }


        /*******End code to modify********/

    }
}

