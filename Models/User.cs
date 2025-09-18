using System;
using System.ComponentModel.DataAnnotations;

public enum UserStatus { Active, Blocked, Deleted }

public class User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }

    public DateTime RegistrationTime { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginTime { get; set; } = DateTime.UtcNow;
    public UserStatus Status { get; set; } = UserStatus.Active;
}