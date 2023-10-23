using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PlatformService.AsyncDataServices;
using PlatformService.Data;
using PlatformService.Dtos;
using PlatformService.Models;
using PlatformService.SyncDataServices.Http;

namespace PlatformService.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class PlatformsController : ControllerBase
  {
    private readonly IPlatformRepo _repository;
    private readonly IMapper _mapper;
    private readonly ICommandDataClient _commandDateClient;
    private readonly IMessageBusClient _messageBusClient;

    public PlatformsController(IPlatformRepo repository, IMapper mapper, ICommandDataClient commandDataClient, IMessageBusClient messageBusClient)
    {
      _repository = repository;
      _mapper = mapper;
      _commandDateClient = commandDataClient;
      _messageBusClient = messageBusClient;
    }

    [HttpGet]
    public ActionResult<IEnumerable<PlatformReadDto>> GetPlatforms()
    {
      Console.WriteLine("--> Getting Platforms");

      var allPlatforms = _repository.GetAllPlatForms();

      return Ok(_mapper.Map<IEnumerable<PlatformReadDto>>(allPlatforms));

    }

    [HttpGet("{id}", Name = "GetPlatformById")]
    public ActionResult<PlatformReadDto> GetPlatformById(int id)
    {
      var platformItem = _repository.GetPlatformById(id);

      if (platformItem != null)
      {
        return Ok(_mapper.Map<PlatformReadDto>(platformItem));
      }

      return NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<PlatformReadDto>> CreatePlatform(PlatformCreateDto platformRequest)
    {

      var platformItem = _mapper.Map<Platform>(platformRequest);

      _repository.CreatePlatform(platformItem);
      _repository.SaveChanges();

      PlatformReadDto platformReadDto = _mapper.Map<PlatformReadDto>(platformItem);

      // send sync message =
      try
      {
        await _commandDateClient.SendPlatformToCommand(platformReadDto);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"--> Could not send synchronously: {ex.Message}");
      }

      // send Async message
      try
      {
        var platformPublishedDto = _mapper.Map<PlatformPublishedDto>(platformReadDto);

        platformPublishedDto.Event = "Platform.Published";

        _messageBusClient.PublishNewPlatform(platformPublishedDto);
      }
      catch (Exception ex)
      {

        Console.WriteLine($"--> Could not send synchronously: {ex.Message}");
      }
      return CreatedAtRoute(nameof(GetPlatformById), new { platformReadDto.Id }, platformReadDto);

    }
  }
}