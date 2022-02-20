using Microsoft.AspNetCore.Identity;

namespace OpenIdDictDemo.Data
{
    public class ApplicationUser : IdentityUser
    {
        public int OfficeNumber { get; set; }
        public bool IsManager { get; set; }
        public bool IsAdministrator { get; set; }
    }
}
