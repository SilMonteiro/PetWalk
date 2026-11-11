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

// Tudo Scoped: repositórios, serviços e unidade de trabalho compartilham o mesmo
// PetWalkDbContext na requisição. O domínio compara entidades por referência (ex.:
// Passeio.ExigirPasseadorAtivo), então instâncias duplicadas quebrariam as regras.
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IPasseioRepositorio, PasseioRepositorio>();
builder.Services.AddScoped<INotificacaoRepositorio, NotificacaoRepositorio>();
builder.Services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();

builder.Services.AddScoped<AutenticacaoServico>();
builder.Services.AddScoped<NotificacaoServico>();

// Tradução única de falhas de constraint do EF/SQLite (409/422) — ver ErroDePersistencia.
// AddProblemDetails cobre o caso não tratado: é o fallback exigido pelo UseExceptionHandler()
// sem path; as exceções reconhecidas continuam respondendo pelo handler abaixo.
builder.Services.AddExceptionHandler<ErroDePersistenciaHandler>();
builder.Services.AddProblemDetails();

// Relógio injetável: os endpoints leem "agora" de time.GetUtcNow().UtcDateTime em vez de DateTime.UtcNow.
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

// Falha rápida: nenhuma rotina de startup cria o schema. Sem ele, toda rota que toca o banco
// responderia 500; a verificação interrompe aqui, com o comando manual de migration.
using (var escopo = app.Services.CreateScope())
{
    VerificacaoDeSchema.GarantirSchemaAplicado(
        escopo.ServiceProvider.GetRequiredService<PetWalkDbContext>());
}

// Primeiro do pipeline: converte DbUpdateException conhecida em resposta e deixa as demais
// exceções seguirem para o 500 padrão.
app.UseExceptionHandler();

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
