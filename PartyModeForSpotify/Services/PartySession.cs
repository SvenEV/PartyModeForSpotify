using SpotifyAPI.Web;
using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;

namespace PartyModeForSpotify.Services
{
    public record PartySessionState(
        bool IsActive,
        SpotifyClient SpotifyClient,
        string AccessToken,
        FullTrack? CurrentTrack,
        ImmutableQueue<FullTrack> Queue);

    public class PartySession : IDisposable
    {
        private readonly BehaviorSubject<PartySessionState> stateSubject;
        private readonly Action disposeCallback;
        private readonly ILogger<PartySession> logger;
        private readonly IDisposable loggerScope;

        public Guid Id { get; }

        public string Title { get; } = "Spotify Party";

        public string QRCode { get; }

        public IObservable<PartySessionState> State => stateSubject;

        public PartySession(Guid id, string title, string accessToken, Action disposeCallback, ILogger<PartySession> logger, SpotifyConfiguration spotifyConfig)
        {
            Id = id;
            Title = title;
            this.disposeCallback = disposeCallback;
            this.logger = logger;

            QRCode = GenerateQRCodeUrl(spotifyConfig.DeployUrl!);

            loggerScope = logger.BeginScope(new
            {
                PartyId = Id,
                PartyTitle = Title
            });

            var config = SpotifyClientConfig.CreateDefault();
            var spotify = new SpotifyClient(config.WithToken(accessToken));

            var initialState = new PartySessionState(
                IsActive: true,
                SpotifyClient: spotify,
                AccessToken: accessToken,
                CurrentTrack: null,
                Queue: ImmutableQueue<FullTrack>.Empty);

            stateSubject = new(initialState);
        }

        private string GenerateQRCodeUrl(string deployUrl)
        {
            var clientUrl = string.IsNullOrEmpty(deployUrl) ? GetDevelopmentUrl() : $"{deployUrl}/client/{Id}";
            return $"https://api.qrserver.com/v1/create-qr-code/?format=svg&size=120x120&qzone=2&data={clientUrl}";

            string GetDevelopmentUrl()
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ip = host.AddressList
                    .OrderByDescending(addr => addr.ToString())
                    .FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork)?
                    .ToString();

                if (ip is null)
                    return "";

                return $"https://{ip}:7078/client/{Id}";
            }
        }

        public async Task SetPlaybackDeviceAsync(string deviceId)
        {
            if (stateSubject.Value is { IsActive: true } state)
            {
                await state.SpotifyClient.Player.TransferPlayback(new PlayerTransferPlaybackRequest(new[] { deviceId }));
                logger.LogInformation(nameof(SetPlaybackDeviceAsync) + " {DeviceId}", deviceId);
            }
        }

        public async Task<IReadOnlyList<FullTrack>> SearchAsync(string searchText)
        {
            if (stateSubject.Value is { IsActive: true } state && !string.IsNullOrWhiteSpace(searchText))
            {
                var searchResponse = await state.SpotifyClient.Search.Item(new SearchRequest(SearchRequest.Types.Track, searchText));
                return searchResponse.Tracks.Items ?? (IReadOnlyList<FullTrack>)ImmutableList<FullTrack>.Empty;
            }

            return ImmutableList<FullTrack>.Empty;
        }

        public async Task EnqueueTrackAsync(FullTrack track)
        {
            if (stateSubject.Value is { IsActive: true } state)
            {
                stateSubject.OnNext(state with
                {
                    Queue = state.Queue.Enqueue(track)
                });

                if (state.CurrentTrack is null)
                    await SkipToNextTrackAsync();

                logger.LogInformation(nameof(EnqueueTrackAsync) + " '{TrackName}' ({TrackUri})", track.Name, track.Uri);
            }
        }

        public async Task SkipToNextTrackAsync()
        {
            if (stateSubject.Value is { IsActive: true } state)
            {
                if (state.Queue.IsEmpty)
                    return;

                var remainingQueue = state.Queue.Dequeue(out var track);

                await state.SpotifyClient.Player.ResumePlayback(new PlayerResumePlaybackRequest
                {
                    Uris = new[] { track.Uri },
                });

                stateSubject.OnNext(state with
                {
                    CurrentTrack = track,
                    Queue = remainingQueue
                });

                logger.LogInformation(nameof(SkipToNextTrackAsync) + " '{TrackName}' ({TrackUri})", track.Name, track.Uri);
            }
        }

        public void Dispose()
        {
            stateSubject.OnNext(stateSubject.Value with { IsActive = false });
            stateSubject.OnCompleted();
            loggerScope.Dispose();
            disposeCallback();
        }
    }
}
