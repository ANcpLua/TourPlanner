using System.Security.Claims;
using BL.Interface;

namespace API.Infrastructure;

public class HttpUserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public string UserId =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User is not authenticated.");
}
