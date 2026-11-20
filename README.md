# LogiMarket 360

Aplicación empresarial de escritorio para ventas y logística, construida con .NET 8, Windows Forms, EF Core y SQLite. Aplica Clean Architecture, autorización en Application, persistencia transaccional y los 23 patrones GoF en flujos verificables.

## Requisitos y estructura

- Windows 10/11 y SDK de .NET 8 o posterior compatible.
- `LogiMarket360.Domain`: entidades, invariantes y patrones puros.
- `LogiMarket360.Application`: casos de uso, puertos, Commands y Facade.
- `LogiMarket360.Infrastructure`: EF Core/SQLite, repositorios y adaptadores.
- `LogiMarket360.WinForms`: composición DI e interfaz.
- `LogiMarket360.Tests`: pruebas unitarias e integración SQLite aislada.

Dirección: `WinForms → Application → Domain`; Infrastructure implementa puertos de Application/Domain.

## Restaurar, compilar, probar y ejecutar

```powershell
dotnet restore LogiMarket360.slnx
dotnet build LogiMarket360.slnx --no-restore -m:1
dotnet test LogiMarket360.slnx --no-build -m:1
dotnet run --project LogiMarket360.WinForms/LogiMarket360.WinForms.csproj
```

La matriz actual ejecuta 65 pruebas. La aplicación crea y migra automáticamente `%LOCALAPPDATA%\LogiMarket360\logimarket360.db`; nunca se versionan archivos `*.db`.

## Cuentas de demostración

| Usuario | Contraseña | Rol |
|---|---|---|
| `admin` | `admin123` | ADMIN |
| `ventas` | `ventas123` | VENTAS |
| `bodega` | `bodega123` | BODEGA |
| `consulta` | `consulta123` | CONSULTA |

Las contraseñas se almacenan con hash y salt; estas cuentas contienen datos ficticios.

## Demostración recomendada

1. Iniciar como `ventas` y crear para Ana `KIT_TELETRABAJO ×1`, estándar.
2. Confirmar: USD 59.00; P01/P02/P03 disminuyen una unidad.
3. Como `bodega`, despachar y entregar.
4. Como `ventas`, devolver por defecto: reembolso USD 54.00.
5. Consultar inventario, pagos, auditoría, métricas y comprobantes.

## Restablecimiento seguro

Cerrar la aplicación y eliminar únicamente `%LOCALAPPDATA%\LogiMarket360\logimarket360.db`. La siguiente ejecución recrea la base con migraciones y seed idempotente. No elimine directorios amplios ni edite tablas manualmente.

## Integridad

- Cobro, stock, pedido y Outbox se guardan transaccionalmente.
- Pago/pedido tienen claves únicas de idempotencia.
- `Producto.Version` habilita concurrencia optimista; P36 impide sobreventa.
- Devolución única y reintegro único por pedido.
- P37 recupera eventos pendientes tras reiniciar sin duplicar pagos.
