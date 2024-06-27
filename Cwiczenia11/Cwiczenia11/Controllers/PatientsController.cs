using Cwiczenia11.DTOs;
using Cwiczenia11.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Cwiczenia11.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IDbService _service;

    public PatientsController(IDbService service)
    {
        _service = service;
    }


    [AllowAnonymous]
    [HttpPost("register")]
    public IActionResult RegisterPatient(RegisterRequest request)
    {
        _service.RegisterPatient(request);

        return Ok("User registered");
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> LoginPatient(LoginRequest request)
    {
        var token = await _service.LoginPatient(request);

        return Ok(new
        {
            accesToken = token.Item1,
            refreshToken = token.Item2
        });
    }
    
    
    [HttpPost("refresh")]
    [Authorize(AuthenticationSchemes = "IgnoreTokenExpirationScheme")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest refreshToken)
    {
        var token = await _service.Refresh(refreshToken);

        return Ok(new
            {
                accesToken = token.Item1,
                refreshToken = token.Item2
            });
    }

    [Authorize]
    [HttpGet]
    public IActionResult GetPatients()
    {
        //var claimsFromAccessToken = User.Claims;

        return Ok(_service.GetPatients());
    }
}