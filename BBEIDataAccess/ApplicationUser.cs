using Microsoft.AspNetCore.Identity;

namespace BBEIDataAccess
{
    public class ApplicationUser : IdentityUser
    {
        public override string? UserName { get; set; }
        public string? SecondaryEmailAddress { get; set; }
        public string? Name { get; set; }
    }

}