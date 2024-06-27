using Cwiczenia11.DTOs;

namespace Cwiczenia11.Services;

public interface IDbService
{
    public Task RegisterPatient(RegisterRequest request);

    public Task<Tuple<string, string>> LoginPatient(LoginRequest request);

    public Task<Tuple<string, string>> Refresh(RefreshTokenRequest refreshToken);

    public string GetPatients();
}