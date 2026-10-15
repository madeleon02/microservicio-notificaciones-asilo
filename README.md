# Microservicio de Notificaciones — Asilo de Ancianos Cabeza de Algodón

## ¿Qué es este microservicio?

Microservicio independiente desarrollado en **C# con ASP.NET Core 8**, responsable
del envío de notificaciones por correo electrónico a los familiares de los internos
del Sistema de Gestión del Asilo de Ancianos Cabeza de Algodón.

Implementa el **RF-10** del proyecto:
> "Al crear la solicitud, el sistema debe enviar automáticamente un correo
> electrónico al familiar del interno informando el estado del paciente
> y a qué médico fue referido."

---

## Estructura del proyecto

```
NotificacionesService/
├── Controllers/
│   ├── NotificacionesController.cs   ← Endpoints principales
│   └── HealthController.cs           ← Health check
├── DTOs/
│   └── NotificacionDtos.cs           ← Objetos de entrada/salida
├── Middleware/
│   └── ErrorHandlerMiddleware.cs     ← Manejo global de errores
├── Models/
│   └── ConfiguracionCorreo.cs        ← Modelo de configuración SMTP
├── Services/
│   ├── ICorreoService.cs             ← Interfaz del servicio
│   └── CorreoService.cs              ← Implementación con MailKit
├── Program.cs                        ← Punto de entrada
├── appsettings.json                  ← Configuración
├── Dockerfile                        ← Para contenedor Docker
NotificacionesService_Postman.json    ← Colección de pruebas Postman
docker-compose.yml                    ← Para levantar con Docker
```

---

## Requisitos

- Visual Studio 2022 o superior
- .NET 8 SDK
- (Opcional) Docker Desktop para contenedor

---

## Cómo abrir en Visual Studio

1. Abrir Visual Studio 2022
2. `Archivo → Abrir → Proyecto o solución`
3. Seleccionar `NotificacionesService.csproj`
4. Esperar que se restauren los paquetes NuGet automáticamente
5. Presionar `F5` para ejecutar

El microservicio arranca en: **http://localhost:5050**
Swagger UI disponible en: **http://localhost:5050** (modo desarrollo)

---

## Configuración del correo (appsettings.json)

```json
"ConfiguracionCorreo": {
  "ServidorSmtp": "smtp.gmail.com",
  "Puerto": 587,
  "UsarSsl": true,
  "CorreoRemitente": "notificaciones@asiloanuevavida.com",
  "NombreRemitente": "Asilo de Ancianos Cabeza de Algodón",
  "Usuario": "tu_correo@gmail.com",
  "Contrasena": "tu_contrasena_de_aplicacion",
  "ModoDebug": true
}
```

> **ModoDebug = true**: los correos NO se envían realmente.
> Solo se registran en el log. Ideal para desarrollo y pruebas.
>
> **ModoDebug = false**: envío real. Requiere credenciales SMTP válidas.

### Configurar Gmail para envío real

1. Ir a tu cuenta Google → Seguridad
2. Activar "Verificación en 2 pasos"
3. Ir a "Contraseñas de aplicaciones"
4. Crear una nueva contraseña para "Correo - Otro"
5. Usar esa contraseña en el campo `Contrasena` del appsettings

---

## Endpoints disponibles

### GET /api/health
Verifica que el microservicio esté activo. No requiere autenticación.
```json
{
  "estado": "ok",
  "servicio": "NotificacionesService",
  "version": "1.0.0",
  "smtpDisponible": true,
  "modoActual": "debug (sin envío real)"
}
```

### POST /api/notificaciones/solicitud
Notifica al familiar sobre una nueva solicitud de referencia médica.
Requiere header: `X-Api-Key: asilo-nueva-vida-2025-notificaciones-key`

```json
// Request body:
{
  "idSolicitud": 34,
  "nombreFamiliar": "María Elena Ajú",
  "correoFamiliar": "m.aju@gmail.com",
  "nombreInterno": "Carlos Enrique Ajú Pérez",
  "especialidadRequerida": "Neurología",
  "motivoReferencia": "Deterioro cognitivo progresivo",
  "nombreMedicoGeneral": "Dr. Roberto Samayoa",
  "nombreMedicoEspecialista": "Dra. Ana Leticia Ruiz",
  "nombreEnfermero": "Licda. Carmen López",
  "fechaCita": "2025-04-12T10:00:00",
  "estadoSolicitud": "asignada"
}

// Response:
{
  "exitoso": true,
  "mensaje": "Notificación procesada correctamente",
  "idRastreo": "guid-unico",
  "timestamp": "2025-04-10T09:15:00Z",
  "detalleEnvio": {
    "correoDestino": "m.aju@gmail.com",
    "asunto": "[Asilo Cabeza de Algodón] Actualización médica de Carlos Enrique Ajú Pérez",
    "enviadoReal": false,
    "modo": "debug"
  }
}
```

### POST /api/notificaciones/recordatorio-cita
Envía recordatorio de cita médica 24 horas antes.

### POST /api/notificaciones/generico
Correo personalizado para casos especiales.

---

## Cómo probar con Postman

1. Abrir Postman
2. `Import → Upload Files`
3. Seleccionar `NotificacionesService_Postman.json`
4. Ejecutar primero `1. Health Check` para verificar que el servicio está activo
5. Ejecutar `2. Notificar Solicitud de Referencia` para la prueba principal
6. Revisar el log en la consola de Visual Studio para ver el correo simulado

---

## Cómo correr con Docker

```bash
# Desde la carpeta raíz (donde está docker-compose.yml)
docker-compose up --build

# El microservicio queda disponible en http://localhost:5050
# Los logs se guardan en ./logs/
```

---

## Cómo lo consume el backend Node.js

En el proyecto principal (Node.js + Express), al crear una solicitud
de referencia médica, se agrega esta llamada:

```javascript
// services/NotificacionService.js
const axios = require('axios');

async function enviarCorreoFamiliar(familiar, solicitud, medicoGeneral) {
  try {
    const response = await axios.post(
      'http://localhost:5050/api/notificaciones/solicitud',
      {
        idSolicitud: solicitud.id_solicitud,
        nombreFamiliar: familiar.nombres + ' ' + familiar.apellidos,
        correoFamiliar: familiar.correo_electronico,
        nombreInterno: solicitud.interno.nombres + ' ' + solicitud.interno.apellidos,
        especialidadRequerida: solicitud.especialidad_requerida,
        motivoReferencia: solicitud.motivo_referencia,
        nombreMedicoGeneral: medicoGeneral.nombre_completo,
        estadoSolicitud: 'pendiente'
      },
      {
        headers: {
          'X-Api-Key': process.env.NOTIFICACIONES_API_KEY
        },
        timeout: 5000
      }
    );
    console.log('Notificación enviada:', response.data.idRastreo);
  } catch (error) {
    // Si el microservicio falla, NO se cancela la solicitud principal
    console.error('Error en microservicio de notificaciones:', error.message);
  }
}
```

---

## Relación con el proyecto

| Elemento del proyecto | Implementado en este microservicio |
|---|---|
| RF-10 — Correo automático al familiar | ✅ POST /api/notificaciones/solicitud |
| RNF-05 — Disponibilidad 98% | ✅ Health check + Docker restart |
| RN-10 — Envío asíncrono sin bloquear | ✅ async/await + fire-and-forget |
| Seguridad — API Key interna | ✅ Header X-Api-Key en todos los endpoints |

---

*Proyecto: Sistema de Gestión — Asilo de Ancianos Cabeza de Algodón*
*Análisis de Sistemas I — Ing. Angel Atilio Maltez*
*Universidad Mariano Gálvez de Guatemala — 2025*
