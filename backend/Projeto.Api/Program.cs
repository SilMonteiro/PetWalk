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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapGet("/teste", () => "API PetWalk funcionando!");

app.MapPost("/cadastro", (UsuarioRequest req) =>
{
    try
    {
        var usuarioCriado = Usuario.Cadastrar(req.Nome, req.Email, req.Telefone, req.Endereco, req.TipoPerfil, req.Senha);
        return Results.Ok(new { mensagem = "Usuário cadastrado com sucesso!", id = usuarioCriado.Id });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.MapPost("/login", (LoginRequest req) =>
{
    var usuario = Usuario.Login(req.Email, req.Senha);

    if (usuario == null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        mensagem = "Login realizado com sucesso!",
        id = usuario.Id,
        nome = usuario.Nome,
        email = usuario.Email
    });
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record UsuarioRequest(string Nome, string Email, string Telefone, string Endereco, TipoPerfil TipoPerfil, string Senha);
record LoginRequest(string Email, string Senha);