# Sistema de Control de Accesos - Parking

API REST para gestión de entrada y salida de vehículos en estacionamiento.

## Tecnologías

- .NET 9
- Entity Framework Core con SQL Server
- MediatR (patrón CQRS)
- FluentValidation
- xUnit + Moq (testing)

## Requisitos

- .NET 9 SDK
- SQL Server (Express o superior)
- Visual Studio 2022 / VS Code / Rider

## Estructura del Proyecto

```
Parking/
|-- API/
|   |-- Controllers/
|   |-- Middleware/
|-- Application/
|   |-- Commands/
|   |-- Queries/
|   |-- Validators/
|   |-- Behaviors/
|-- Domain/
|   |-- Entities/
|   |-- Exceptions/
|   |-- Interfaces/
|-- Infrastructure/
    |-- Data/
    |-- Repositories/

Parking.Tests/
|-- ProcessAccessCommandHandlerTests.cs
|-- ConcurrencyTests.cs
```

## Configuración

1. Clonar el repositorio

2. Configurar la cadena de conexión en `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ParkingDB;Trusted_Connection=true;TrustServerCertificate=true"
  }
}
```

3. Ejecutar migraciones:

```bash
cd Parking
dotnet ef database update
```

4. Ejecutar la aplicación:

```bash
dotnet run
```

La API estará disponible en `http://localhost:5206`

## Reglas de Negocio

| # | Regla | Código de Error |
|---|-------|-----------------|
| 1 | Un vehículo no puede entrar si ya está dentro | `VEHICLE_ALREADY_INSIDE` |
| 2 | Un vehículo no puede salir si no está dentro | `VEHICLE_NOT_INSIDE` |
| 3 | Un usuario solo puede tener un vehículo activo | `USER_HAS_ACTIVE_VEHICLE` |
| 4 | El sistema soporta operaciones concurrentes | `CONCURRENCY_CONFLICT` |
| 5 | Todo intento queda registrado en auditoría | - |

## Endpoints

### POST /api/access

Registrar entrada o salida de un vehículo.

**Request:**
```json
{
  "vehiclePlate": "ABC123",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "accessType": 0,
  "timestamp": "2025-01-16T10:00:00Z"
}
```

- `accessType`: `0` = Entrada, `1` = Salida

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Entrada registrada exitosamente",
  "logId": "guid"
}
```

**Response (409 Conflict):**
```json
{
  "error": "El vehículo ABC123 ya se encuentra dentro del estacionamiento",
  "code": "VEHICLE_ALREADY_INSIDE",
  "statusCode": 409
}
```

### GET /api/access/vehicle/{plate}/status

Consultar el estado actual de un vehículo.

**Response:**
```json
{
  "id": "guid",
  "plate": "ABC123",
  "isInside": true,
  "lastEntry": "2025-01-16T10:00:00Z",
  "lastExit": null,
  "currentUserId": "guid"
}
```

### GET /api/access/audit

Obtener logs de auditoría con paginación.

**Query params:** `skip` (default: 0), `take` (default: 50)

**Response:**
```json
[
  {
    "id": "guid",
    "vehiclePlate": "ABC123",
    "userId": "guid",
    "accessType": 0,
    "timestamp": "2025-01-16T10:00:00Z",
    "success": true,
    "failureReason": null,
    "createdAt": "2025-01-16T10:00:00Z"
  }
]
```

## Testing

Ejecutar las pruebas unitarias:

```bash
cd Parking.Tests
dotnet test
```

### Cobertura de Tests

| Clase | Tests |
|-------|-------|
| ProcessAccessCommandHandlerTests | 7 |
| ConcurrencyTests | 2 |

## Decisiones Técnicas

### CQRS con MediatR
Separación entre operaciones de lectura (Queries) y escritura (Commands). Facilita el testing y la escalabilidad.

### Unit of Work + Repository Pattern
Abstracción de la capa de datos que permite transacciones atómicas y facilita el mocking en tests.

### Optimistic Concurrency
Control de concurrencia mediante `RowVersion` en la entidad `Vehicle`. Detecta conflictos cuando dos operaciones intentan modificar el mismo registro simultáneamente.

### Manejo Centralizado de Errores
Middleware global que captura excepciones de dominio y las transforma en respuestas HTTP consistentes.

### FluentValidation
Validación de requests desacoplada de los handlers, integrada en el pipeline de MediatR.

## Supuestos

- Las placas de vehículos son identificadores únicos
- Los IDs de usuario vienen pre-validados desde un sistema de autenticación externo
- Los timestamps se manejan en UTC
- El sistema opera en una única zona horaria

## Mejoras Futuras

- Autenticación y autorización con JWT
- Rate limiting para prevenir abuso
- Cache distribuido con Redis
- Notificaciones en tiempo real con SignalR
- Contenedorización con Docker
- Pipeline CI/CD
