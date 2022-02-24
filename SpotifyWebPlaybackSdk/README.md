# Blazor Wrapper for Spotify Web Playback SDK

## How to Use

Include the following scripts on the page:

```html
<script src="_content/SpotifyWebPlaybackSdk/spotify.js" type="module"></script>
<script src="https://sdk.scdn.co/spotify-player.js"></script>
```

Then use the `SpotifyWebPlayer` component as follows:

```razor
<SpotifyWebPlayer AccessToken="your-access-token" />
```

The `AccessToken` property is required, the others are optional.