using AssetDesk.Api.Application.Abstractions;

namespace AssetDesk.Api.Infrastructure.Identity;

/// <summary>
/// Implementacion provisional de <see cref="ICurrentUser"/> para la etapa 1, cuando todavia
/// no hay autenticacion: toma el actor de la cabecera <c>X-Acting-User</c>.
/// <para>
/// Esto NO es seguridad: cualquiera puede mandar la cabecera que quiera. Existe solo para
/// que la bitacora de auditoria ya funcione de verdad mientras se construye el resto, y se
/// reemplaza por la version basada en claims del JWT en la etapa 2.
/// </para>
/// </summary>
public class HeaderCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public const string HeaderName = "X-Acting-User";
    private const string Fallback = "anonymous";

    public string UserId
    {
        get
        {
            var value = accessor.HttpContext?.Request.Headers[HeaderName].FirstOrDefault();
            return string.IsNullOrWhiteSpace(value) ? Fallback : value.Trim();
        }
    }
}
