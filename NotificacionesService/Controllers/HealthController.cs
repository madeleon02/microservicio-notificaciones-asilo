using Microsoft.AspNetCore.Mvc;
using NotificacionesService.DTOs;
using NotificacionesService.Services;

namespace NotificacionesService.Controllers;

/// <summary>
/// Endpoint de salud del microservicio.
/// Permite verificar que el servicio está activo y
/// que la conexión SMTP funciona correctamente.
/// </summary>
[ApiController]
[Route("api/health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ICorreoService _correoService;
    private readonly IConfiguration _config;

    public HealthController(ICorreoService correoService, IConfiguration config)
    {
        _correoService = correoService;
        _config = config;
    }

    /// <summary>
    /// Verifica el estado del microservicio y la conectividad SMTP.
    /// No requiere autenticación — es público para monitoreo.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthCheckDto), 200)]
    public async Task<IActionResult> GetHealth()
    {
        var smtpOk = await _correoService.VerificarConexionSmtpAsync();
        var modoDebug = _config.GetValue<bool>("ConfiguracionCorreo:ModoDebug");

        var resultado = new HealthCheckDto
        {
            Estado        = "ok",
            Servicio      = "NotificacionesService",
            Version       = "1.0.0",
            Proyecto      = "Sistema de Gestión — Asilo de Ancianos Cabeza de Algodón",
            Timestamp     = DateTime.UtcNow,
            SmtpDisponible = smtpOk,
            ModoActual    = modoDebug ? "debug (sin envío real)" : "producción"
        };

        return Ok(resultado);
    }
}
