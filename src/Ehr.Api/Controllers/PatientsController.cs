using Ehr.Application.DTOs.Encounters;
using Ehr.Application.DTOs.Patients;
using Ehr.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ehr.Api.Controllers;

[ApiController]
[Route("api/patients")]
public sealed class PatientsController(IPatientService patients, IEncounterService encounters) : ControllerBase
{
    /// <summary>Active patients for the documentation session picker.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PatientSessionOptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PatientSessionOptionDto>>> ListForSession(
        CancellationToken cancellationToken)
    {
        var items = await patients.ListForSessionPickerAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientResponse>> GetById(
        [FromRoute] Guid id,
        [FromHeader(Name = "X-Tenant-Id")] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId is not { } tid || tid == Guid.Empty)
        {
            return BadRequest("A non-empty X-Tenant-Id header is required.");
        }

        var dto = await patients.GetByIdAsync(tid, id, cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        return Ok(dto);
    }

    [HttpGet("{id:guid}/visits")]
    [ProducesResponseType(typeof(IReadOnlyList<EncounterHistoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EncounterHistoryItemDto>>> ListVisits(
        [FromRoute] Guid id,
        [FromHeader(Name = "X-Tenant-Id")] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId is not { } tid || tid == Guid.Empty)
        {
            return BadRequest("A non-empty X-Tenant-Id header is required.");
        }

        try
        {
            var items = await encounters.ListHistoryForPatientAsync(tid, id, cancellationToken);
            return Ok(items);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/visits")]
    [ProducesResponseType(typeof(Ehr.Application.DTOs.Pipeline.EncounterPipelineResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Ehr.Application.DTOs.Pipeline.EncounterPipelineResultDto>> CreateVisit(
        [FromRoute] Guid id,
        [FromHeader(Name = "X-Tenant-Id")] Guid? tenantId,
        [FromBody] CreatePatientVisitRequest body,
        CancellationToken cancellationToken)
    {
        if (tenantId is not { } tid || tid == Guid.Empty)
        {
            return BadRequest("A non-empty X-Tenant-Id header is required.");
        }

        try
        {
            var result = await encounters.CreateVisitAndDocumentAsync(tid, id, body, cancellationToken);
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
