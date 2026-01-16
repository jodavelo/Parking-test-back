# Sistema de Control de Accesos - Parking

API REST para gestión de entrada y salida de vehículos en estacionamiento.

## Tecnologías

- .NET 9
- Entity Framework Core (SQL Server)
- MediatR (CQRS)
- FluentValidation

## Arquitectura

```
Parking/
??? API/                    # Capa de presentación
?   ??? Controllers/        # Controladores REST
?   ??? Middleware/         # Manejo de errores
??? Application/            # Capa de aplicación (CQRS)
?   ??? Commands/           # Comandos (escritura)
?   ??? Queries/            # Queries (lectura)
?   ??? Validators/         # Validaciones
?   ??? Behaviors/          # Pipeline MediatR
??? Domain/                 # Capa de dominio
?   ??? Entities/           # Entidades
?   ??? Exceptions/         # Excepciones de negocio
?   ??? Interfaces/         # Contratos
??? Infrastructure/         # Capa de infraestructura
    ??? Data/               # DbContext
    ??? Repositories/       # Implementación de repositorios
```

## Reglas de Negocio

1. Un vehículo no puede entrar si ya está dentro
2. Un vehículo no puede salir si no está dentro
3. Un usuario solo puede tener un vehículo activo
4. El sistema soporta concurrencia (optimistic locking)
5. Todo intento queda registrado en auditoría

## Configuración

### Base de datos

Editar `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ParkingDB;Trusted_Connection=true;TrustServerCertificate=true"
  }
}
```

### Ejecutar

```bash
dotnet run
```

La base de datos se crea automáticamente en desarrollo.

## Endpoints

### POST /api/access
Procesar entrada o salida de vehículo.

```json
{
  "vehiclePlate": "ABC123",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "accessType": 0,
  "timestamp": "2025-01-16T10:00:00Z"
}
```

- `accessType`: 0 = Entry, 1 = Exit

### GET /api/access/vehicle/{plate}/status
Obtener estado actual de un vehículo.

### GET /api/access/audit
Obtener logs de auditoría.

Query params: `skip`, `take`

## Decisiones Técnicas

- **CQRS con MediatR**: Separación clara entre comandos y queries
- **Unit of Work**: Transacciones atómicas
- **Optimistic Locking**: Control de concurrencia con RowVersion
- **FluentValidation**: Validación desacoplada
- **Middleware de errores**: Respuestas consistentes

## Mejoras Futuras

- Autenticación JWT
- Rate limiting
- Cache con Redis
- Eventos de dominio
- Tests unitarios e integración
