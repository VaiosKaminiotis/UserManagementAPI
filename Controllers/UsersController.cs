using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Models;

namespace UserManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private static readonly ConcurrentDictionary<int, User> Users = new(
        new[]
        {
            new KeyValuePair<int, User>(1, new User
            {
                Id = 1,
                FirstName = "Maria",
                LastName = "Papadopoulou",
                Email = "maria.papadopoulou@techhive.local",
                Department = "HR"
            }),
            new KeyValuePair<int, User>(2, new User
            {
                Id = 2,
                FirstName = "Nikos",
                LastName = "Georgiou",
                Email = "nikos.georgiou@techhive.local",
                Department = "IT"
            })
        });

    private static int _nextId = 2;
    private static readonly object WriteLock = new();

    [HttpGet]
    public ActionResult<IEnumerable<User>> GetUsers()
    {
        var users = Users.Values
            .OrderBy(user => user.Id)
            .ToArray();

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public ActionResult<User> GetUserById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "User ID must be greater than zero." });
        }

        if (!Users.TryGetValue(id, out var user))
        {
            return NotFound(new { message = $"User with ID {id} was not found." });
        }

        return Ok(user);
    }

    [HttpPost]
    public ActionResult<User> CreateUser([FromBody] UserRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        User newUser;

        lock (WriteLock)
        {
            if (Users.Values.Any(user =>
                    user.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)))
            {
                return Conflict(new { message = "A user with this email already exists." });
            }

            newUser = new User
            {
                Id = Interlocked.Increment(ref _nextId),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = normalizedEmail,
                Department = request.Department.Trim()
            };

            if (!Users.TryAdd(newUser.Id, newUser))
            {
                throw new InvalidOperationException("The generated user ID could not be added.");
            }
        }

        return CreatedAtAction(nameof(GetUserById), new { id = newUser.Id }, newUser);
    }

    [HttpPut("{id:int}")]
    public IActionResult UpdateUser(int id, [FromBody] UserRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "User ID must be greater than zero." });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        lock (WriteLock)
        {
            if (!Users.ContainsKey(id))
            {
                return NotFound(new { message = $"User with ID {id} was not found." });
            }

            var duplicateEmail = Users.Values.Any(user =>
                user.Id != id &&
                user.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));

            if (duplicateEmail)
            {
                return Conflict(new { message = "Another user already uses this email." });
            }

            Users[id] = new User
            {
                Id = id,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = normalizedEmail,
                Department = request.Department.Trim()
            };
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult DeleteUser(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "User ID must be greater than zero." });
        }

        if (!Users.TryRemove(id, out _))
        {
            return NotFound(new { message = $"User with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpGet("test-error")]
    public IActionResult TestErrorHandling()
    {
        throw new InvalidOperationException("Intentional test exception for Activity 3.");
    }
}
