window.onSpotifyWebPlaybackSDKReady = () => {
    resolveSdkInit();
}

let resolveSdkInit;
let sdkInit = new Promise(resolve => resolveSdkInit = resolve);
let dotNetRef = null;
let player = null;

export async function initialize(ref, playerName, initialVolume, token) {
    await sdkInit;

    console.log("Spotify Web Playback SDK ready");

    dotNetRef = ref;

    player = new Spotify.Player({
        name: playerName,
        getOAuthToken: async cb => { cb(token); },
        volume: initialVolume
    });

    player.addListener('ready', async args => {
        console.log('Ready with Device ID', args.device_id);
        await dotNetRef.invokeMethodAsync("NotifyReadyAsync", args);
    });

    player.addListener('not_ready', async args => {
        console.log('Device ID has gone offline', args.device_id);
        await dotNetRef.invokeMethodAsync("NotifyNotReadyAsync", args);
    });

    player.addListener('initialization_error', async error => {
        console.error(error.message);
        await dotNetRef.invokeMethodAsync("NotifyErrorAsync", error);
    });

    player.addListener('authentication_error', async error => {
        console.error(error.message);
        await dotNetRef.invokeMethodAsync("NotifyErrorAsync", error);
    });

    player.addListener('account_error', async error => {
        console.error(error.message);
        await dotNetRef.invokeMethodAsync("NotifyErrorAsync", error);
    });

    player.addListener('player_state_changed', async args => {
        console.log('PlayerStateChanged', args);
        await dotNetRef.invokeMethodAsync("NotifyPlayerStateChangedAsync", args);
    });

    console.log("Player connecting...", player);
    await player.connect();
    console.log("Player connected");
}

export async function setName(playerName) {
    if (player) {
        await player.setName(playerName);
        console.log("Player name updated");
    }
}

export async function setVolume(volume) {
    if (player)
        await player.setVolume(volume);
}

export async function pause() {
    if (player)
        await player.pause();
}

export async function resume() {
    if (player)
        await player.resume();
}

export async function previousTrack() {
    if (player)
        await player.previousTrack();
}

export async function nextTrack() {
    if (player)
        await player.nextTrack();
}