using System.Net;
using System.Text.Json;

namespace NotificacionesService.Middleware;

/// <summary>
/// Middleware que captura todas las excepciones no controladas
/// y las convierte en respuestas JSON con formato estándar.
/// Así el backend Node.js siempre recibe una respuesta coherente.
/// </summary>
public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlerMiddleware> _logger;

    public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error no controlado en {Metodo} {Ruta}",
                context.Request.Method,
                context.Request.Path);

            await ManejarErrorAsync(context, ex);
        }
    }

    private static async Task ManejarErrorAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, mensaje) = ex switch
        {
            ArgumentNullException    => (HttpStatusCode.BadRequest, "Datos requeridos no proporcionados"),
            ArgumentException        => (HttpStatusCode.BadRequest, ex.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "No autorizado"),
            _                        => (HttpStatusCode.InternalServerError, "Error interno del servidor")
        };

        context.Response.StatusCode = (int)statusCode;

        var respuesta = new
        {
            exitoso  = false,
            mensaje,
            error    = ex.Message,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(respuesta, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        );
    }
}
