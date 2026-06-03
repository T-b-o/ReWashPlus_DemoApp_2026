using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ReWashPlus_DemoApp.Services;
using ReWashPlus_DemoApp;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Blazored.LocalStorage
builder.Services.AddBlazoredLocalStorage();

// ── Infrastructure / context services (register first — others depend on these) ──
builder.Services.AddScoped<TenantContextService>();
builder.Services.AddScoped<ConnectivityService>();
builder.Services.AddScoped<OfflineDatabaseService>();

// ── Domain / feature services ──────────────────────────────────────────────────
builder.Services.AddScoped<PageTitleService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<VehicleService>();       // replaces CarService
builder.Services.AddScoped<ServiceService>();
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<PaymentService>();

// ── Cross-cutting services ─────────────────────────────────────────────────────
builder.Services.AddScoped<CarWashLocationService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SyncService>();
builder.Services.AddScoped<FeatureToggleService>();

await builder.Build().RunAsync();
