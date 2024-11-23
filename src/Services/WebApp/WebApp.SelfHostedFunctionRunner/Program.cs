using WebApp.SelfHostedFunctionRunner;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<HostedService>();

var host = builder.Build();
host.Run();