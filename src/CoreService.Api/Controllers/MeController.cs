using CoreService.Api.Common;
using CoreService.Application.UseCases.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreService.Api.Controllers;

[ApiController]
[Route("me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    private readonly GetMeUseCase _getMe;

    public MeController(GetMeUseCase getMe)
    {
        _getMe = getMe;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _getMe.ExecuteAsync(ct);
        return result.ToActionResult(this);
    }
}