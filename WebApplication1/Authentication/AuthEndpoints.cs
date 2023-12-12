using Microsoft.AspNetCore.Identity;
using WebApplication1.Authentication.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebApplication1.Authentication.Model;
using WebApplication1.Authentication;

namespace WebApplication1.Authentication
{
    public static class AuthEndpoints
    {
        public static void AddAuthApi(this WebApplication app)
        {
            // register
            app.MapPost("api/register", async (UserManager<ForumRestUser> userManager, RoleManager<IdentityRole> roleManager, RegisterUserDto registerUserDto) =>
            {
                var roleExists = await roleManager.RoleExistsAsync(ForumRoles.ForumUser);
                if (!roleExists)
                {
                    var createRoleResult = await roleManager.CreateAsync(new IdentityRole(ForumRoles.ForumUser));
                    if (!createRoleResult.Succeeded)
                    {
                        // Handle role creation failure
                        return Results.UnprocessableEntity("Failed to create role.");
                    }
                }

                //check user exists
                var user = await userManager.FindByNameAsync(registerUserDto.UserName);
                if (user != null)
                {
                    return Results.UnprocessableEntity($"User with the username '{registerUserDto.UserName}' already exists.");
                }

                var newUser = new ForumRestUser
                {
                    Email = registerUserDto.Email,
                    UserName = registerUserDto.UserName
                };

                var createUserResult = await userManager.CreateAsync(newUser, registerUserDto.Password);

                if (!createUserResult.Succeeded)
                {
                    var errors = string.Join(", ", createUserResult.Errors.Select(e => e.Description));
                    return Results.UnprocessableEntity($"User registration failed. Errors: {errors}");
                }
                await userManager.AddToRoleAsync(newUser, "ForumUser");

                return Results.Created("api/login", new UserDto(newUser.Id, newUser.UserName, newUser.Email));

            });


            app.MapPost("api/login", async (UserManager<ForumRestUser> userManager, JwtTokenService jwtTokenService, LoginDto loginDto) =>
            {
                // Check if the user exists
                var user = await userManager.FindByNameAsync(loginDto.UserName);
                if (user == null)
                {
                    return Results.UnprocessableEntity("UserName or Password was incorrect.");
                }

                // Check if the password is valid
                var isPasswordValid = await userManager.CheckPasswordAsync(user, loginDto.Password);
                if (!isPasswordValid)
                {
                    return Results.UnprocessableEntity("UserName or Password was incorrect.");
                }

                user.ForceRelogin = false;

                await userManager.UpdateAsync(user);


                var roles = await userManager.GetRolesAsync(user);
                Console.WriteLine($"User Roles: {string.Join(", ", roles)}");

                var accessToken = jwtTokenService.CreateAccessToken(user.UserName, user.Id, roles);
                var refreshToken = jwtTokenService.CreateRefreshToken(user.Id);

                return Results.Ok(new SuccessfulLoginDto(accessToken, refreshToken));

            });

            // access token
            app.MapPost("api/accessToken", async (UserManager<ForumRestUser> userManager, JwtTokenService jwtTokenService, RefreshAccessTokenDto refreshAccessTokenDto) =>
            {
                if (!jwtTokenService.TryParseRefreshToken(refreshAccessTokenDto.RefreshToken, out var claims))
                {
                    return Results.UnprocessableEntity();
                }

                var userId = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);

                var user = await userManager.FindByNameAsync(userId);
                if (user == null)
                {
                    return Results.UnprocessableEntity("Invalid token");
                }

                if (user.ForceRelogin)
                {
                    return Results.UnprocessableEntity();
                }

                var roles = await userManager.GetRolesAsync(user);

                var accessToken = jwtTokenService.CreateAccessToken(user.UserName, user.Id, roles);
                var refreshToken = jwtTokenService.CreateRefreshToken(user.Id);

                return Results.Ok(new SuccessfulLoginDto(accessToken, refreshToken));

            });

        }
    }
}

public record SuccessfulLoginDto(string AccessToken, string RefreshToken);

public record RegisterUserDto(string UserName, string Email, string Password);

public record LoginDto(string UserName, string Password);

public record UserDto(string UserId, string UserName, string Email);

public record RefreshAccessTokenDto(string RefreshToken);
