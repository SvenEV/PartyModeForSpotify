using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;

namespace PartyModeForSpotify.Services
{
    public class SpotifyAuthenticationManager
    {
        private readonly NavigationManager navigationManager;
        private readonly SpotifyConfiguration spotifyConfig;

        public string? AccessToken { get; private set; }

        public SpotifyAuthenticationManager(NavigationManager navigationManager, IOptions<SpotifyConfiguration> spotifyConfig)
        {
            this.navigationManager = navigationManager;
            this.spotifyConfig = spotifyConfig.Value;
        }

        public void SignIn(string relativeRedirectUri)
        {
            var redirectUri = navigationManager.ToAbsoluteUri(relativeRedirectUri);

            var loginRequest = new LoginRequest(redirectUri, spotifyConfig.ClientId!, LoginRequest.ResponseType.Code)
            {
                Scope = new[]
                {
                    Scopes.Streaming,
                    Scopes.UserModifyPlaybackState,
                    Scopes.UserReadCurrentlyPlaying,
                    Scopes.UserReadPlaybackPosition,
                    Scopes.UserReadPlaybackState,
                    Scopes.PlaylistReadPrivate,
                    Scopes.PlaylistReadCollaborative
                }
            };

            // Redirect user to uri via your favorite web-server
            navigationManager.NavigateTo(loginRequest.ToUri().ToString());
        }

        public async Task<string> CompleteSignInAsync(string relativeRedirectUri, string authCode)
        {
            var redirectUri = navigationManager.ToAbsoluteUri(relativeRedirectUri);

            var response = await new OAuthClient().RequestToken(
                new AuthorizationCodeTokenRequest(spotifyConfig.ClientId!, spotifyConfig.ClientSecret!, authCode, redirectUri)
            );

            AccessToken = response.AccessToken;
            return AccessToken;
        }
    }
}
