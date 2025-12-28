using Grpc.Core;

namespace Storages.DataPlane.Services;

public class KvService : DataPlane.KvService.KvServiceBase
{
    public override async Task<ReadReply> Read(ReadRequest request, ServerCallContext context)
    {
        var instanceId = request.InstanceId;
        var key = request.Key;
        
        
        
        return new ReadReply();
    }
}