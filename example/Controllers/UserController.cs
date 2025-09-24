using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("controller/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public UsersController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /controller/users
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<User>), StatusCodes.Status200OK)]
    [EnableQuery<User>(maxTop: 10)]
    public ActionResult<IEnumerable<User>> Get()
    {
        var users = _db.Users
            .Include(x => x.Company)
            .Include(x => x.Addresses)
                .ThenInclude(x => x.City)
            .Include(x => x.Manager)
                .ThenInclude(x => x.Manager)
            .Where(x => !x.IsDeleted);

        return Ok(users);
    }
}