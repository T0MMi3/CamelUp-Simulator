var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CamelUp Simulator API",
        Version = "v1",
        Description = "Real-time Monte Carlo EV engine for the Camel Up board game"
    });
});

// ── CORS ──────────────────────────────────────────────────────────────────────
// Allow the React frontend (dev: localhost:3000/5173, prod: your domain)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "https://camelup-simulator-production.up.railway.app"
                "https://camel-up-simulator.vercel.app"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<GameService>();

// ── App ───────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CamelUp API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

app.Run();