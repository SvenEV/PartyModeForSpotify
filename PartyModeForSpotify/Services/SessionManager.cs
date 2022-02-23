using Microsoft.Extensions.Options;

namespace PartyModeForSpotify.Services
{
    public class SessionManager
    {
        private readonly Dictionary<Guid, PartySession> sessions = new();
        private readonly ILoggerFactory loggerFactory;
        private readonly SpotifyConfiguration spotifyConfig;

        public SessionManager(ILoggerFactory loggerFactory, IOptions<SpotifyConfiguration> spotifyConfig)
        {
            this.loggerFactory = loggerFactory;
            this.spotifyConfig = spotifyConfig.Value;
        }

        public PartySession CreateSession(string title, string accessToken)
        {
            var id = Guid.NewGuid();
            var session = new PartySession(id, title, accessToken, DestroySession, loggerFactory.CreateLogger<PartySession>(), spotifyConfig);
            sessions.Add(id, session);
            return session;

            void DestroySession()
            {
                sessions.Remove(id);
            }
        }

        public PartySession? TryGetSession(Guid id)
        {
            return sessions.TryGetValue(id, out var session) ? session : null;
        }

        public PartySession? FirstSession => sessions.Values.FirstOrDefault();
    }
}
