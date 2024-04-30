using Hospital_Booking_Page_BackEnd.Data;
using Hospital_Booking_Page_BackEnd.Models;
using Hospital_Booking_Page_BackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hospital_Booking_Page_BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;


        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            return Ok(await _context.Categories.ToListAsync());
        }

        [HttpGet("count-duplicates/{name}")]
        public async Task<IActionResult> CountCategoryDuplicates(string name)
        {

            int count = await _context.Categories
                                      .Where(c => c.Name == name)
                                      .CountAsync(); 
            if (count > 0)
            {
                return Ok(new { CategoryName = name, DuplicateCount = count });
            }
            else
            {
                return Ok(new { CategoryName = name, Message = "No duplicates found." });
            }
        }




        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();
            return Ok(category);
        }

        [HttpPost]
        public async Task<IActionResult> PostCategory(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetCategory", new { id = category.Id }, category);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategory(int id, Category category)
        {
            if (id != category.Id)
                return BadRequest();

            _context.Entry(category).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            //var category = await _context.Categories.FindAsync(id);

            /* var category = await _context.Categories
                 .Include(c => c.Doctor)
                 .FirstOrDefaultAsync(c => c.Id == id);*/

            var category = await _context.Categories.FindAsync(id);

            if (category == null)
                return NotFound();

         /*  if (category.Doctor.Any())
                return BadRequest("Category cannot be deleted because it is in use by doctors.");*/

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }


     

    }
}
