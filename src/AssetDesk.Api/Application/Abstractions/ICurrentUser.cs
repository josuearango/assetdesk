namespace AssetDesk.Api.Application.Abstractions;

/// <summary>
/// Quien esta ejecutando la peticion. El dominio necesita este dato para poder registrar
/// "quien hizo que", pero no tiene por que saber de donde sale.
/// <para>
/// En la etapa 1 la implementacion lee una cabecera HTTP. En la etapa 2 pasa a leer los
/// claims del JWT y ni los servicios ni las entidades cambian una linea. Ese es el punto
/// de tener la interfaz: el dominio no depende del mecanismo de autenticacion.
/// </para>
/// </summary>
public interface ICurrentUser
{
    string UserId { get; }
}
