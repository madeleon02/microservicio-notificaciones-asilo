using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using NotificacionesService.DTOs;
using NotificacionesService.Models;

namespace NotificacionesService.Services;


public class CorreoService : ICorreoService
{
    private readonly ConfiguracionCorreo _config;
    private readonly ILogger<CorreoService> _logger;

    public CorreoService(IConfiguration configuration, ILogger<CorreoService> logger)
    {
        _config = configuration.GetSection("ConfiguracionCorreo")
                               .Get<ConfiguracionCorreo>()
                  ?? new ConfiguracionCorreo();
        _logger = logger;

        _logger.LogInformation(
            "CorreoService inicializado | Modo: {Modo} | SMTP: {Smtp}:{Puerto}",
            _config.ModoDebug ? "DEBUG (sin envío real)" : "PRODUCCIÓN",
            _config.ServidorSmtp,
            _config.Puerto);
    }

 
    public async Task<RegistroEnvio> EnviarNotificacionSolicitudAsync(NotificacionSolicitudDto dto)
    {
        var registro = new RegistroEnvio { CorreoDestino = dto.CorreoFamiliar };

        try
        {
            string asunto = $"[Asilo Cabeza de Algodón] Actualización médica de {dto.NombreInterno}";
            string cuerpoHtml = GenerarHtmlSolicitud(dto);
            string cuerpoTexto = GenerarTextoSolicitud(dto);

            registro.Asunto = asunto;
            await EnviarAsync(dto.CorreoFamiliar, dto.NombreFamiliar, asunto, cuerpoHtml, cuerpoTexto, registro);

            _logger.LogInformation(
                "Notificación de solicitud enviada | IdSolicitud: {Id} | Familiar: {Correo} | Modo: {Modo}",
                dto.IdSolicitud, dto.CorreoFamiliar, _config.ModoDebug ? "debug" : "real");
        }
        catch (Exception ex)
        {
            registro.Exitoso = false;
            registro.Error = ex.Message;
            _logger.LogError(ex, "Error al enviar notificación de solicitud {Id}", dto.IdSolicitud);
        }

        return registro;
    }


    public async Task<RegistroEnvio> EnviarRecordatorioCitaAsync(NotificacionRecordatorioCitaDto dto)
    {
        var registro = new RegistroEnvio { CorreoDestino = dto.CorreoFamiliar };

        try
        {
            string asunto = $"[Asilo Cabeza de Algodón] Recordatorio de cita — {dto.NombreInterno}";
            string cuerpoHtml = GenerarHtmlRecordatorio(dto);
            string cuerpoTexto = $"Recordatorio: {dto.NombreInterno} tiene cita con {dto.NombreMedico} " +
                                 $"({dto.Especialidad}) el {dto.FechaCita:dd/MM/yyyy HH:mm}.";

            registro.Asunto = asunto;
            await EnviarAsync(dto.CorreoFamiliar, dto.NombreFamiliar, asunto, cuerpoHtml, cuerpoTexto, registro);
        }
        catch (Exception ex)
        {
            registro.Exitoso = false;
            registro.Error = ex.Message;
            _logger.LogError(ex, "Error al enviar recordatorio de cita para {Interno}", dto.NombreInterno);
        }

        return registro;
    }

   
    public async Task<RegistroEnvio> EnviarCorreoGenericoAsync(NotificacionGenericaDto dto)
    {
        var registro = new RegistroEnvio
        {
            CorreoDestino = dto.DestinatarioCorreo,
            Asunto = dto.Asunto
        };

        try
        {
            await EnviarAsync(dto.DestinatarioCorreo, dto.DestinatarioNombre,
                              dto.Asunto, dto.MensajeHtml, dto.MensajeTexto, registro);
        }
        catch (Exception ex)
        {
            registro.Exitoso = false;
            registro.Error = ex.Message;
            _logger.LogError(ex, "Error al enviar correo genérico a {Correo}", dto.DestinatarioCorreo);
        }

        return registro;
    }

    public async Task<bool> VerificarConexionSmtpAsync()
    {
        if (_config.ModoDebug)
        {
            _logger.LogDebug("Verificación SMTP omitida — modo debug activo");
            return true;
        }

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_config.ServidorSmtp, _config.Puerto,
                SecureSocketOptions.StartTls);
            await client.DisconnectAsync(true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMTP no disponible: {Smtp}:{Puerto}", _config.ServidorSmtp, _config.Puerto);
            return false;
        }
    }

    
    private async Task EnviarAsync(string correoDestino, string nombreDestino,
        string asunto, string cuerpoHtml, string cuerpoTexto, RegistroEnvio registro)
    {
        
        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(_config.NombreRemitente, _config.CorreoRemitente));
        mensaje.To.Add(new MailboxAddress(nombreDestino, correoDestino));
        mensaje.Subject = asunto;

        
        var builder = new BodyBuilder
        {
            HtmlBody = cuerpoHtml,
            TextBody = cuerpoTexto
        };
        mensaje.Body = builder.ToMessageBody();

        if (_config.ModoDebug)
        {
            
            _logger.LogInformation(
                "═══ [MODO DEBUG — CORREO NO ENVIADO] ═══\n" +
                "  Para:    {Nombre} <{Correo}>\n" +
                "  Asunto:  {Asunto}\n" +
                "  Cuerpo:  {Texto}",
                nombreDestino, correoDestino, asunto, cuerpoTexto);

            registro.Exitoso = true;
            registro.FueEnvioReal = false;
        }
        else
        {
           
            using var client = new SmtpClient();
            client.Timeout = _config.TimeoutSegundos * 1000;

            await client.ConnectAsync(_config.ServidorSmtp, _config.Puerto,
                SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_config.Usuario, _config.Contrasena);
            await client.SendAsync(mensaje);
            await client.DisconnectAsync(true);

            registro.Exitoso = true;
            registro.FueEnvioReal = true;

            _logger.LogInformation(
                "Correo enviado exitosamente a {Correo} — Asunto: {Asunto}",
                correoDestino, asunto);
        }
    }

    

    private string GenerarHtmlSolicitud(NotificacionSolicitudDto dto)
    {
        string estadoBadge = dto.EstadoSolicitud switch
        {
            "pendiente" => "<span style='background:#FFF9E0;color:#7d5a00;padding:3px 10px;border-radius:4px;font-size:12px;font-weight:bold;'>PENDIENTE DE ASIGNACIÓN</span>",
            "asignada"  => "<span style='background:#E2EFDA;color:#375623;padding:3px 10px;border-radius:4px;font-size:12px;font-weight:bold;'>CITA ASIGNADA</span>",
            _           => "<span style='background:#DEEAF1;color:#1F4E79;padding:3px 10px;border-radius:4px;font-size:12px;font-weight:bold;'>" + dto.EstadoSolicitud.ToUpper() + "</span>",
        };

        string seccionCita = dto.FechaCita.HasValue
            ? $@"<tr>
                   <td style='padding:8px;background:#E2EFDA;font-weight:bold;color:#375623;width:40%'>Fecha y hora de cita</td>
                   <td style='padding:8px;background:#F5FAF2;color:#375623;font-weight:bold;'>{dto.FechaCita.Value:dddd dd/MM/yyyy HH:mm}</td>
                 </tr>"
            : $@"<tr>
                   <td style='padding:8px;background:#FFF9E0;font-weight:bold;color:#7d5a00;width:40%'>Fecha de cita</td>
                   <td style='padding:8px;background:#FFFDF0;color:#7d5a00;'>Pendiente de asignación por la Fundación</td>
                 </tr>";

        string seccionMedicoEsp = !string.IsNullOrEmpty(dto.NombreMedicoEspecialista)
            ? $@"<tr>
                   <td style='padding:8px;background:#F2F2F2;font-weight:bold;color:#595959;width:40%'>Médico especialista</td>
                   <td style='padding:8px;'>{dto.NombreMedicoEspecialista}</td>
                 </tr>"
            : $@"<tr>
                   <td style='padding:8px;background:#F2F2F2;font-weight:bold;color:#595959;width:40%'>Médico especialista</td>
                   <td style='padding:8px;color:#999;'>Por asignar</td>
                 </tr>";

        return $@"<!DOCTYPE html>
<html lang='es'>
<head>
  <meta charset='UTF-8'>
  <meta name='viewport' content='width=device-width,initial-scale=1'>
  <title>Notificación Médica — Asilo Cabeza de Algodón</title>
</head>
<body style='margin:0;padding:0;font-family:Arial,sans-serif;background:#f0f4f8;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f0f4f8;'>
    <tr><td align='center' style='padding:30px 10px;'>

      <!-- Contenedor principal -->
      <table width='600' cellpadding='0' cellspacing='0'
             style='background:#ffffff;border-radius:8px;overflow:hidden;
                    box-shadow:0 2px 12px rgba(0,0,0,0.1);'>

        <!-- ENCABEZADO -->
        <tr>
          <td style='background:#1F4E79;padding:28px 30px;'>
            <p style='margin:0;color:#BDD7EE;font-size:12px;letter-spacing:1px;'>
              ASILO DE ANCIANOS
            </p>
            <h1 style='margin:4px 0 0;color:#ffffff;font-size:22px;font-weight:bold;'>
              Cabeza de Algodón
            </h1>
            <p style='margin:8px 0 0;color:#BDD7EE;font-size:13px;'>
              Notificación de Referencia Médica
            </p>
          </td>
        </tr>

        <!-- SALUDO -->
        <tr>
          <td style='padding:28px 30px 16px;'>
            <p style='margin:0;color:#1A1A1A;font-size:15px;'>
              Estimado/a <strong>{dto.NombreFamiliar}</strong>,
            </p>
            <p style='margin:12px 0 0;color:#595959;font-size:14px;line-height:1.6;'>
              Le informamos que el equipo médico del Asilo de Ancianos Cabeza de Algodón
              ha generado una <strong>solicitud de referencia médica</strong> para
              su familiar <strong>{dto.NombreInterno}</strong>.
            </p>
          </td>
        </tr>

        <!-- ESTADO -->
        <tr>
          <td style='padding:0 30px 20px;'>
            <p style='margin:0 0 8px;font-size:13px;color:#595959;'>Estado de la solicitud:</p>
            {estadoBadge}
          </td>
        </tr>

        <!-- DETALLE DE LA SOLICITUD -->
        <tr>
          <td style='padding:0 30px 24px;'>
            <p style='margin:0 0 10px;font-size:14px;font-weight:bold;color:#1F4E79;
                      border-bottom:2px solid #2E75B6;padding-bottom:6px;'>
              Detalle de la solicitud
            </p>
            <table width='100%' cellpadding='0' cellspacing='1' style='border-collapse:collapse;font-size:13px;'>
              <tr>
                <td style='padding:8px;background:#F2F2F2;font-weight:bold;color:#595959;width:40%'>Paciente</td>
                <td style='padding:8px;'>{dto.NombreInterno}</td>
              </tr>
              <tr>
                <td style='padding:8px;background:#F2F2F2;font-weight:bold;color:#595959;'>Especialidad requerida</td>
                <td style='padding:8px;'><strong>{dto.EspecialidadRequerida}</strong></td>
              </tr>
              <tr>
                <td style='padding:8px;background:#F2F2F2;font-weight:bold;color:#595959;'>Motivo de la referencia</td>
                <td style='padding:8px;'>{dto.MotivoReferencia}</td>
              </tr>
              <tr>
                <td style='padding:8px;background:#F2F2F2;font-weight:bold;color:#595959;'>Médico que refiere</td>
                <td style='padding:8px;'>Dr./Dra. {dto.NombreMedicoGeneral}</td>
              </tr>
              {seccionMedicoEsp}
              <tr>
                <td style='padding:8px;background:#F2F2F2;font-weight:bold;color:#595959;'>Enfermero/a asignado/a</td>
                <td style='padding:8px;'>{dto.NombreEnfermero ?? "Por asignar"}</td>
              </tr>
              {seccionCita}
              <tr>
                <td style='padding:8px;background:#F2F2F2;font-weight:bold;color:#595959;'>Número de solicitud</td>
                <td style='padding:8px;color:#2E75B6;font-weight:bold;'>#{dto.IdSolicitud:D6}</td>
              </tr>
            </table>
          </td>
        </tr>

        <!-- NOTA INFORMATIVA -->
        <tr>
          <td style='padding:0 30px 24px;'>
            <div style='background:#DEEAF1;border-left:4px solid #2E75B6;
                        padding:12px 16px;border-radius:0 4px 4px 0;'>
              <p style='margin:0;font-size:13px;color:#1F4E79;line-height:1.5;'>
                <strong>Información importante:</strong> Si la cita aún no tiene fecha asignada,
                la Fundación la confirmará próximamente. Puede comunicarse con
                la administración del asilo para consultar el estado de la solicitud
                referenciando el número <strong>#{dto.IdSolicitud:D6}</strong>.
              </p>
            </div>
          </td>
        </tr>

        <!-- FOOTER -->
        <tr>
          <td style='background:#1F4E79;padding:20px 30px;'>
            <p style='margin:0;color:#BDD7EE;font-size:12px;line-height:1.6;'>
              Este correo fue generado automáticamente por el Sistema de Gestión
              del Asilo de Ancianos Cabeza de Algodón.<br>
              Por favor no responda a este mensaje.<br>
              Para consultas, comuníquese directamente con la administración del asilo.
            </p>
            <p style='margin:10px 0 0;color:#BDD7EE;font-size:11px;'>
              © 2025 Asilo de Ancianos Cabeza de Algodón — Guatemala
            </p>
          </td>
        </tr>

      </table>
    </td></tr>
  </table>
</body>
</html>";
    }

    private string GenerarTextoSolicitud(NotificacionSolicitudDto dto)
    {
        return $"""
            Asilo de Ancianos Cabeza de Algodón — Notificación de Referencia Médica
            ================================================================

            Estimado/a {dto.NombreFamiliar},

            Se ha generado una solicitud de referencia médica para su familiar.

            DETALLE:
            - Paciente:             {dto.NombreInterno}
            - Especialidad:         {dto.EspecialidadRequerida}
            - Motivo:               {dto.MotivoReferencia}
            - Médico que refiere:   {dto.NombreMedicoGeneral}
            - Médico especialista:  {dto.NombreMedicoEspecialista ?? "Por asignar"}
            - Enfermero/a:          {dto.NombreEnfermero ?? "Por asignar"}
            - Fecha de cita:        {(dto.FechaCita.HasValue ? dto.FechaCita.Value.ToString("dd/MM/yyyy HH:mm") : "Pendiente")}
            - No. de solicitud:     #{dto.IdSolicitud:D6}
            - Estado:               {dto.EstadoSolicitud.ToUpper()}

            Para consultas, comuníquese con la administración del asilo.

            © 2025 Asilo de Ancianos Cabeza de Algodón — Guatemala
            """;
    }

    private string GenerarHtmlRecordatorio(NotificacionRecordatorioCitaDto dto)
    {
        return $@"<!DOCTYPE html>
<html lang='es'>
<head><meta charset='UTF-8'><title>Recordatorio de Cita</title></head>
<body style='font-family:Arial,sans-serif;background:#f0f4f8;margin:0;padding:20px;'>
  <table width='600' style='margin:auto;background:#fff;border-radius:8px;overflow:hidden;'>
    <tr><td style='background:#1F4E79;padding:24px;'>
      <h1 style='color:#fff;margin:0;font-size:20px;'>Recordatorio de Cita Médica</h1>
      <p style='color:#BDD7EE;margin:4px 0 0;font-size:13px;'>Asilo de Ancianos Cabeza de Algodón</p>
    </td></tr>
    <tr><td style='padding:24px;'>
      <p style='color:#1A1A1A;'>Estimado/a <strong>{dto.NombreFamiliar}</strong>,</p>
      <p style='color:#595959;'>Le recordamos que <strong>{dto.NombreInterno}</strong>
         tiene una cita médica programada:</p>
      <div style='background:#E2EFDA;border-left:4px solid #375623;padding:16px;border-radius:0 4px 4px 0;margin:16px 0;'>
        <p style='margin:0;color:#375623;font-size:16px;font-weight:bold;'>
          📅 {dto.FechaCita:dddd dd/MM/yyyy} — {dto.FechaCita:HH:mm}
        </p>
        <p style='margin:6px 0 0;color:#375623;'>
          Médico: Dr./Dra. {dto.NombreMedico} — {dto.Especialidad}
        </p>
        <p style='margin:4px 0 0;color:#375623;font-size:13px;'>
          Lugar: {dto.LugarAtencion}
        </p>
      </div>
      <p style='color:#595959;font-size:13px;'>
        Por favor asegúrese de que su familiar llegue acompañado del enfermero asignado.
      </p>
    </td></tr>
    <tr><td style='background:#1F4E79;padding:16px 24px;'>
      <p style='color:#BDD7EE;font-size:12px;margin:0;'>
        Asilo de Ancianos Cabeza de Algodón — Guatemala © 2025
      </p>
    </td></tr>
  </table>
</body>
</html>";
    }
}
