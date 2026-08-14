using Microsoft.AspNetCore.Mvc;
using Application.Services;
using Domain.Models;
using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        User user = _userService.CreateUser(request.Email, request.Name, request.Password);
        UserResponse response = new UserResponse { Id = user.Id, Email = user.Email, Name = user.Name };
        return Ok(response);
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        User user = _userService.Login(request.Email, request.Password);
        UserResponse response = new UserResponse { Id = user.Id, Email = user.Email, Name = user.Name };
        return Ok(response);
    }
}