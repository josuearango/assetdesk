namespace AssetDesk.Api.Domain.Exceptions;

/// <summary>No existe la entidad pedida. El middleware la traduce a HTTP 404.</summary>
public sealed class NotFoundException(string entity, object id)
    : Exception($"{entity} '{id}' no existe.");
