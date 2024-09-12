using Microsoft.FluentUI.AspNetCore.Components;
using StackExchange.Redis;
using TravelService.CustomerUI.Clients.Backend;
using TravelService.CustomerUI.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
{
   config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
   config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
   config.AddEnvironmentVariables();
});

var configuration = builder.Configuration;

builder.Services.AddRazorComponents()   
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
   var redisConfiguration = ConfigurationOptions.Parse(configuration["RedisConnectionString"], true);
   return ConnectionMultiplexer.Connect(redisConfiguration);
});

builder.Services.AddHttpClient<TravelAgentBackendClient>(client =>
    client.BaseAddress = new Uri(configuration.GetValue<string>("apiUrl")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
