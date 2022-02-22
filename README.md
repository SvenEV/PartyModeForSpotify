# Party Mode for Spotify

## Registration on Spotify for Developers

To access Spotify APIs you must register an app on *Spotify for Developers*.

* Go to the [dashboard](https://developer.spotify.com/dashboard/login) and log in
* Create a new app and take note of the client ID and client secret
* Configure PartyModeForSpotify on your local machine as follows (commands must be run from the project folder "PartyModeForSpotify"):
  * `dotnet user-secrets set "Spotify:ClientId" "your-client-ID"`
  * `dotnet user-secrets set "Spotify:ClientSecret" "your-client-secret"`
  * Documentation: [Safe storage of app secrets in development in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-6.0&tabs=windows)
* Click 'Edit Settings' on the dashboard, add "https://localhost:7078/login" as redirect URI, and click 'Save'
* Click 'Users and Access' on the dashboard and add the user(s) that should be able to use the app

## Build and Run

From the project folder "PartyModeForSpotify":

```
dotnet run
```