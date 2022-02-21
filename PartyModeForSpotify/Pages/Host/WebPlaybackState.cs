using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace ParyModeForSpotify.Pages.Host
{
    // https://developer.spotify.com/documentation/web-playback-sdk/reference/#objects

    public record WebPlaybackPlayer(
        [property: JsonPropertyName("device_id")] string DeviceId
    );

    public record WebPlaybackState(
        [property: JsonPropertyName("paused")] bool IsPaused,
        [property: JsonPropertyName("shuffle")] bool IsShuffleEnabled,
        [property: JsonPropertyName("position")] int PositionMilliseconds,
        [property: JsonPropertyName("duration")] int DurationMilliseconds,
        [property: JsonPropertyName("track_window")] WebPlaybackTrackWindow TrackWindow
    );

    public record WebPlaybackTrackWindow(
        [property: JsonPropertyName("current_track")] WebPlaybackTrack CurrentTrack,
        [property: JsonPropertyName("previous_tracks")] ImmutableList<WebPlaybackTrack> PreviousTracks,
        [property: JsonPropertyName("next_tracks")] ImmutableList<WebPlaybackTrack> NextTracks
    );

    public record WebPlaybackTrack(
        [property: JsonPropertyName("uri")] string Uri,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("type"), JsonConverter(typeof(JsonStringEnumConverter))] WebPlaybackTrackType Type,
        [property: JsonPropertyName("media_type"), JsonConverter(typeof(JsonStringEnumConverter))] WebPlaybackTrackMediaType MediaType,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("is_playable")] bool IsPlayable
        // TODO: Album
        // TODO: Artists
    );

    public enum WebPlaybackTrackType
    {
        Track, Episode, Ad
    }

    public enum WebPlaybackTrackMediaType
    {
        Audio, Video
    }

    public record WebPlaybackError(
        [property: JsonPropertyName("message")] string Message
    );
}
