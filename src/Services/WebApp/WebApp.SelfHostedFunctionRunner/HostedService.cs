using Microsoft.AspNetCore.SignalR.Client;
using WebApp.FunctionShared;

namespace WebApp.SelfHostedFunctionRunner;

public class HostedService(ILogger<HostedService> logger, IConfiguration configuration)
    : IHostedService, IDisposable
{
    private HubConnection? _connection;

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("HostedService is starting.");

        var appId = configuration.GetValue<int>("AppId");
        var token = configuration.GetValue<string>("Token");

        _connection = new HubConnectionBuilder()
            .WithUrl($"{configuration["MycroCloudHost"]}/_functionExecutionHub", options =>
            {
                options.Headers.Add("app_id", appId.ToString());
                options.Headers.Add("token", token!);
            })
            .WithAutomaticReconnect()
            .Build();

        var executeFunction = ExecuteFunction;
        _connection.On("ExecuteFunction", executeFunction);

        await _connection.StartAsync(stoppingToken);
    }

    private async Task ExecuteFunction(string requestId, Request request, string handler,
        Dictionary<string, string>? env)
    {
        logger.LogInformation($"ExecuteFunction: {requestId}");

        var executor = new JintExecutor();

        var result = executor.Execute(request, handler, env);

        await _connection!.InvokeAsync("ReceiveFunctionExecutionResult", requestId, result);
    }

    public async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("HostedService is stopping.");
        await _connection!.StopAsync(stoppingToken);
    }

    public void Dispose()
    {
        _connection?.DisposeAsync();
    }
}