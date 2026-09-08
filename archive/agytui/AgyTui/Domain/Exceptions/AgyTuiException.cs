namespace AgyTui.Domain.Exceptions;

public class AgyTuiException : Exception
{
    public AgyTuiException(string message) : base(message) { }
    public AgyTuiException(string message, Exception innerException) : base(message, innerException) { }
}

public class AccountNotFoundException : AgyTuiException
{
    public AccountNotFoundException(string accountName) : base($"Account '{accountName}' was not found.") { }
}

public class QuotaExceededException : AgyTuiException
{
    public QuotaExceededException(string accountName) : base($"Quota limit reached for account '{accountName}'.") { }
}

public class InvalidConfigurationException : AgyTuiException
{
    public InvalidConfigurationException(string detail) : base($"Invalid application configuration: {detail}") { }
}

public class CommandExecutionException : AgyTuiException
{
    public CommandExecutionException(string commandAlias, string error) : base($"Execution failed for command '{commandAlias}': {error}") { }
}
