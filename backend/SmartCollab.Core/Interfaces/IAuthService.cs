using SmartCollab.Core.DTOs;

namespace SmartCollab.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
    Task<bool> UserExistsAsync(string email);
    Guid GetCurrentUserId();
}