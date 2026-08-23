# AssetDesk

API de gestión de activos de TI y tickets de soporte, en ASP.NET Core y SQL Server.

Modela tres reglas que en la vida real se rompen constantemente cuando el sistema no las
obliga, y las obliga desde el dominio:

1. **No se cierra un ticket que tiene subtareas abiertas.**
2. **Un activo dado de baja no acepta tickets nuevos.**
3. **Todo cambio queda registrado con autor y momento**, en una bitácora inmutable por activo
   y por ticket.

> **Estado:** etapa 1 de 7. Lo que está documentado abajo está implementado y corre. El
> [roadmap](#roadmap) lista lo que todavía no existe.

---

## Stack

| Capa | Tecnología |
|---|---|
| API | ASP.NET Core 10 (LTS), controllers |
| Datos | Entity Framework Core 10 + SQL Server 2022, migraciones |
| Base de datos | Contenedor `mcr.microsoft.com/mssql/server:2022-latest` |
| Documentación | OpenAPI (`Microsoft.AspNetCore.OpenApi`) + UI de Swagger |
| Salud | `/health` con chequeo de conectividad a la base |

## Correrlo

Requiere .NET SDK 10 y Docker.

```bash
cp .env.example .env      # y poner una contraseña propia
docker compose up -d      # SQL Server en 127.0.0.1:1433
```

La API lee la cadena de conexión de user-secrets, nunca de un archivo del repo:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Server=127.0.0.1,1433;Database=AssetDesk;User Id=sa;Password=LA_DEL_ENV;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" --project src/AssetDesk.Api
```

`127.0.0.1` y no `localhost`, a propósito. En Windows `localhost` resuelve primero a `::1`, y
el puerto que publica Docker está bindeado solo en IPv4, así que la conexión se va a timeout
con un error 258 que no dice nada útil.

Aplicar el esquema y levantar:

```bash
dotnet dotnet-ef database update --project src/AssetDesk.Api
dotnet run --project src/AssetDesk.Api
```

Swagger queda en la raíz. Mientras no haya autenticación, el actor de cada operación se manda
en la cabecera `X-Acting-User`; es lo que aparece como autor en la bitácora.

## Arquitectura

```
Controllers  →  Services  →  Repositories  →  EF Core  →  SQL Server
                    ↓
                 Domain (entidades con las reglas adentro)
```

Las decisiones que vale la pena señalar:

**Las reglas de negocio viven en las entidades, no en los servicios.** `Asset` y `Ticket`
tienen setters privados y toda mutación pasa por un método que valida y deja rastro. Un
servicio que solo carga, delega y confirma no puede dejar una entidad en estado inválido.
Efecto secundario útil: las reglas se testean sin levantar base de datos.

**Los enums se guardan como texto, no como int.** La tabla se lee sin diccionario, y reordenar
un enum en C# no reinterpreta en silencio las filas ya grabadas.

**Dos códigos de error distintos para dos preguntas distintas.** Las DataAnnotations validan la
forma del request y dan `400`. El dominio valida el estado y da `409`. "Está mal escrito" y "el
sistema no lo permite ahora" no son el mismo error.

**Concurrencia optimista con `rowversion`.** Dos peticiones editando el mismo activo: la
segunda falla con `409` en vez de sobrescribir a ciegas.

**Las operaciones con efectos tienen su propia ruta.** `POST /assignment`,
`POST /decommission`, `PUT /status` en lugar de un `PATCH` genérico. Cada una arrastra efectos
distintos y escribe un renglón distinto en la bitácora; la intención del cliente tiene que
estar en la URL, no adivinarse de qué campos cambiaron.

**Dar de baja no es `DELETE`.** La fila se conserva porque el historial y los tickets que la
referencian tienen que seguir existiendo.

**El reloj se inyecta (`TimeProvider`).** No hay `DateTime.UtcNow` disperso por el código, así
que los tests pueden fijar el tiempo y las aserciones sobre timestamps son deterministas.

**Un seam para la identidad.** Los servicios dependen de `ICurrentUser`, no de JWT. Hoy la
implementación lee una cabecera HTTP; cuando entre la autenticación real se cambia esa clase y
ni los servicios ni las entidades se enteran.

## Modelo

**Activo** — ciclo de vida `InStock → Assigned → InRepair`, con `Decommissioned` como estado
terminal. Cada transición escribe en `AssetHistory`.

**Ticket** — máquina de estados explícita:

```
New ──→ InProgress ──→ Resolved ──→ Closed
         ↕                 │
       OnHold              └──→ InProgress   (reabrir)

New / InProgress / OnHold ──→ Cancelled

Closed y Cancelled son terminales.
```

Un ticket puede colgar de otro como subtarea, un solo nivel de profundidad. Reabrir un ticket
resuelto limpia `ResolvedAtUtc`: si no, cualquier métrica de tiempo de resolución miente.

## Roadmap

Etapas 2 a 7, todavía **no implementadas**: autenticación JWT con roles, pruebas con xUnit,
CI en GitHub Actions, métricas Prometheus, dashboards de Grafana, logs centralizados con Loki,
alertas, y SLIs/SLOs documentados.
