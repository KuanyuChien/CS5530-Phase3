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
    public class CommonController : Controller
    {
        private readonly LMSContext db;

        public CommonController(LMSContext _db)
        {
            db = _db;
        }

        /*******Begin code to modify********/

        /// <summary>
        /// Retreive a JSON array of all departments from the database.
        /// Each object in the array should have a field called "name" and "subject",
        /// where "name" is the department name and "subject" is the subject abbreviation.
        /// </summary>
        /// <returns>The JSON array</returns>
        public IActionResult GetDepartments()
        {
            var depts = from d in db.Departments
                        select new { name = d.Name, subject = d.Subject };
            return Json(depts.ToArray());
        }

        /// <summary>
        /// Returns a JSON array representing the course catalog.
        /// Each object in the array should have the following fields:
        /// "subject": The subject abbreviation, (e.g. "CS")
        /// "dname": The department name, as in "Computer Science"
        /// "courses": An array of JSON objects representing the courses in the department.
        ///            Each field in this inner-array should have the following fields:
        ///            "number": The course number (e.g. 5530)
        ///            "cname": The course name (e.g. "Database Systems")
        /// </summary>
        /// <returns>The JSON array</returns>
        public IActionResult GetCatalog()
        {
            var catalog = from d in db.Departments
                          select new
                          {
                              subject = d.Subject,
                              dname = d.Name,
                              courses = (from c in db.Courses
                                         where c.Department == d.Subject
                                         select new { number = c.Number, cname = c.Name }).ToArray()
                          };
            return Json(catalog.ToArray());
        }

        /// <summary>
        /// Returns a JSON array of all class offerings of a specific course.
        /// Each object in the array should have the following fields:
        /// "season": the season part of the semester, such as "Fall"
        /// "year": the year part of the semester
        /// "location": the location of the class
        /// "start": the start time in format "hh:mm:ss"
        /// "end": the end time in format "hh:mm:ss"
        /// "fname": the first name of the professor
        /// "lname": the last name of the professor
        /// </summary>
        /// <param name="subject">The subject abbreviation, as in "CS"</param>
        /// <param name="number">The course number, as in 5530</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetClassOfferings(string subject, int number)
        {
            var offerings = from co in db.Courses
                            where co.Department == subject && co.Number == (uint)number
                            join cl in db.Classes on co.CatalogId equals cl.Listing
                            join p in db.Professors on cl.TaughtBy equals p.UId into profs
                            from prof in profs.DefaultIfEmpty()
                            select new
                            {
                                season = cl.Season,
                                year = cl.Year,
                                location = cl.Location,
                                start = cl.StartTime.ToString("HH:mm:ss"),
                                end = cl.EndTime.ToString("HH:mm:ss"),
                                fname = prof == null ? "" : prof.FName,
                                lname = prof == null ? "" : prof.LName
                            };
            return Json(offerings.ToArray());
        }

        /// <summary>
        /// This method does NOT return JSON. It returns plain text (containing html).
        /// Use "return Content(...)" to return plain text.
        /// Returns the contents of an assignment.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The name of the assignment in the category</param>
        /// <returns>The assignment contents</returns>
        public IActionResult GetAssignmentContents(string subject, int num, string season, int year, string category, string asgname)
        {
            var query = from co in db.Courses
                        where co.Department == subject && co.Number == (uint)num
                        join cl in db.Classes on co.CatalogId equals cl.Listing
                        where cl.Season == season && cl.Year == (uint)year
                        join ac in db.AssignmentCategories on cl.ClassId equals ac.InClass
                        where ac.Name == category
                        join a in db.Assignments on ac.CategoryId equals a.Category
                        where a.Name == asgname
                        select a.Contents;
            string? contents = query.FirstOrDefault();
            return Content(contents == null ? "" : contents);
        }

        /// <summary>
        /// This method does NOT return JSON. It returns plain text (containing html).
        /// Use "return Content(...)" to return plain text.
        /// Returns the contents of an assignment submission.
        /// Returns the empty string ("") if there is no submission.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The name of the assignment in the category</param>
        /// <param name="uid">The uid of the student who submitted it</param>
        /// <returns>The submission text</returns>
        public IActionResult GetSubmissionText(string subject, int num, string season, int year, string category, string asgname, string uid)
        {
            var query = from co in db.Courses
                        where co.Department == subject && co.Number == (uint)num
                        join cl in db.Classes on co.CatalogId equals cl.Listing
                        where cl.Season == season && cl.Year == (uint)year
                        join ac in db.AssignmentCategories on cl.ClassId equals ac.InClass
                        where ac.Name == category
                        join a in db.Assignments on ac.CategoryId equals a.Category
                        where a.Name == asgname
                        join s in db.Submissions on new { A = a.AssignmentId, B = uid } equals new { A = s.Assignment, B = s.Student }
                        select s.SubmissionContents;
            string? content = query.FirstOrDefault();
            return Content(content == null ? "" : content);
        }


        /// <summary>
        /// Gets information about a user as a single JSON object.
        /// The object should have the following fields:
        /// "fname": the user's first name
        /// "lname": the user's last name
        /// "uid": the user's uid
        /// "department": (professors and students only) the name (such as "Computer Science") of the department for the user. 
        ///               If the user is a Professor, this is the department they work in.
        ///               If the user is a Student, this is the department they major in.    
        ///               If the user is an Administrator, this field is not present in the returned JSON
        /// </summary>
        /// <param name="uid">The ID of the user</param>
        /// <returns>
        /// The user JSON object 
        /// or an object containing {success: false} if the user doesn't exist
        /// </returns>
        public IActionResult GetUser(string uid)
        {
            var student = db.Students.FirstOrDefault(s => s.UId == uid);
            if (student != null)
            {
                var dept = db.Departments.FirstOrDefault(d => d.Subject == student.Major);
                return Json(new { fname = student.FName, lname = student.LName, uid = student.UId, department = dept!.Name });
            }

            var prof = db.Professors.FirstOrDefault(p => p.UId == uid);
            if (prof != null)
            {
                var dept = db.Departments.FirstOrDefault(d => d.Subject == prof.WorksIn);
                return Json(new { fname = prof.FName, lname = prof.LName, uid = prof.UId, department = dept!.Name });
            }

            var admin = db.Administrators.FirstOrDefault(a => a.UId == uid);
            if (admin != null)
                return Json(new { fname = admin.FName, lname = admin.LName, uid = admin.UId });

            return Json(new { success = false });
        }

        /*******End code to modify********/
    }
}

