namespace Storages.Core.Exceptions;

public class NotFoundException(string message) : Exception(message);