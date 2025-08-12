namespace ZephyrScaleServerExporter.Client.Exceptions;

public class ApiException(
    string message,
    int? statusCode = null,
    string? responseContent = null,
    Exception? innerException = null)
    : Exception(message, innerException)
{
    public int? StatusCode { get; } = statusCode;
    public string? ResponseContent { get; } = responseContent;
}