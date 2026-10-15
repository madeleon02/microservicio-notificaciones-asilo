namespace NotificacionesService.DTOs;

// ─── REQUEST DTOs ─────────────────────────────────────────────────────────────

/// <summary>
/// Datos necesarios para notificar al familiar cuando se crea
/// una solicitud de referencia médica (RF-10 del proyecto).
/// El backend Node.js envía este objeto al crear una solicitud.
/// </summary>
public class NotificacionSolicitudDto
{
    /// <summary>Nombre completo del familiar que recibirá el correo</summary>
    public string NombreFamiliar { get; set; } = string.Empty;

    /// <summary>Correo electrónico del familiar (destino del mensaje)</summary>
    public string CorreoFamiliar { get; set; } = string.Empty;

    /// <summary>Nombre completo del interno del asilo</summary>
    public string NombreInterno { get; set; } = string.Empty;

    /// <summary>Especialidad médica a la que fue referido el interno</summary>
    public string EspecialidadRequerida { get; set; } = string.Empty;

    /// <summary>Motivo por el que el médico general creó la referencia</summary>
    public string MotivoReferencia { get; set; } = string.Empty;

    /// <summary>Nombre del médico general que creó la solicitud</summary>
    public string NombreMedicoGeneral { get; set; } = string.Empty;

    /// <summary>Nombre del médico especialista asignado (puede ser nulo si aún no se asignó)</summary>
    public string? NombreMedicoEspecialista { get; set; }

    /// <summary>Nombre del enfermero asignado para acompañar al interno</summary>
    public string? NombreEnfermero { get; set; }

    /// <summary>Fecha y hora de la cita asignada (nulo si aún está pendiente)</summary>
    public DateTime? FechaCita { get; set; }

    /// <summary>ID de la solicitud en el sistema principal (para trazabilidad)</summary>
    public int IdSolicitud { get; set; }

    /// <summary>Estado actual de la solicitud</summary>
    public string EstadoSolicitud { get; set; } = "pendiente";
}

/// <summary>
/// Notificación genérica para casos especiales (cambio de estado,
/// recordatorios de cita, etc.)
/// </summary>
public class NotificacionGenericaDto
{
    public string DestinatarioNombre { get; set; } = string.Empty;
    public string DestinatarioCorreo { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string MensajeHtml { get; set; } = string.Empty;
    public string MensajeTexto { get; set; } = string.Empty;
}

/// <summary>
/// Notificación de recordatorio de cita médica
/// </summary>
public class NotificacionRecordatorioCitaDto
{
    public string NombreFamiliar { get; set; } = string.Empty;
    public string CorreoFamiliar { get; set; } = string.Empty;
    public string NombreInterno { get; set; } = string.Empty;
    public string NombreMedico { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public DateTime FechaCita { get; set; }
    public string LugarAtencion { get; set; } = "Clínica de la Fundación";
}

// ─── RESPONSE DTOs ────────────────────────────────────────────────────────────

/// <summary>
/// Respuesta estándar del microservicio de notificaciones
/// </summary>
public class RespuestaNotificacionDto
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string? IdRastreo { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DetalleEnvioDto? DetalleEnvio { get; set; }
}

public class DetalleEnvioDto
{
    public string CorreoDestino { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public bool EnviadoReal { get; set; }
    public string Modo { get; set; } = string.Empty; // "real" o "debug"
}

/// <summary>
/// Estado de salud del microservicio
/// </summary>
public class HealthCheckDto
{
    public string Estado { get; set; } = "ok";
    public string Servicio { get; set; } = "NotificacionesService";
    public string Version { get; set; } = "1.0.0";
    public string Proyecto { get; set; } = "Sistema de Gestión — Asilo de Ancianos Cabeza de Algodón";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool SmtpDisponible { get; set; }
    public string ModoActual { get; set; } = string.Empty;
}
