using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cwiczenia11.Context;
using Cwiczenia11.DTOs;
using Cwiczenia11.Helpers;
using Cwiczenia11.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Cwiczenia11.Services;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;
    private readonly Cw11Context _context;
    
    public DbService(IConfiguration configuration, Cw11Context context)
    {
        _configuration = configuration;
        _context = context;
    }
    
    
    public async Task RegisterPatient(RegisterRequest request)
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
    }
    
    public async Task<Tuple<string, string>> LoginPatient(LoginRequest request)
    {
        User user = await _context.Users.Where(u => u.Login == request.Login).FirstOrDefaultAsync();
        
        string passwordHashFromDb = user.Password;
        string curHashedPassword = SecurityHelper.GetHashedPasswordWithSalt(request.Password, user.Salt);
        
        if (user == null || passwordHashFromDb != curHashedPassword)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }
        
        Claim[] userclaim = new[]
        {
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, "admin")
        };
        
        SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]));
        SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        JwtSecurityToken token = new JwtSecurityToken(
            issuer: _configuration["JWT:Issuer"],
            audience: _configuration["JWT:Audience"],
            claims: userclaim,
            expires: DateTime.Now.AddMinutes(10), // // //
            signingCredentials: creds
        );
        
        user.RefreshToken = SecurityHelper.GenerateRefreshToken();
        user.RefreshTokenExpirationTime = DateTime.Now.AddDays(1);
        await _context.SaveChangesAsync();
        
        return new Tuple<string, string>(new JwtSecurityTokenHandler().WriteToken(token), user.RefreshToken);
    }
    
    public async Task<Tuple<string, string>> Refresh(RefreshTokenRequest refreshToken)
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
            new Claim(ClaimTypes.NameIdentifier, user.IdUser.ToString()),
            new Claim(ClaimTypes.Name, user.Login)
        };

        SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]));

        SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken jwtToken = new JwtSecurityToken(
            issuer: _configuration["JWT:Issuer"],
            audience: _configuration["JWT:Audience"],
            claims: userclaim,
            expires: DateTime.Now.AddMinutes(10), // // //
            signingCredentials: creds
        );

        user.RefreshToken = SecurityHelper.GenerateRefreshToken();
        user.RefreshTokenExpirationTime = DateTime.Now.AddDays(1);
        _context.SaveChanges();
        
        return new Tuple<string, string>(new JwtSecurityTokenHandler().WriteToken(jwtToken), user.RefreshToken);
    }
    
    public string GetPatients()
    {
        return "Secret data here you are";
    }
}