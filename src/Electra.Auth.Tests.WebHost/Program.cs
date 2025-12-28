using Electra.Auth.Controllers;
using Electra.Auth.Extensions;
using Electra.Models;
using Electra.Models.Entities;
using Electra.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddApplicationPart(typeof(AccountController).Assembly);
builder.Services.AddLogging();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<ElectraDbContext>(o => { o.UseInMemoryDatabase("ElectraAuthTestsDb"); });
var env = builder.Environment;
var config = builder.Configuration;
builder.Services.AddElectraAuthentication(env, config);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

var user = new ElectraUser()
{
    FirstName = "Test",
    LastName = "User",
    UserName = "testuser",
    Email = "testW@user.com",
    CreatedBy = "system",
    ModifiedBy = "system",
    CreatedOn = DateTimeOffset.UtcNow,
    ModifiedOn = DateTimeOffset.UtcNow,
    ProfilePictureDataUrl = "",
    MiddleName = "",
    UserHandle = [],
    RefreshToken = "",
    Profile = new ElectraUserProfile
    {
        Username = "testuser",
        Headline = "💩 My Headline 💩",
        Location = "Los Angeles, CA",
        Bio = "This is my bio",
        Website = "https://example.com",
        CreatedBy = "system",
        ModifiedBy = "system",
        CreatedOn = DateTimeOffset.UtcNow,
        ModifiedOn = DateTimeOffset.UtcNow,
    },
    UserSettings = new UserSettingsModel()
    {
        Stuff = "{}",
        CreatedBy = "system",
        ModifiedBy = "system",
        CreatedOn = DateTimeOffset.UtcNow,
        ModifiedOn = DateTimeOffset.UtcNow,
    }
};
var scope = app.Services.CreateScope();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ElectraUser>>();

var result = await userManager.CreateAsync(user, "Password123!");
if (!result.Succeeded)
{
    throw new Exception("Failed to create test user");
}

var verified = await userManager.FindByNameAsync("testuser");
if (verified == null)
    Console.WriteLine("User not found after creation");

await app.RunAsync();


public partial class Program;