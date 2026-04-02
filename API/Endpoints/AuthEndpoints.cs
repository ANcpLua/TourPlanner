using System.Security.Claims;
using Contracts.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup(ApiRoute.ApiBase)
            .MapGroup(GetAuthSegment())
            .WithTags(GetAuthTag());
        auth.MapPost(ApiRoute.Auth.Register, RegisterAsync).AllowAnonymous();
        auth.MapPost(ApiRoute.Auth.Login, LoginAsync).AllowAnonymous();
        auth.MapPost(ApiRoute.Auth.Logout, LogoutAsync).RequireAuthorization();
        auth.MapGet(ApiRoute.Auth.Me, GetCurrentUser).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
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

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Results.Problem("Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);

        var result = await signInManager.PasswordSignInAsync(
            user, request.Password, isPersistent: true, lockoutOnFailure: true);

        return result switch
        {
            { IsLockedOut: true } => Results.Problem(
                "Account locked due to too many failed attempts. Try again later.",
                statusCode: StatusCodes.Status429TooManyRequests),
            { Succeeded: true } => Results.Ok(new UserInfo
            {
                UserId = user.Id, Email = user.Email ?? request.Email
            }),
            _ => Results.Problem("Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized)
        };
    }

    private static async Task<IResult> LogoutAsync(SignInManager<IdentityUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.Ok();
    }

    private static IResult GetCurrentUser(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = httpContext.User.FindFirstValue(ClaimTypes.Email)
                    ?? httpContext.User.FindFirstValue(ClaimTypes.Name);

        if (userId is null || email is null)
            return Results.Problem("Required identity claims missing.", statusCode: StatusCodes.Status401Unauthorized);

        return Results.Ok(new UserInfo { UserId = userId, Email = email });
    }

    private static string GetAuthSegment()
    {
        return nameof(AuthEndpoints)[..^nameof(Endpoints).Length].ToLowerInvariant();
    }

    private static string GetAuthTag()
    {
        return nameof(IdentityUser).Replace("User", string.Empty, StringComparison.Ordinal);
    }
}
