using NotificacionesService.DTOs;
using NotificacionesService.Models;

namespace NotificacionesService.Services;

/// <summary>
/// Contrato del servicio de correo electrónico.
/// Cualquier implementación (SMTP real, mock para pruebas) debe
/// implementar esta interfaz.
/// </summary>
public interface ICorreoService
{
    /// <summary>
    /// Envía la notificación al familiar cuando se crea una solicitud
    /// de referencia médica (RF-10 del proyecto).
    /// </summary>
    Task<RegistroEnvio> EnviarNotificacionSolicitudAsync(NotificacionSolicitudDto dto);

    /// <summary>
    /// Envía un recordatorio de cita médica al familiar.
    /// </summary>
    Task<RegistroEnvio> EnviarRecordatorioCitaAsync(NotificacionRecordatorioCitaDto dto);

    /// <summary>
    /// Envía un correo genérico con contenido HTML personalizado.
    /// Para casos especiales no cubiertos por los otros métodos.
    /// </summary>
    Task<RegistroEnvio> EnviarCorreoGenericoAsync(NotificacionGenericaDto dto);

    /// <summary>
    /// Verifica si el servidor SMTP está disponible y accesible.
    /// Usado por el endpoint de health check.
    /// </summary>
    Task<bool> VerificarConexionSmtpAsync();
}
