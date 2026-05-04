using CareConnect.Application.Abstractions;
using CareConnect.Infrastructure;
using CareConnect.Web.Security;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Lead", "DepartmentLeadOnly");
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
});
builder.Services.AddHealthChecks();
builder.Services.AddCareConnectInfrastructure(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSecurityHeaders();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seedData = scope.ServiceProvider.GetRequiredService<ISeedDataService>();
    await seedData.SeedDevelopmentAsync();
}

app.Run();

public partial class Program;
