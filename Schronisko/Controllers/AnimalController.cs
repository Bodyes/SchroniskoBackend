using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schronisko.Data;

namespace Schronisko.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AnimalController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AnimalController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Animal>>> GetAnimals()
        {
            var animals = await _context.Animal
            .Include(a => a.User)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Description,
                a.Age,
                a.UserId,
                Username = a.User != null ? a.User.UserName : null,
                a.CreatedAt,
                a.Adopted
            })
            .ToListAsync();

            return Ok(animals);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Animal>> GetAnimal(int id)
        {
            
           var animal = await _context.Animal
                .Include(a => a.User)
                .Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.Description,
                    a.Age,
                    a.UserId,
                    a.CreatedAt,
                    a.Adopted,
                    username = a.User != null ? a.User.UserName: null // Include the user information
                })
                .FirstOrDefaultAsync(a => a.Id == id);

            if (animal == null)
                return NotFound();

            return Ok(animal);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] // Only admins can add animals
        public async Task<ActionResult<Animal>> PostAnimal([Bind("Name,Description,Age,UserId,Adopted")] Animal animal)
        {
            animal.CreatedAt = DateTime.UtcNow; // Set CreatedAt to current UTC time

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Opcjonalnie: Pobierz użytkownika na podstawie UserId
            var user = await _context.Users.FindAsync(animal.UserId);
            if (user == null)
            {
                return BadRequest("Invalid UserId.");
            }

            // Konwersja Age na UTC
            if (animal.Age.Kind == DateTimeKind.Unspecified)
            {
                animal.Age = DateTime.SpecifyKind(animal.Age, DateTimeKind.Utc);
            }
            else if (animal.Age.Kind != DateTimeKind.Utc)
            {
                animal.Age = animal.Age.ToUniversalTime();
            }

            _context.Animal.Add(animal);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAnimal", new { id = animal.Id }, new
            {
                animal.Id,
                animal.Name,
                animal.Description,
                animal.Age,
                animal.UserId,
                Username = user.UserName, // tylko nazwa użytkownika
                animal.CreatedAt,
                animal.Adopted
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> PutAnimal(int id, [Bind("Id,Name,Description,Age,UserId,Adopted")] Animal animal)
        {

            if (id != animal.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Opcjonalnie: Pobierz użytkownika na podstawie UserId
            var user = await _context.Users.FindAsync(animal.UserId);
            if (user == null)
            {
                return BadRequest("Invalid UserId.");
            }

            var existingAnimal = await _context.Animal.FindAsync(id);
            if (existingAnimal == null)
            {
                return NotFound();
            }

            // Only allow the original author or admin to update
            if (existingAnimal.UserId != User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value &&
                !User.IsInRole("Admin") && !User.IsInRole("Moderator"))
            {
                return Forbid();
            }

            existingAnimal.Name = animal.Name;
            existingAnimal.Description = animal.Description;
            existingAnimal.Age = animal.Age;
            existingAnimal.Adopted = animal.Adopted;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AnimalExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok("Update success.");
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Only admins can delete animals
        public async Task<IActionResult> DeleteAnimal(int id)
        {
            var animal = await _context.Animal.FindAsync(id);
            if (animal == null)
            {
                return NotFound();
            }

            _context.Animal.Remove(animal);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{id}/adopt")]
        [Authorize(Roles = "Admin,Moderator")]   
        public async Task<IActionResult> AdoptAnimal(int id)
        {
            var animal = await _context.Animal.FindAsync(id);
            if (animal == null)
            {
                return NotFound();
            }

            animal.Adopted = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AnimalExists(int id)
        {
            return _context.Animal.Any(e => e.Id == id);
        }
    }
}
