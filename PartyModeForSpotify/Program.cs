using Microsoft.Extensions.Options;
using PartyModeForSpotify;
using PartyModeForSpotify.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.Configure<SpotifyConfiguration>(builder.Configuration.GetSection("Spotify"));
builder.Services.AddScoped<SpotifyAuthenticationManager>();
builder.Services.AddSingleton<SessionManager>();
builder.Services.AddHttpClient();

var app = builder.Build();

// Validate configuration
var config = app.Services.GetRequiredService<IOptions<SpotifyConfiguration>>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

if (string.IsNullOrEmpty(config.Value.ClientId))
    logger.LogError("Incomplete configuration: ClientId is not configured");

if (string.IsNullOrEmpty(config.Value.ClientSecret))
    logger.LogError("Incomplete configuration: ClientSecret is not configured");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
