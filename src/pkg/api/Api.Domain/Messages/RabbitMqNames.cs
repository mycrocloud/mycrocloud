namespace Api.Domain.Messages;

public static class RabbitMqNames
{
    public const string SpaBuildJobQueue = "webapp.spa.build.jobs";
    public const string SpaBuildStatusExchange = "webapp.spa.build.status";
    public const string SpaBuildLogsExchange = "webapp.spa.build.logs";
}
