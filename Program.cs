using System.Text;
using System.Text.Json.Serialization;
using EMS.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using Serilog;
using EMS.Api.Hubs;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up the application");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

    // --- YAHAN CHANGE KIYA GAYA HAI ---
    var corsOrigin = builder.Configuration["CorsOrigin"];
    if (string.IsNullOrEmpty(corsOrigin))
    {
        // Agar CorsOrigin set nahi hai, to application start hi nahi hogi.
        throw new InvalidOperationException("'CorsOrigin' is not configured in appsettings.json.");
    }

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowReactApp",
            policy =>
            {
                policy.WithOrigins(corsOrigin)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials(); // <-- YEH LINE SIGNALR KE LIYE BAHUT ZAROORI HAI
            });
    });
    // --- CHANGE END ---

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
            };
        });

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        });
    builder.Services.AddSignalR();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseStaticFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(
            Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "documents")),
        RequestPath = "/documents"
    });

    app.UseCors("AllowReactApp");
    app.UseAuthentication();
    app.UseAuthorization();

    app.Use(async (context, next) =>
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            context.Response.Headers.Append("Pragma", "no-cache");
            context.Response.Headers.Append("Expires", "0");
            return Task.CompletedTask;
        });
        await next();
    });

    app.MapControllers();
    app.MapHub<ChatHub>("/chatHub");
    app.MapHub<NotificationHub>("/notificationHub");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred during application bootstrapping");
}
finally
{
    Log.CloseAndFlush();
}

