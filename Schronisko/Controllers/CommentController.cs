using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schronisko.Data;

namespace Schronisko.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CommentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CommentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Comment>>> GetComments()
        {
            var comments = await _context.Comment
                .Include(c => c.User)
                .Select(c => new
                {
                    c.Id,
                    c.Body,
                    c.UserId,
                    Username = c.User != null ? c.User.UserName : null,
                    c.PostId,
                    c.Status,
                    c.CreatedAt
                })
                .ToListAsync();
            return Ok(comments);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Comment>> GetComment(int id)
        {
            var comment = await _context.Comment.Include(c => c.User)
                .Select(c => new
                {
                    c.Id,
                    c.Body,
                    c.UserId,
                    Username = c.User != null ? c.User.UserName : null,
                    c.PostId,
                    c.Status,
                    c.CreatedAt
                })
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
            {
                return NotFound();
            }

            return Ok(comment);
        }

        [HttpPost]
        public async Task<ActionResult<Comment>> PostComment([Bind("Body,UserId,Status, PostId")] Comment comment)
        {
            comment.CreatedAt = DateTime.UtcNow;

            // Validate the post exists
            var post = await _context.Post.FindAsync(comment.PostId);
            if (post == null)
            {
                return BadRequest("Invalid PostId.");
            }

            var user = await _context.Users.FindAsync(comment.UserId);
            if (user == null)
            {
                return BadRequest("Invalid UserId.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Comment.Add(comment);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetComment", new { id = comment.Id }, new { 
                comment.Id,
                comment.Body,
                comment.UserId,
                Username = user.UserName,
                comment.PostId,
                comment.Status,
                comment.CreatedAt
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutComment(int id, [Bind("Body,UserId,Status, PostId")] Comment comment)
        {
            if (id != comment.Id)
            {
                return BadRequest();
            }

            var existingComment = await _context.Comment.FindAsync(id);
            if (existingComment == null)
            {
                return NotFound();
            }

            // Only allow the original author or admin to update
            if (existingComment.UserId != User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value &&
                !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            existingComment.Body = comment.Body;
            existingComment.Status = comment.Status;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CommentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comment.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            // Only allow the original author or admin to delete
            if (comment.UserId != User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value &&
                !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            _context.Comment.Remove(comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CommentExists(int id)
        {
            return _context.Comment.Any(e => e.Id == id);
        }
    }
}
