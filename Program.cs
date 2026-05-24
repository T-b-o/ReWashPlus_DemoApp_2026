using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ReWashPlus_DemoApp.Services;
using ReWashPlus_DemoApp;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register Blazored.LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Register domain services
builder.Services.AddScoped<PageTitleService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<CarService>();
builder.Services.AddScoped<ServiceService>();
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<PaymentService>();

await builder.Build().RunAsync();
