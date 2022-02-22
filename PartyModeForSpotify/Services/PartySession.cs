using SpotifyAPI.Web;
using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;

namespace PartyModeForSpotify.Services
{
    public abstract record PartySessionState
    {
        private PartySessionState() { }

        public record UninitializedSession : PartySessionState;

        public record ActiveSession(
            SpotifyClient SpotifyClient,
            string AccessToken,
            FullTrack? CurrentTrack,
            ImmutableQueue<FullTrack> Queue) : PartySessionState;

        public record BrokenSession(Exception Exception) : PartySessionState;

        public record ClosedSession : PartySessionState;
    }

    public class PartySession : IDisposable
    {
        private readonly BehaviorSubject<PartySessionState> state;
        private readonly Action disposeCallback;
        private readonly ILogger<PartySession> logger;
        private readonly IDisposable loggerScope;

        public Guid Id { get; }

        public string Title { get; } = "Spotify Party";

        public string QRCode { get; }

        public IObservable<PartySessionState> State => state;

        public PartySession(Guid id, string title, string accessToken, Action disposeCallback, ILogger<PartySession> logger)
        {
            Id = id;
            Title = title;
            this.disposeCallback = disposeCallback;
            this.logger = logger;

            QRCode = GenerateQRCodeUrl();

            loggerScope = logger.BeginScope(new
            {
                PartyId = Id,
                PartyTitle = Title
            });

            var config = SpotifyClientConfig.CreateDefault();
            var spotify = new SpotifyClient(config.WithToken(accessToken));
            state = new(new PartySessionState.ActiveSession(spotify, accessToken, null, ImmutableQueue<FullTrack>.Empty));
        }

        private string GenerateQRCodeUrl()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList
                .OrderByDescending(addr => addr.ToString())
                .FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork)?
                .ToString();

            if (ip is null)
                return "";

            var clientUrl = $"https://{ip}:7078/client/{Id}";
            return $"https://api.qrserver.com/v1/create-qr-code/?format=svg&size=120x120&qzone=2&data={clientUrl}";
        }

        public async Task SetHostDeviceAsync(string deviceId)
        {
            if (state.Value is PartySessionState.ActiveSession session)
            {
                await session.SpotifyClient.Player.TransferPlayback(new PlayerTransferPlaybackRequest(new[] { deviceId }));
                logger.LogInformation(nameof(SetHostDeviceAsync) + " {DeviceId}", deviceId);
            }
        }

        public async Task<IReadOnlyList<FullTrack>> SearchAsync(string searchText)
        {
            if (state.Value is PartySessionState.ActiveSession session && ! string.IsNullOrWhiteSpace(searchText))
            {
                var searchResponse = await session.SpotifyClient.Search.Item(new SearchRequest(SearchRequest.Types.Track, searchText));
                return searchResponse.Tracks.Items ?? (IReadOnlyList<FullTrack>)ImmutableList<FullTrack>.Empty;
            }

            return ImmutableList<FullTrack>.Empty;
        }

        public async Task EnqueueTrackAsync(FullTrack track)
        {
            if (state.Value is PartySessionState.ActiveSession session)
            {
                state.OnNext(session with
                {
                    Queue = session.Queue.Enqueue(track)
                });

                if (session.CurrentTrack is null)
                    await SkipToNextTrackAsync();

                logger.LogInformation(nameof(EnqueueTrackAsync) + " '{TrackName}' ({TrackUri})", track.Name, track.Uri);
            }
        }

        public async Task SkipToNextTrackAsync()
        {
            if (state.Value is PartySessionState.ActiveSession session)
            {
                if (session.Queue.IsEmpty)
                    return;

                var remainingQueue = session.Queue.Dequeue(out var track);

                await session.SpotifyClient.Player.ResumePlayback(new PlayerResumePlaybackRequest
                {
                    Uris = new[] { track.Uri },
                });

                state.OnNext(session with
                {
                    CurrentTrack = track,
                    Queue = remainingQueue
                });

                logger.LogInformation(nameof(SkipToNextTrackAsync) + " '{TrackName}' ({TrackUri})", track.Name, track.Uri);
            }
        }

        public void Dispose()
        {
            state.OnNext(new PartySessionState.ClosedSession());
            state.OnCompleted();
            loggerScope.Dispose();
            disposeCallback();
        }
    }
}
