﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaristaHome.Models
{
    public class RegisterViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(32), Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required, StringLength(32), Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required, EmailAddress, StringLength(64)]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,24}$",
            ErrorMessage = "Password must be between 8 and 24 characters and contain " +
            "one uppercase letter, one lowercase letter, one digit and one special character.")]
        public string Password { get; set; }

        [NotMapped, DataType(DataType.Password), Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Password and confirmation password not match.")]
        public string ConfirmPassword { get; set; }
    }
}
