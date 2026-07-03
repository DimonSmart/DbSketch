namespace DimonSmart.DbSketch.Core.Schema;

public sealed class DatabaseConnectionException(string message, Exception innerException) : Exception(message, innerException);
