using System.Net.Http.Json;
using System.Security.Claims;
using Contracts.Auth;
using Microsoft.AspNetCore.Components.Authorization;

namespace UI.Auth;

public class CookieAuthenticationStateProvider(HttpClient httpClient) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var user = await httpClient.GetFromJsonAsync<UserInfo>("api/auth/me");
            if (user is null) return Anonymous;

            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Email, user.Email)
            ], "cookie");

            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return Anonymous;
        }
    }

    public void NotifyAuthStateChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
