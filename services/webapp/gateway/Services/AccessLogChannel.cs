using System.Threading.Channels;
using MycroCloud.WebApp.Gateway.Models;

namespace MycroCloud.WebApp.Gateway.Services;

public class AccessLogChannel
{
    private readonly Channel<AccessLog> _channel = Channel.CreateBounded<AccessLog>(
        new BoundedChannelOptions(1024)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelWriter<AccessLog> Writer => _channel.Writer;
    public ChannelReader<AccessLog> Reader => _channel.Reader;
}
