#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models;

[Table("Users")]
public class User
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public bool HasCompletedOnboarding { get; set; } = false;
    public int ItemsPerPage { get; set; } = 10;
    public bool IsDarkMode { get; set; } = false;

    // Lưu trạng thái phiên làm việc cuối
    public bool RememberLastSession { get; set; } = true;
    public string? LastVisitedPage { get; set; }
}