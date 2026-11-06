var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IUsuarioRepositorio, UsuarioRepositorioEmMemoria>();
builder.Services.AddSingleton<IPasseioRepositorio, PasseioRepositorioEmMemoria>();
builder.Services.AddSingleton<INotificacaoRepositorio, NotificacaoRepositorioEmMemoria>();
builder.Services.AddSingleton<AutenticacaoServico>();
builder.Services.AddSingleton<NotificacaoServico>();

// Relógio injetável: os endpoints leem "agora" de time.GetUtcNow().UtcDateTime em vez de DateTime.UtcNow.
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.MapUsuarioEndpoints();

app.MapPetEndpoints();

app.Run();
