using CasinoApp.DataAccess;
using CasinoApp.DataAccess.DB_operations;
using CasinoApp.Web.Client.Pages;
using CasinoApp.Web.Components;
using CasinoApp.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<PlayerRepository>();
builder.Services.AddScoped<BetRepository>();
builder.Services.AddScoped<GameRepository>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<EmailService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddScoped<PlayerRepository>();
var app = builder.Build();
DatabaseInitializer.Initialize();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(CasinoApp.Web.Client._Imports).Assembly);
app.UseStaticFiles();
app.Run();
