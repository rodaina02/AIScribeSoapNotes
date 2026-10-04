using Ehr.Application.DTOs.Soap;
using Ehr.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ehr.Api.Controllers;

[ApiController]
[Route("api/soap")]
public sealed class SoapController(ISoapNoteService soapNotes) : ControllerBase
{
    /// <summary>GET SOAP note by id (tenant from X-Tenant-Id header).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SoapNoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SoapNoteResponse>> GetById(
        [FromRoute] Guid id,
        [FromHeader(Name = "X-Tenant-Id")] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId is not { } tid || tid == Guid.Empty)
        {
            return BadRequest("A non-empty X-Tenant-Id header is required.");
        }

        var dto = await soapNotes.GetByIdAsync(tid, id, cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        return Ok(dto);
    }
}
