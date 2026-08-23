namespace AssetDesk.Api.Domain.Exceptions;

/// <summary>
/// Se viola una regla de negocio. El middleware la traduce a HTTP 409 Conflict:
/// la peticion estaba bien formada, pero el estado del dominio no la permite.
/// </summary>
public sealed class DomainRuleException(string message) : Exception(message);
