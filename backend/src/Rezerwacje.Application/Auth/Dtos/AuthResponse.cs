namespace Rezerwacje.Application.Auth.Dtos;

public record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    string Email,
    string Role
);