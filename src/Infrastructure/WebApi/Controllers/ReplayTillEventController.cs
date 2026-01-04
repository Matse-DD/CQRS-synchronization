using Application.WebApi;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.WebApi.Controllers;

public class ReplayTillEventController(
    IUseCase<ReplayTillEventInput, Task> replayTillEvent
    ) : IController
{   
    public static Task Invoke()
    {

    }
}
