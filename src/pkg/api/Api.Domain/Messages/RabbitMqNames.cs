namespace Api.Domain.Messages;

public static class RabbitMqNames
{
    public const string BuildQueue = "build_queue";
    public const string BuildEventsExchange = "app.build.events";
    public const string BuildLogsExchange = "app.build.logs";
}
