namespace WebApp.SlackIntegration.Services;

public class SubscribeService([FromKeyedServices("SlackAppService")]SlackAppService slackAppService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await slackAppService.SendSlackMessage("T0812FA86PM", "C09PWUD1WR0", "hi");
            
            await Task.Delay(3000, stoppingToken);
        }
    }
}