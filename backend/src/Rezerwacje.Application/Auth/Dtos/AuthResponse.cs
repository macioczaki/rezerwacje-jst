namespace Rezerwacje.Application.Auth.Dtos;

public record AuthResponse(string AccessToken, DateTime ExpiresAt, string Email, string Role);