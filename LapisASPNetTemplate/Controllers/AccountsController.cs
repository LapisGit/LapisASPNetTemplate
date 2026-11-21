using System.Text.RegularExpressions;
using LapisASPNetTemplate.Models;
using LapisASPNetTemplate.Models.Requests;
using LapisASPNetTemplate.Models.Responses;
using LapisASPNetTemplate.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LapisASPNetTemplate.Controllers;

[ApiController, Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly DBContext _db;
    private readonly AuthService _authService;

    public AccountsController(DBContext db, AuthService authService)
    {
        _db = db;
        _authService = authService;
    }
    
    [HttpPost("register")]
    [Consumes("application/json")]
    public async Task<ActionResult<string>> CreateAccount([FromBody] RegisterAccount request)
    {
        var userExists = await _db.Accounts.AnyAsync(a => a.AccountName == request.Username);
        if (userExists)
        {
            return BadRequest(new GenericResponse()
            {
                ErrorCode = 1,
                Message = "Username already exists."
            });
        }

        if (request.Username.Length < 3)
        {
            return BadRequest(new GenericResponse()
            {
                ErrorCode = 1,
                Message = "Username must be at least 3 characters long."
            });
        }
        
        if (request.Username.Length > 24)
        {
            return BadRequest(new GenericResponse()
            {
                ErrorCode = 1,
                Message = "Username cannot be longer than 24 characters."
            });
        }
        
        string pattern = @"^[a-zA-Z0-9]{3,20}$";

        if (!Regex.IsMatch(request.Username, pattern))
        {
            return BadRequest(new GenericResponse()
            {
                ErrorCode = 1,
                Message = "Username must not contain any special symbols."
            });
        }

        if (request.Password.Length < 8)
        {
            return BadRequest(new GenericResponse()
            {
                ErrorCode = 1,
                Message = "Password must be at least 8 characters long."
            });
        }
        
        Account account = new Account();
        account.AccountName = request.Username;
        account.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
        account.Email = request.Email;
        account.Bio = "";
        account.DateCreated = DateTime.UtcNow;
        
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();
        
        return Ok(new GenericResponse()
        {
            ErrorCode = 0,
            Message = "Success"
        });
    }
    
    [HttpPost("login")]
    [Consumes("application/json")]
    public async Task<ActionResult<string>> Login([FromBody] LoginAccount request)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountName == request.Username);

        if (account == null)
        {
            return Unauthorized(new { ErrorCode = 1, Message = "Invalid username or password." });
        }

        if (BCrypt.Net.BCrypt.Verify(request.Password, account.Password))
        {
            var tokenAcc = _authService.GenerateToken(accountId: account.AccountId.ToString(), platform: request.Platform);

            return Ok(new
            {
                token = tokenAcc,
                Message = "Success"
            });
        }
    
        return Unauthorized(new { ErrorCode = 1, Message = "Invalid username or password." });
    }
}
