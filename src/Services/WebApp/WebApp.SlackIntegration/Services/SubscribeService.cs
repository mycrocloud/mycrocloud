namespace WebApp.SlackIntegration.Services;

public class SubscribeService(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        
        var slackAppService = scope.ServiceProvider.GetRequiredService<SlackAppService>();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            //await slackAppService.SendSlackMessage("T0812FA86PM", "C09PWUD1WR0", "hi");
            
            await Task.Delay(5000, stoppingToken);
        }
    }
}