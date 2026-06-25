using Microsoft.AspNetCore.Mvc;

namespace JNPF.API.Entry.Controllers;

[ApiController]
[Route("api/log")]
public class LogController : ControllerBase
{
    [HttpPost("frontend-error")]
    public IActionResult LogFrontendError([FromBody] List<FrontendErrorEntry> errors)
    {
        foreach (var error in errors)
        {
            Serilog.Log.Warning(
                "[Frontend] Source={Source} | Message={Message} | TraceId={TraceId} | URL={URL}",
                error.Source, error.Message, error.TraceId ?? "none", error.Url);
        }
        return Ok(new { code = 200 });
    }
}

public class FrontendErrorEntry
{
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? Stack { get; set; }
    public string? TraceId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
}
