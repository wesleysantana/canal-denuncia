using CanalDenuncias.API.Bootstrap;
using CanalDenuncias.API.IoC;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    // Política baseada em IP para proteger contra abusos anônimos
    options.AddSlidingWindowLimiter("ReclamacaoPolicy", config =>
    {
        // 5 requisições por janela (suficiente para o usuário e talvez algum erro)
        config.PermitLimit = 5;

        // A janela total de tempo
        config.Window = TimeSpan.FromMinutes(5);

        // Divide a janela em segmentos para suavizar a contagem
        config.SegmentsPerWindow = 5;

        // Não queremos fila longa para anônimos (evita gastar memória do servidor)
        config.QueueLimit = 0;
    });
});

builder.Services.AddAuthorization(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true) // Sempre permite
            .Build();
    }
});

// Bootstrap: Configuration and Dependency Injection
Configuration.Register(builder.Services, builder.Configuration);
IoC.Register(builder.Services);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseRateLimiter();

app.UseCors(c => c.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.Run();