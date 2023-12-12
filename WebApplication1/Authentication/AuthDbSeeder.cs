using Microsoft.AspNetCore.Identity;
using WebApplication1.Authentication.Model;

namespace WebApplication1.Authentication
{
    public class AuthDbSeeder
    {
        private readonly UserManager<ForumRestUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthDbSeeder(UserManager<ForumRestUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            await AddDefaultRoles();
            await AddAdminUser();
        }

        private async Task AddDefaultRoles()
        {
            foreach (var role in ForumRoles.All)
            {
                var roleExists = await _roleManager.RoleExistsAsync(role);
                if (roleExists)
                    await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        private async Task AddAdminUser()
        {
            var newAdminUser = new ForumRestUser
            {
                UserName = "admin1",
                Email = "admin@admin.com"
            };

            var existingAdminUser = await _userManager.FindByNameAsync(newAdminUser.UserName);
            if (existingAdminUser == null)
            {
                var createAdminUserResult = await _userManager.CreateAsync(newAdminUser, "VerySafePassword1!");
                if (createAdminUserResult.Succeeded)
                {
                    foreach (var role in ForumRoles.All)
                    {
                        var roleExists = await _roleManager.RoleExistsAsync(role);
                        if (!roleExists)
                        {
                            await _roleManager.CreateAsync(new IdentityRole(role));
                        }
                    }
                    Console.WriteLine($"Roles Exist: {string.Join(", ", ForumRoles.All)}");



                    await _userManager.AddToRoleAsync(newAdminUser, ForumRoles.Admin);
                    var adminRoles = await _userManager.GetRolesAsync(newAdminUser);
                    Console.WriteLine($"Admin Roles: {string.Join(", ", adminRoles)}");

                }
                else
                {
                    foreach (var error in createAdminUserResult.Errors)
                    {
                        // Log or print the errors for debugging
                        Console.WriteLine($"Error: {error.Description}");
                    }
                }
            }
        }


    }
}
