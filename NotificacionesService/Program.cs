using NotificacionesService.Services;
using NotificacionesService.Middleware;
using Serilog;


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/notificaciones.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Microservicio de Notificaciones — Asilo Cabeza de Algodón",
        Version = "v1",
        Description = "Microservicio responsable del envío de notificaciones " +
                      "por correo electrónico a los familiares de los internos " +
                      "del Asilo de Ancianos Cabeza de Algodón. Se activa cuando el " +
                      "Médico General crea una solicitud de referencia médica."
    });
});


builder.Services.AddSingleton<ICorreoService, CorreoService>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBackend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",    
                "https://nuevavida.com"     
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notificaciones API v1");
        c.RoutePrefix = string.Empty; 
    });
}

app.UseCors("AllowBackend");
//app.UseHttpsRedirection();


app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseAuthorization();
app.MapControllers();

Log.Information("=== Microservicio de Notificaciones iniciado en puerto 5050 ===");
Log.Information("=== Asilo de Ancianos Cabeza de Algodón — Análisis de Sistemas I ===");

app.Run();
