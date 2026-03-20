using System.Security.Claims;
using Contracts.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/account").WithTags("Account");
        auth.MapPost("/register", RegisterAsync).AllowAnonymous();
        auth.MapPost("/login", LoginAsync).AllowAnonymous();
        auth.MapPost("/logout", LogoutAsync);
        auth.MapGet("/me", GetCurrentUser);
        return endpoints;
    }

    internal static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager)
    {
        var user = new IdentityUser { UserName = request.Email, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return Results.ValidationProblem(
                result.Errors.GroupBy(static e => e.Code).ToDictionary(
                    static g => g.Key,
                    static g => g.Select(static e => e.Description).ToArray()));
        }

        await signInManager.SignInAsync(user, isPersistent: true);
        return Results.Ok(new UserInfo { UserId = user.Id, Email = user.Email });
    }

    internal static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager)
    {
        var result = await signInManager.PasswordSignInAsync(
            request.Email, request.Password, isPersistent: true, lockoutOnFailure: false);

        if (!result.Succeeded)
            return Results.Problem("Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);

        var user = await userManager.FindByEmailAsync(request.Email);
        return Results.Ok(new UserInfo { UserId = user!.Id, Email = user.Email! });
    }

    internal static async Task<IResult> LogoutAsync(SignInManager<IdentityUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.Ok();
    }

    internal static IResult GetCurrentUser(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = httpContext.User.FindFirstValue(ClaimTypes.Email)
                    ?? httpContext.User.FindFirstValue(ClaimTypes.Name);

        if (userId is null || email is null)
            return Results.Problem("User not found.", statusCode: StatusCodes.Status401Unauthorized);

        return Results.Ok(new UserInfo { UserId = userId, Email = email });
    }
}
