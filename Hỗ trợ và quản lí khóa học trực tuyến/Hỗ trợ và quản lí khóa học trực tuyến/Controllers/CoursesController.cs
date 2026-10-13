using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data;

public class CoursesController : Controller
{
    private readonly NTULearnContext _context;

    public CoursesController(NTULearnContext context)
    {
        _context = context;
    }

    // GET: COURSES
    [AllowAnonymous]
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Courses.ToListAsync());
    }

    // GET: COURSES/Details/5
    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var course = await _context.Courses
            .Include(c => c.Category)
            .Include(c => c.Instructor)
            .FirstOrDefaultAsync(c => c.CourseId == id);

        if (course == null)
            return NotFound();

        return View(course);
    }

    // GET: COURSES/Create
    [Authorize(Roles = "Admin,GiangVien")]
    public IActionResult Create()
    {
        return View();
    }

    // POST: COURSES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.

    [Authorize(Roles = "Admin,GiangVien")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Title,Description,Price,ThumbnailUrl,CategoryId,InstructorId,Status")]
    Course course)
    {
        if (User.IsInRole("GiangVien"))
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int instructorId))
                return Forbid();

            course.InstructorId = instructorId;
        }

        course.CreatedAt = DateTime.Now;

        if (!ModelState.IsValid)
            return View(course);

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: COURSES/Edit/5
    [Authorize(Roles = "Admin,GiangVien")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound();
        }
        return View(course);
    }

    // POST: COURSES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [Authorize(Roles = "Admin,GiangVien")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("CourseId,Title,Description,Price,ThumbnailUrl,CategoryId,InstructorId,Status,CreatedAt,Category,Instructor")] Course course)
    {
        if (id == null || id != course.CourseId)
        {
            return NotFound();
        }

        // Lấy khóa học gốc từ database
        var existingCourse = await _context.Courses.FindAsync(id);

        if (existingCourse == null)
        {
            return NotFound();
        }

        // Kiểm tra quyền quản lý khóa học gốc
        if (!CanManageCourse(existingCourse))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(course);
        }

        existingCourse.Title = course.Title;
        existingCourse.Description = course.Description;
        existingCourse.Price = course.Price;
        existingCourse.ThumbnailUrl = course.ThumbnailUrl;
        existingCourse.CategoryId = course.CategoryId;
        existingCourse.Status = course.Status;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: COURSES/Delete/5

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.CourseId == id);

        if (course == null)
        {
            return NotFound();
        }

        // Kiểm tra quyền quản lý khóa học
        if (!CanManageCourse(course))
        {
            return Forbid();
        }

        return View(course);
    }

    // POST: COURSES/Delete/5
    [Authorize(Roles = "Admin,GiangVien")]  
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var course = await _context.Courses.FindAsync(id);

        if (course == null)
        {
            return NotFound();
        }

        // Kiểm tra quyền trước khi xóa
        if (!CanManageCourse(course))
        {
            return Forbid();
        }

        _context.Courses.Remove(course);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private bool CourseExists(int? id)
    {
        return _context.Courses.Any(e => e.CourseId == id);
    }

    private bool CanManageCourse(Course course)
    {
        if (User.IsInRole("Admin"))
            return true;

        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(userId, out int instructorId)
            && course.InstructorId == instructorId;
    }
}
