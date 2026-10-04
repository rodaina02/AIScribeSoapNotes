using Ehr.Application.Abstractions.Pipeline;
using Ehr.Application.DTOs.Encounters;
using Ehr.Application.DTOs.Pipeline;
using Ehr.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ehr.Api.Controllers;

[ApiController]
[Route("api/encounters")]
public sealed class EncountersController(
    IEncounterService encounters,
    IEncounterAiPipelineService encounterPipeline) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EncounterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EncounterResponse>> GetById(
        [FromRoute] Guid id,
        [FromHeader(Name = "X-Tenant-Id")] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId is not { } tid || tid == Guid.Empty)
        {
            return BadRequest("A non-empty X-Tenant-Id header is required.");
        }

        var dto = await encounters.GetByIdAsync(tid, id, cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        return Ok(dto);
    }

    /// <summary>Step 8 pipeline: clean → SOAP → coding → persist <c>soap_notes</c> and AI <c>encounter_medical_codes</c>.</summary>
    [HttpPost("{id:guid}/ai-pipeline")]
    [ProducesResponseType(typeof(EncounterPipelineResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EncounterPipelineResultDto>> RunAiPipeline(
        [FromRoute] Guid id,
        [FromHeader(Name = "X-Tenant-Id")] Guid? tenantId,
        [FromBody] RunEncounterPipelineBody body,
        CancellationToken cancellationToken)
    {
        if (tenantId is not { } tid || tid == Guid.Empty)
        {
            return BadRequest("A non-empty X-Tenant-Id header is required.");
        }

        if (string.IsNullOrWhiteSpace(body.RawConversation))
        {
            return BadRequest("RawConversation is required.");
        }

        try
        {
            var result = await encounterPipeline.RunAsync(
                new EncounterPipelineRequest
                {
                    TenantId = tid,
                    EncounterId = id,
                    RawConversation = body.RawConversation,
                },
                cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
