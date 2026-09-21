using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Models;

public class UserRequest
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be in a valid format.")]
    [StringLength(120, ErrorMessage = "Email cannot be longer than 120 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Department must be between 2 and 80 characters.")]
    public string Department { get; set; } = string.Empty;
}
