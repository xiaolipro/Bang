using System.Text;
using Fake.ExceptionHandling;

namespace Microsoft.Extensions.Logging;

public static class FakeLoggerExtensions
{
    public static void LogException(this ILogger logger, Exception exception)
    {
        var logLevel = exception.GetLogLevel();

        logger.LogException(exception, logLevel);
    }

    public static void LogException(this ILogger logger, Exception exception, LogLevel logLevel)
    {
        logger.LogWithLevel(logLevel, exception.Message, exception);

        LogKnownProperties(logger, exception, logLevel);
        LogData(logger, exception, logLevel);
    }

    private static void LogData(ILogger logger, Exception exception, LogLevel logLevel)
    {
        if (exception.Data.Count > 0)
        {
            return;
        }
        
        var dataBuilder = new StringBuilder();
        dataBuilder.AppendLine("---------- Exception Data ----------");
        foreach (var key in exception.Data.Keys)
        {
            dataBuilder.AppendLine($"{key} = {exception.Data[key]}");
        }
        
        logger.LogWithLevel(logLevel, dataBuilder.ToString());
    }

    private static void LogKnownProperties(ILogger logger, Exception exception, LogLevel logLevel)
    {
        if (exception is IHasErrorCode exceptionWithErrorCode)
        {
            logger.LogWithLevel(logLevel, "Code:" + exceptionWithErrorCode.Code);
        }
        
        if (exception is IHasErrorDetails exceptionWithErrorDetails)
        {
            logger.LogWithLevel(logLevel, "Details:" + exceptionWithErrorDetails.Details);
        }
    }


    public static void LogWithLevel(this ILogger logger, LogLevel logLevel, string message, Exception exception)
    {
        switch (logLevel)
        {
            case LogLevel.Critical:
                logger.LogCritical(exception, message);
                break;
            case LogLevel.Error:
                logger.LogError(exception, message);
                break;
            case LogLevel.Warning:
                logger.LogWarning(exception, message);
                break;
            case LogLevel.Information:
                logger.LogInformation(exception, message);
                break;
            case LogLevel.Debug:
                logger.LogDebug(exception, message);
                break;
            case LogLevel.Trace:
            case LogLevel.None:
            default: // LogLevel.Trace || LogLevel.None
                logger.LogTrace(exception, message);
                break;
        }
    }

    public static void LogWithLevel(this ILogger logger, LogLevel logLevel, string message)
    {
        switch (logLevel)
        {
            case LogLevel.Critical:
                logger.LogCritical(message);
                break;
            case LogLevel.Error:
                logger.LogError(message);
                break;
            case LogLevel.Warning:
                logger.LogWarning(message);
                break;
            case LogLevel.Information:
                logger.LogInformation(message);
                break;
            case LogLevel.Debug:
                logger.LogDebug(message);
                break;
            case LogLevel.Trace:
            case LogLevel.None:
            default: // LogLevel.Trace || LogLevel.None
                logger.LogTrace(message);
                break;
        }
    }
}