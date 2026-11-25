using RedLightGreenLight.Components;
using RedLightGreenLight.Services;
using RedLightGreenLight.Services.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(o => o.DetailedErrors = true);

builder.Services.AddSingleton<LogService>();
builder.Services.AddSingleton<ILoggerProvider>(sp => sp.GetRequiredService<LogService>());
builder.Services.AddSingleton<WebSocketService>();
builder.Services.AddSingleton<GameManager>();
builder.Services.AddHostedService<MQTTListenerService>();
builder.Services.AddHostedService<GameHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
