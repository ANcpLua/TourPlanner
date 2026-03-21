using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Contracts.Auth;
using Microsoft.AspNetCore.Components;
using UI.Auth;

namespace UI.ViewModel;

public class AuthViewModel(
    HttpClient httpClient,
    NavigationManager navigationManager,
    CookieAuthenticationStateProvider authStateProvider) : INotifyPropertyChanged
{
    public string? ErrorMessage
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public bool IsProcessing
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task LoginAsync(LoginRequest request)
    {
        if (!await SendAuthRequestAsync("api/auth/login", request)) return;

        authStateProvider.NotifyAuthStateChanged();
        navigationManager.NavigateTo("/");
    }

    public async Task RegisterAsync(RegisterRequest request)
    {
        if (!await SendAuthRequestAsync("api/auth/register", request)) return;

        authStateProvider.NotifyAuthStateChanged();
        navigationManager.NavigateTo("/");
    }

    private async Task<bool> SendAuthRequestAsync<T>(string endpoint, T request)
    {
        if (IsProcessing) return false;

        IsProcessing = true;
        ErrorMessage = null;

        try
        {
            var response = await httpClient.PostAsJsonAsync(endpoint, request);

            if (response.IsSuccessStatusCode) return true;

            ErrorMessage = endpoint switch
            {
                _ when await response.Content.ReadAsStringAsync() is { } body
                       && body.Contains("DuplicateUserName") => "An account with this email already exists.",
                "api/auth/login" => "Invalid email or password.",
                _ => "Registration failed. Please check your input."
            };

            return false;
        }
        catch
        {
            ErrorMessage = "Unable to reach the server.";
            return false;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
