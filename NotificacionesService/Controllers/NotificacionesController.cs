using Microsoft.AspNetCore.Mvc;
using NotificacionesService.DTOs;
using NotificacionesService.Services;

namespace NotificacionesService.Controllers;


[ApiController]
[Route("api/notificaciones")]
[Produces("application/json")]
public class NotificacionesController : ControllerBase
{
    private readonly ICorreoService _correoService;
    private readonly ILogger<NotificacionesController> _logger;
    private readonly IConfiguration _config;

    public NotificacionesController(
        ICorreoService correoService,
        ILogger<NotificacionesController> logger,
        IConfiguration config)
    {
        _correoService = correoService;
        _logger = logger;
        _config = config;
    }

    
    private bool ApiKeyValida()
    {
        var claveEsperada = _config["Microservicio:ClaveApi"];
        Request.Headers.TryGetValue("X-Api-Key", out var claveRecibida);
        return claveRecibida == claveEsperada;
    }

    
    [HttpPost("solicitud")]
    [ProducesResponseType(typeof(RespuestaNotificacionDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> NotificarSolicitud(
        [FromBody] NotificacionSolicitudDto dto)
    {
        
        if (!ApiKeyValida())
        {
            _logger.LogWarning("Intento de acceso sin API Key válida desde {IP}",
                HttpContext.Connection.RemoteIpAddress);
            return Unauthorized(new { exitoso = false, mensaje = "API Key inválida o no proporcionada" });
        }

        
        if (string.IsNullOrWhiteSpace(dto.CorreoFamiliar))
            return BadRequest(new { exitoso = false, mensaje = "El correo del familiar es requerido" });

        if (string.IsNullOrWhiteSpace(dto.NombreInterno))
            return BadRequest(new { exitoso = false, mensaje = "El nombre del interno es requerido" });

        _logger.LogInformation(
            "Procesando notificación de solicitud #{Id} para {Interno} → {Familiar}",
            dto.IdSolicitud, dto.NombreInterno, dto.CorreoFamiliar);

        var registro = await _correoService.EnviarNotificacionSolicitudAsync(dto);

        var respuesta = new RespuestaNotificacionDto
        {
            Exitoso    = registro.Exitoso,
            Mensaje    = registro.Exitoso
                         ? "Notificación procesada correctamente"
                         : $"Error al enviar: {registro.Error}",
            IdRastreo  = registro.IdRastreo,
            Timestamp  = registro.FechaIntento,
            DetalleEnvio = new DetalleEnvioDto
            {
                CorreoDestino = dto.CorreoFamiliar,
                Asunto        = registro.Asunto,
                EnviadoReal   = registro.FueEnvioReal,
                Modo          = registro.FueEnvioReal ? "producción" : "debug"
            }
        };

        return Ok(respuesta);
    }

   
    [HttpPost("recordatorio-cita")]
    [ProducesResponseType(typeof(RespuestaNotificacionDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> EnviarRecordatorioCita(
        [FromBody] NotificacionRecordatorioCitaDto dto)
    {
        if (!ApiKeyValida())
            return Unauthorized(new { exitoso = false, mensaje = "API Key inválida" });

        if (string.IsNullOrWhiteSpace(dto.CorreoFamiliar))
            return BadRequest(new { exitoso = false, mensaje = "El correo del familiar es requerido" });

        _logger.LogInformation(
            "Enviando recordatorio de cita para {Interno} — {Fecha}",
            dto.NombreInterno, dto.FechaCita);

        var registro = await _correoService.EnviarRecordatorioCitaAsync(dto);

        return Ok(new RespuestaNotificacionDto
        {
            Exitoso   = registro.Exitoso,
            Mensaje   = registro.Exitoso ? "Recordatorio enviado" : $"Error: {registro.Error}",
            IdRastreo = registro.IdRastreo,
            Timestamp = registro.FechaIntento
        });
    }

    // ══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Envía un correo genérico con contenido HTML personalizado.
    /// Para casos especiales no cubiertos por los otros endpoints.
    /// </summary>
    // ══════════════════════════════════════════════════════════════════════════
    [HttpPost("generico")]
    [ProducesResponseType(typeof(RespuestaNotificacionDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> EnviarCorreoGenerico(
        [FromBody] NotificacionGenericaDto dto)
    {
        if (!ApiKeyValida())
            return Unauthorized(new { exitoso = false, mensaje = "API Key inválida" });

        if (string.IsNullOrWhiteSpace(dto.DestinatarioCorreo) ||
            string.IsNullOrWhiteSpace(dto.Asunto))
            return BadRequest(new { exitoso = false, mensaje = "Correo y asunto son requeridos" });

        var registro = await _correoService.EnviarCorreoGenericoAsync(dto);

        return Ok(new RespuestaNotificacionDto
        {
            Exitoso   = registro.Exitoso,
            Mensaje   = registro.Exitoso ? "Correo enviado" : $"Error: {registro.Error}",
            IdRastreo = registro.IdRastreo,
            Timestamp = registro.FechaIntento
        });
    }
}
