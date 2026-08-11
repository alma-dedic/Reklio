using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Reklio.Api.BackgroundJobs;
using Reklio.Api.Data;
using Reklio.Api.Models;
using Reklio.Api.Services;
using Reklio.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Reklio.Api", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Unesi samo JWT token (bez 'Bearer ' prefiksa)."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

builder.Services.AddDbContext<ReklioDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
        // EF tools (10.0.5) su stariji od runtime-a (10.0.10) → lažni "pending changes"
        // pri MigrateAsync. Migracije su tačne; suzbijamo taj warning-kao-grešku.
        .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

builder.Services
    .AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<ReklioDbContext>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IClaimEvidenceService, ClaimEvidenceService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReceiptValidationService, ReceiptValidationService>();
builder.Services.AddSingleton<IFileStorageService, ClaimFileStorage>();

// T9.1 — deterministički gate je čista funkcija bez stanja.
builder.Services.AddSingleton<IDecisionGate, DecisionGate>();
// T9.4 — orkestrator koji worker zove po reklamaciji.
builder.Services.AddScoped<ClaimAnalysisPipeline>();

builder.Services.AddSingleton<IClaimQueue, ClaimQueue>();
builder.Services.AddHostedService<ClaimProcessingWorker>();

// T5.9 — fraud servis zove Python (FastAPI) preko typed HttpClient-a.
builder.Services.AddHttpClient<IFraudService, FraudService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["AiService:BaseUrl"] ?? "http://127.0.0.1:8000");
});

// T6.6 — OCR servis isto zove Python (FastAPI).
builder.Services.AddHttpClient<IOcrService, OcrService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["AiService:BaseUrl"] ?? "http://127.0.0.1:8000");
});

// T7.6 — RAG servis (pravilnik) zove Python (FastAPI).
builder.Services.AddHttpClient<IRagService, RagService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["AiService:BaseUrl"] ?? "http://127.0.0.1:8000");
});

// T8.3 — vision servis (oštećenja) zove Python (FastAPI).
builder.Services.AddHttpClient<IVisionService, VisionService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["AiService:BaseUrl"] ?? "http://127.0.0.1:8000");
});

// T9.3 — explanation servis (LLM objašnjenje odluke) zove Python (FastAPI).
builder.Services.AddHttpClient<IExplanationService, ExplanationService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["AiService:BaseUrl"] ?? "http://127.0.0.1:8000");
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();

const string devCorsPolicy = "AngularDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(devCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReklioDbContext>();
    await db.Database.MigrateAsync();

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    await DbSeeder.SeedAsync(userManager);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(devCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();