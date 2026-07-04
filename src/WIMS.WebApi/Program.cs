using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using WIMS.Application;
using WIMS.Application.Common.Interfaces;
using WIMS.Infrastructure;
using WIMS.Infrastructure.Identity;
using WIMS.Infrastructure.Persistence;
using WIMS.WebApi.Authorization;
using WIMS.WebApi.Middleware;
using WIMS.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog (Rolling File + Console) ──
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// ── طبقات التطبيق ──
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── المستخدم الحالي (من HttpContext) ──
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// ── RBAC: موفّر سياسات ديناميكي + معالج الصلاحيات ──
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// ── المصادقة عبر JWT ──
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;

// حارس إنتاج (OWASP A02/A05): امنع الإقلاع بمفتاح توقيع افتراضي/ضعيف خارج بيئة التطوير.
if (!builder.Environment.IsDevelopment() &&
    (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32 || jwt.SigningKey.Contains("CHANGE_ME")))
{
    throw new InvalidOperationException(
        "Jwt:SigningKey غير آمن للإنتاج — عيّنه عبر متغيّر بيئة بمفتاح عشوائي طوله ≥ 32 بايت.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── الترويسات المُمرَّرة من الوكيل العكسي (IIS/Kestrel) ──
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ── CORS لواجهة Angular (المنشأ من الإعدادات) ──
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (corsOrigins.Length > 0)
        policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));

// ── تحديد المعدّل (Rate Limiting) — دفاع ضد الإغراق وكسر كلمات المرور (OWASP A07) ──
var rlEnabled = builder.Configuration.GetValue("RateLimiting:Enabled", true);
var rlGlobalPerMin = builder.Configuration.GetValue("RateLimiting:PermitPerMinute", 120);
var rlLoginPerMin = builder.Configuration.GetValue("RateLimiting:Login:PermitPerMinute", 10);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        if (!rlEnabled) return RateLimitPartition.GetNoLimiter("disabled");
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rlGlobalPerMin,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });

    // سياسة مشدّدة لنقطة تسجيل الدخول.
    options.AddPolicy("auth", context =>
    {
        if (!rlEnabled) return RateLimitPartition.GetNoLimiter("disabled");
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter($"auth:{key}", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rlLoginPerMin,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });
});

// ── محرّك الإنذارات الخلفي (المرحلة 4) ──
builder.Services.AddHostedService<AlertsBackgroundService>();

// ── Swagger + دعم Bearer ──
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "WIMS API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

var app = builder.Build();

// ── تهيئة قاعدة البيانات وبذر البيانات الأساسية ──
await DbSeeder.SeedAsync(app.Services);

app.UseForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseSerilogRequestLogging();

// ── خدمة واجهة Angular المبنية من wwwroot (نفس الـ origin) ──
// تُفعَّل فقط عند وجود wwwroot/index.html، فيبقى وضع التطوير API-only سليماً.
var webRoot = app.Environment.WebRootPath ?? string.Empty;
var serveSpa = !string.IsNullOrEmpty(webRoot) && File.Exists(Path.Combine(webRoot, "index.html"));
if (serveSpa)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// إعادة التوجيه إلى HTTPS اختيارية — تُعطَّل في الحزمة الذاتية (localhost/HTTP) عبر Security:RequireHttps=false.
if (builder.Configuration.GetValue("Security:RequireHttps", true))
    app.UseHttpsRedirection();

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// أي مسار غير /api/* وغير ملف موجود يُخدَم كواجهة SPA (توجيه من طرف Angular).
if (serveSpa)
    app.MapFallbackToFile("index.html");

app.Run();

/// <summary>يُعرّف للاختبارات التكاملية (WebApplicationFactory).</summary>
public partial class Program;
