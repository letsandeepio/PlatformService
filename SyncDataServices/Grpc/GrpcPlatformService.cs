using AutoMapper;
using Grpc.Core;
using PlatformService.Data;

namespace PlatformService.SyncDataServices.Grpc
{
  public class GrpcPlatformService : GrpcPlatform.GrpcPlatformBase
  {
    private readonly IPlatformRepo _repository;
    private readonly Mapper _mapper;

    public GrpcPlatformService(IPlatformRepo repository, Mapper mapper)
    {
      _repository = repository;
      _mapper = mapper;
    }

    public override Task<PlatformResponse> GetAllPlatforms(GetAllRequest request, ServerCallContext context)
    {
      var response = new PlatformResponse();
      var platforms = _repository.GetAllPlatForms();

      foreach (var platform in platforms)
      {
        response.Platform.Add(_mapper.Map<GrpcPlatformModel>(platform));
      }

      return Task.FromResult(response);
    }
  }
}