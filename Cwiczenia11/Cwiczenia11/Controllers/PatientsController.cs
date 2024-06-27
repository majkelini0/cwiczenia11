using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cwiczenia11.Context;
using Cwiczenia11.DTOs;
using Cwiczenia11.Helpers;
using Cwiczenia11.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Cwiczenia11.Controllers;


[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly Cw11Context _context;
    
    public PatientsController(IConfiguration configuration, Cw11Context context)
    {
        _configuration = configuration;
        _context = context;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public IActionResult RegisterPatient(RegisterRequest request)
    {
        var hashedPasswordAndSalt = SecurityHelper.GetHashedPasswordAndSalt(request.Password);

        var user = new User()
        {
            Login = request.Login,
            Email = request.Email,
            Password = hashedPasswordAndSalt.Item1,
            Salt = hashedPasswordAndSalt.Item2,
            RefreshToken = SecurityHelper.GenerateRefreshToken(),
            RefreshTokenExpirationTime = DateTime.Now.AddDays(1)
        };
        
        _context.Users.Add(user);
        _context.SaveChanges();
        
        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult LoginPatient(LoginRequest request)
    {
        User user = _context.Users.Where(u => u.Login == request.Login).FirstOrDefault();
        
        string passwordHashFromDb = user.Password;
        string curHashedPassword = SecurityHelper.GetHashedPasswordWithSalt(request.Password, user.Salt);
        
        if (passwordHashFromDb != curHashedPassword)
        {
            return Unauthorized();
        }
        
        Claim[] userclaim = new[]
        {
            new Claim(ClaimTypes.Name, request.Login),
            new Claim(ClaimTypes.Role, "user")
        };
        
        SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]));
        SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        JwtSecurityToken token = new JwtSecurityToken(
            // ?! 
            issuer: _configuration["JWT:Issuer"],
            audience: _configuration["JWT:Audience"],
            claims: userclaim,
            expires: DateTime.Now.AddMinutes(1),
            signingCredentials: creds
        );
        
        user.RefreshToken = SecurityHelper.GenerateRefreshToken();
        user.RefreshTokenExpirationTime = DateTime.Now.AddDays(1);
        _context.SaveChanges();

        return Ok(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(token),
            refreshToken = user.RefreshToken
        });
    }
    
    [HttpPost("refresh")]
    [Authorize(AuthenticationSchemes = "IgnoreTokenExpirationScheme")]
    public IActionResult Refresh(RefreshTokenRequest refreshToken)
    {
        User user = _context.Users.Where(u => u.RefreshToken == refreshToken.RefreshToken).FirstOrDefault();
        if (user == null)
        {
            throw new SecurityTokenException("Invalid refresh token");
        }

        if (user.RefreshTokenExpirationTime < DateTime.Now)
        {
            throw new SecurityTokenException("Refresh token expired");
        }
        
        Claim[] userclaim = new[]
        {
            //new Claim(ClaimTypes.Name, request.Login),
            new Claim(ClaimTypes.Name, "user"),
            new Claim(ClaimTypes.Role, "user")
        };

        SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]));

        SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken jwtToken = new JwtSecurityToken(
            issuer: _configuration["JWT:Issuer"],
            audience: _configuration["JWT:Audience"],
            claims: userclaim,
            expires: DateTime.Now.AddMinutes(1),
            signingCredentials: creds
        );

        user.RefreshToken = SecurityHelper.GenerateRefreshToken();
        user.RefreshTokenExpirationTime = DateTime.Now.AddDays(1);
        _context.SaveChanges();

        return Ok(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken),
            refreshToken = user.RefreshToken
        });
    }
    
    [Authorize]
    [HttpGet]
    public IActionResult GetPatients()
    {
        var claimsFromAccessToken = User.Claims;
        return Ok("Secret data here you are");
    }
}