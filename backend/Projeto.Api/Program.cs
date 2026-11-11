using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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

// SQLite em arquivo. Caminho relativo é resolvido contra o content root, nunca contra o
// diretório de trabalho do processo; caminhos absolutos (config ou variável de ambiente)
// são preservados como vieram.
var construtorDeConexao = new SqliteConnectionStringBuilder(
    builder.Configuration.GetConnectionString("PetWalk") ?? "Data Source=App_Data/petwalk.db");

if (!Path.IsPathRooted(construtorDeConexao.DataSource))
{
    construtorDeConexao.DataSource = Path.Combine(
        builder.Environment.ContentRootPath, construtorDeConexao.DataSource);
}

// O SQLite não cria diretórios: sem isso a primeira gravação falharia.
var diretorioDoBanco = Path.GetDirectoryName(construtorDeConexao.DataSource);
if (!string.IsNullOrEmpty(diretorioDoBanco))
{
    Directory.CreateDirectory(diretorioDoBanco);
}

// Apenas registra o contexto: nenhuma conexão é aberta e nenhum schema é criado no startup.
builder.Services.AddDbContext<PetWalkDbContext>(options =>
    options.UseSqlite(construtorDeConexao.ConnectionString));

// Mesmo escopo do PetWalkDbContext: quando os repositórios passarem a ser EF, todos
// compartilham o contexto e o change tracker desta unidade.
builder.Services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();

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

app.MapPasseadorEndpoints();

app.MapPasseioEndpoints();

app.MapNotificacaoEndpoints();

app.Run();
