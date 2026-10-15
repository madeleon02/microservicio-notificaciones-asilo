namespace NotificacionesService.Models;

/// <summary>
/// Configuración SMTP mapeada desde appsettings.json
/// Sección: ConfiguracionCorreo
/// </summary>
public class ConfiguracionCorreo
{
    public string ServidorSmtp { get; set; } = string.Empty;
    public int Puerto { get; set; } = 587;
    public bool UsarSsl { get; set; } = true;
    public string CorreoRemitente { get; set; } = string.Empty;
    public string NombreRemitente { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public int TimeoutSegundos { get; set; } = 30;

    /// <summary>
    /// Si es true, los correos NO se envían realmente.
    /// Solo se registra en el log lo que se hubiera enviado.
    /// Ideal para desarrollo y pruebas.
    /// </summary>
    public bool ModoDebug { get; set; } = true;
}

/// <summary>
/// Registro interno de un intento de envío de correo
/// Para auditoría y trazabilidad
/// </summary>
public class RegistroEnvio
{
    public string IdRastreo { get; set; } = Guid.NewGuid().ToString();
    public string CorreoDestino { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public bool Exitoso { get; set; }
    public string? Error { get; set; }
    public DateTime FechaIntento { get; set; } = DateTime.UtcNow;
    public bool FueEnvioReal { get; set; }
}
