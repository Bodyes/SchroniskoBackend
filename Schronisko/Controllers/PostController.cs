using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schronisko.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http.HttpResults;


namespace Schronisko.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PostController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PostController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Post>>> GetPosts()
        {
            var posts = await _context.Post
                .Include(p => p.User)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Body,
                    p.UserId,
                    Username = p.User != null ? p.User.UserName : null,
                    p.Status,
                    p.CreatedAt
                })
                .ToListAsync();
            return Ok(posts);
        }

        
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Post>> GetPost(int id)
        {
            var post = await _context.Post
                .Include(p => p.User)
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Body,
                    p.UserId,
                    Username = p.User != null ? p.User.UserName : null,
                    p.Status,
                    p.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (post == null)
                return NotFound();

            return Ok(post);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult<Post>> PostPost([Bind("Id,Title,Body,UserId,Status")] Post post)
        {
            post.CreatedAt = DateTime.UtcNow;
            var user = await _context.Users.FindAsync(post.UserId);
            if (user == null)
            {
                return BadRequest("Invalid UserId.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Post.Add(post);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPost", new { id = post.Id }, new
            {
                post.Id,
                post.Title,
                post.Body,
                post.UserId,
                Username = user.UserName,
                post.Status,
                post.CreatedAt
            });
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> PutPost(int id, [Bind("Id,Title,Body,UserId,Status")] Post post)
        {

            if (id != post.Id)
            {
                return BadRequest();
            }

            var existingPost = await _context.Post.FindAsync(id);
            if (existingPost == null)
            {
                return NotFound();
            }

            // Only allow the original author or admin to update
            if (existingPost.UserId != User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value &&
                !User.IsInRole("Admin") && !User.IsInRole("Moderator"))
            {
                return Forbid();
            }

            existingPost.Title = post.Title;
            existingPost.Body = post.Body;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PostExists(id))
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
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _context.Post.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            // Only allow the admin to delete
            if (!User.IsInRole("Admin") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            _context.Post.Remove(post);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{id}/comments")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Comment>>> GetPostComments(int id)
        {
            var comments =  await _context.Comment
                .Where(c => c.PostId == id)
                .Include(c => c.User)
                .Select(c => new
                {
                    c.Id,
                    c.Body,
                    c.UserId,
                    UserName = c.User != null ? c.User.UserName : null,
                    c.PostId,
                    c.Status,
                    c.CreatedAt
                })
                .ToListAsync();
            return Ok(comments);
        }
        private bool PostExists(int id)
        {
            return _context.Post.Any(e => e.Id == id);
        }
    }
}
