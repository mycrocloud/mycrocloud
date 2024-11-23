using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR.Client;
using WebApp.FunctionShared;

namespace WebApp.SelfHostedFunctionRunner;

public partial class HostedService(ILogger<HostedService> logger, IConfiguration configuration)
    : IHostedService, IDisposable
{
    private HubConnection? _connection;

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("HostedService is starting.");

        var url = $"{configuration["Url"]}";

        var match = MyRegex().Match(url);
        if (!match.Success)
        {
            throw new Exception("Invalid Url");
        }

        var scheme = match.Groups[1].Value;
        var token = match.Groups[2].Value;
        var domain = match.Groups[3].Value;

        var hubUrl = $"{scheme}://{domain}/functionExecutionHub";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.Headers.Add("token", token!); })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug);
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

    [GeneratedRegex("(https?)://([^@]+)@(.+)")]
    private static partial Regex MyRegex();
}