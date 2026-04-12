using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Motor.Claim.Application.Features.Claim.Commands;
using Motor.Claim.Application.Features.Claim.Queries;
using Motor.Claim.Application.Features.Coverage.Commands;
using Motor.Claim.Application.Features.Coverage.Queries;
using Motor.Claim.Application.Features.Workshop.Commands;
using Motor.Claim.Application.Features.Workshop.Queries;
using Motor.Claim.Application.Features.WorkshopAppointment.Commands;
using Motor.Claim.Application.Features.WorkshopAppointment.Queries;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Application.Services;
using Motor.Claim.Infrastructure.Persistence.Context;
using Motor.Claim.Infrastructure.Persistence.Repositories;
using Motor.Claim.Infrastructure.Shared.Services;
using Motor.Claim.WebApi.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICoverageRepository, CoverageRepository>();
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<IWorkshopRepository, WorkshopRepository>();
builder.Services.AddScoped<IWorkshopAppointmentRepository, WorkshopAppointmentRepository>();
builder.Services.AddScoped<IWorkshopRepairEstimateRepository, WorkshopRepairEstimateRepository>();
builder.Services.AddScoped<ISystemActivityLogRepository, SystemActivityLogRepository>();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CoverageService>();
builder.Services.AddScoped<ClaimService>();
builder.Services.AddScoped<WorkshopService>();
builder.Services.AddScoped<WorkshopRepairEstimateService>();
builder.Services.AddScoped<StpValidationService>();
builder.Services.AddScoped<SystemMonitoringService>();
builder.Services.AddScoped<MockOcrExtractor>();
builder.Services.AddHttpClient<AzureDocumentIntelligenceOcrExtractor>();
builder.Services.AddScoped<IOcrExtractor>(sp =>
    sp.GetRequiredService<AzureDocumentIntelligenceOcrExtractor>());

builder.Services.AddScoped<CreateCoverageCommandHandler>();
builder.Services.AddScoped<GetMyCoveragesQueryHandler>();
builder.Services.AddScoped<GetAllCoveragesQueryHandler>();

builder.Services.AddScoped<CreateClaimCommandHandler>();
builder.Services.AddScoped<GetMyClaimsQueryHandler>();
builder.Services.AddScoped<GetAllClaimsQueryHandler>();

builder.Services.AddScoped<CreateWorkshopCommandHandler>();
builder.Services.AddScoped<UpdateWorkshopCommandHandler>();
builder.Services.AddScoped<DeleteWorkshopCommandHandler>();
builder.Services.AddScoped<GetPanelWorkshopStatesQueryHandler>();
builder.Services.AddScoped<GetPanelWorkshopsByStateQueryHandler>();
builder.Services.AddScoped<GetAllWorkshopsQueryHandler>();
builder.Services.AddScoped<GetApprovedClaimsForPanelWorkshopQueryHandler>();
builder.Services.AddScoped<CreateOrUpdateWorkshopAppointmentCommandHandler>();
builder.Services.AddScoped<GetWorkshopAppointmentByClaimQueryHandler>();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CustomerOnly", policy =>
        policy.RequireRole("Customer"));

    options.AddPolicy("OfficerOrAdmin", policy =>
        policy.RequireRole("Officer", "Admin"));

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("PanelWorkshopOnly", policy =>
        policy.RequireRole("PanelWorkshop"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseAuthentication();
//app.UseMiddleware<SystemActivityLoggingMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
