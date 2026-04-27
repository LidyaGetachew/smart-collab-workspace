using SmartCollab.Core.DTOs;

namespace SmartCollab.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    Task<UserProfileDto?> GetUserProfileAsync(Guid userId);
    Guid GetCurrentUserId();
    Task<bool> UserExistsAsync(string email);
}