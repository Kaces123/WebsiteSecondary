using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Authentication.Model
{
    public class ForumRestUser : IdentityUser
    {
        public bool ForceRelogin { get; set; }

    }
}
