using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using TestNest.Admin.API.Helpers;
using TestNest.Admin.Application.Contracts.Interfaces.Service;
using TestNest.Admin.Application.Specifications.EstablishmentMemberSpecifications;
using TestNest.Admin.SharedLibrary.Common.Results;
using TestNest.Admin.SharedLibrary.Dtos.Paginations;
using TestNest.Admin.SharedLibrary.Dtos.Requests.Establishment;
using TestNest.Admin.SharedLibrary.Dtos.Responses.Establishments;
using TestNest.Admin.SharedLibrary.Exceptions.Common;
using TestNest.Admin.SharedLibrary.StronglyTypeIds;

namespace TestNest.Admin.API.Controllers;

/// <summary>
/// API endpoints for managing establishment members.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EstablishmentMembersController(
    IEstablishmentMemberService establishmentMemberService,
    IErrorResponseService errorResponseService) : ControllerBase
{
    private readonly IEstablishmentMemberService _establishmentMemberService = establishmentMemberService;
    private readonly IErrorResponseService _errorResponseService = errorResponseService;

    /// <summary>
    /// Creates a new establishment member.
    /// </summary>
    /// <param name="creationRequest">The data for creating the establishment member.</param>
    /// <returns>The newly created establishment member.</returns>
    /// <response code="201">Returns the newly created <see cref="EstablishmentMemberResponse"/>.</response>
    /// <response code="400">If the provided establishment member data is invalid.</response>
    /// <response code="409">If creating the member results in a conflict (e.g., the employee is already a member of this establishment).</response>
    /// <response code="500">If an internal server error occurs during the creation process.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(EstablishmentMemberResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateEstablishmentMember(
        [FromBody] EstablishmentMemberForCreationRequest creationRequest)
    {
        Result<EstablishmentMemberResponse> result = await _establishmentMemberService.CreateEstablishmentMemberAsync(creationRequest);

        if (result.IsSuccess)
        {
            EstablishmentMemberResponse? dto = result.Value;
            return CreatedAtAction(nameof(GetEstablishmentMembers), new { establishmentMemberId = dto.EstablishmentMemberId }, dto);
        }

        return HandleErrorResponse(result.ErrorType, result.Errors);
    }

    /// <summary>
    /// Updates an existing establishment member.
    /// </summary>
    /// <param name="establishmentMemberId">The ID of the establishment member to update.</param>
    /// <param name="updateRequest">The updated data for the establishment member.</param>
    /// <returns>Returns the updated establishment member.</returns>
    /// <response code="200">Returns the updated <see cref="EstablishmentMemberResponse"/> if the update was successful.</response>
    /// <response code="400">If the provided <paramref name="establishmentMemberId"/> or the updated data is invalid.</response>
    /// <response code="404">If the establishment member with the given <paramref name="establishmentMemberId"/> is not found.</response>
    /// <response code="409">If updating the member results in a conflict (e.g., the employee is already a member of this establishment).</response>
    /// <response code="401">If attempting to update the EstablishmentId to a different value than the existing member.</response>
    /// <response code="500">If an internal server error occurs during the update process.</response>
    [HttpPut("{establishmentMemberId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EstablishmentMemberResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateEstablishmentMember(
        string establishmentMemberId,
        [FromBody] EstablishmentMemberForUpdateRequest updateRequest)
    {
        Result<EstablishmentMemberId> memberIdResult = IdHelper.ValidateAndCreateId<EstablishmentMemberId>(establishmentMemberId);
        if (!memberIdResult.IsSuccess)
        {
            return HandleErrorResponse(memberIdResult.ErrorType, memberIdResult.Errors);
        }

        Result<EstablishmentMemberResponse> updatedMember = await _establishmentMemberService.UpdateEstablishmentMemberAsync(memberIdResult.Value!, updateRequest);

        if (updatedMember.IsSuccess)
        {
            return Ok(updatedMember.Value!);
        }

        return HandleErrorResponse(updatedMember.ErrorType, updatedMember.Errors);
    }

    /// <summary>
    /// Partially updates an existing establishment member.
    /// </summary>
    /// <param name="establishmentMemberId">The ID of the establishment member to update.</param>
    /// <param name="patchDocument">A JSON Patch document containing the operations to apply to the establishment member.</param>
    /// <returns>Returns the updated establishment member.</returns>
    /// <response code="200">Returns the updated <see cref="EstablishmentMemberResponse"/> if the update was successful.</response>
    /// <response code="400">If the provided <paramref name="establishmentMemberId"/> is invalid, the patch document is invalid, or if the validation of the patched entity fails.</response>
    /// <response code="404">If the establishment member with the given <paramref name="establishmentMemberId"/> is not found.</response>
    /// <response code="409">If updating the member results in a conflict (e.g., the employee is already a member of this establishment).</response>
    /// <response code="500">If an internal server error occurs during the update process.</response>
    [HttpPatch("{establishmentMemberId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EstablishmentMemberResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PatchEstablishmentMember(
        string establishmentMemberId,
        [FromBody] JsonPatchDocument<EstablishmentMemberPatchRequest> patchDocument)
    {
        Result<EstablishmentMemberId> memberIdResult = IdHelper.ValidateAndCreateId<EstablishmentMemberId>(establishmentMemberId);
        if (!memberIdResult.IsSuccess)
        {
            return HandleErrorResponse(memberIdResult.ErrorType, memberIdResult.Errors);
        }

        var patchRequest = new EstablishmentMemberPatchRequest();
        patchDocument.ApplyTo(patchRequest);

        if (!TryValidateModel(patchRequest))
        {
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error("ValidationError", e.ErrorMessage))
                .ToList();
            return HandleErrorResponse(ErrorType.Validation, validationErrors);
        }

        Result<EstablishmentMemberResponse> patchedMember = await _establishmentMemberService.PatchEstablishmentMemberAsync(memberIdResult.Value!, patchRequest);

        if (patchedMember.IsSuccess)
        {
            return Ok(patchedMember.Value!);
        }

        return HandleErrorResponse(patchedMember.ErrorType, patchedMember.Errors);
    }

    /// <summary>
    /// Deletes an establishment member.
    /// </summary>
    /// <param name="establishmentMemberId">The ID of the establishment member to delete.</param>
    /// <returns>Returns an empty response if the deletion was successful.</returns>
    /// <response code="204">If the establishment member was successfully deleted.</response>
    /// <response code="400">If the provided <paramref name="establishmentMemberId"/> is invalid.</response>
    /// <response code="404">If the establishment member with the given <paramref name="establishmentMemberId"/> is not found.</response>
    /// <response code="500">If an internal server error occurs during the deletion process.</response>
    [HttpDelete("{establishmentMemberId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteEstablishmentMember(string establishmentMemberId)
    {
        Result<EstablishmentMemberId> memberIdResult = IdHelper.ValidateAndCreateId<EstablishmentMemberId>(establishmentMemberId);
        if (!memberIdResult.IsSuccess)
        {
            return HandleErrorResponse(memberIdResult.ErrorType, memberIdResult.Errors);
        }

        Result result = await _establishmentMemberService.DeleteEstablishmentMemberAsync(memberIdResult.Value!);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return HandleErrorResponse(result.ErrorType, result.Errors);
    }

    /// <summary>
    /// Retrieves a paginated list of establishment members based on specified query parameters.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of members to retrieve per page (default: 10).</param>
    /// <param name="sortBy">The field to sort the members by (default: "Id").</param>
    /// <param name="sortOrder">The sort order ("asc" for ascending, "desc" for descending, default: "asc").</param>
    /// <param name="establishmentMemberId">Filter by establishment member ID.</param>
    /// <param name="establishmentId">Filter by establishment ID.</param>
    /// <param name="employeeId">Filter by employee ID.</param>
    /// <param name="memberTitle">Filter by member title.</param>
    /// <param name="memberDescription">Filter by member description.</param>
    /// <param name="memberTag">Filter by member tag.</param>
    /// <returns>A paginated list of establishment members.</returns>
    /// <response code="200">Returns a paginated list of <see cref="EstablishmentMemberResponse"/>.</response>
    /// <response code="400">If the provided query parameters are invalid.</response>
    /// <response code="404">If no establishment members are found based on the provided criteria.</response>
    /// <response code="500">If an internal server error occurs during the retrieval process.</response>
    [HttpGet(Name = "GetEstablishmentMembers")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<EstablishmentMemberResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEstablishmentMembers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "Id",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string? establishmentMemberId = null,
        [FromQuery] string? establishmentId = null, // EstablishmentId as a query parameter
        [FromQuery] string? employeeId = null,
        [FromQuery] string? memberTitle = null,
        [FromQuery] string? memberDescription = null,
        [FromQuery] string? memberTag = null)
    {
        if (!string.IsNullOrEmpty(establishmentMemberId))
        {
            Result<EstablishmentMemberId> memberIdResult = IdHelper
                .ValidateAndCreateId<EstablishmentMemberId>(establishmentMemberId);
            if (!memberIdResult.IsSuccess)
            {
                return HandleErrorResponse(memberIdResult.ErrorType, memberIdResult.Errors);
            }

            Result<EstablishmentMemberResponse> singleMemberResult = await _establishmentMemberService.GetEstablishmentMemberByIdAsync(memberIdResult.Value!);

            return singleMemberResult.IsSuccess && singleMemberResult.Value != null
                ? Ok(singleMemberResult.Value)
                : NotFound();
        }
        else
        {
            var spec = new EstablishmentMemberSpecification(
                pageNumber: pageNumber,
                pageSize: pageSize,
                sortBy: sortBy,
                sortOrder: sortOrder,
                establishmentId: establishmentId,
                employeeId: employeeId,
                memberTitle: memberTitle,
                memberDescription: memberDescription,
                memberTag: memberTag
            );

            Result<int> countResult = await _establishmentMemberService.CountAsync(spec);
            if (!countResult.IsSuccess)
            { return HandleErrorResponse(countResult.ErrorType, countResult.Errors); }
            int totalCount = countResult.Value;

            Result<IEnumerable<EstablishmentMemberResponse>> membersResult = await _establishmentMemberService.ListAsync(spec);
            if (!membersResult.IsSuccess)
            { return HandleErrorResponse(membersResult.ErrorType, membersResult.Errors); }
            IEnumerable<EstablishmentMemberResponse> responseData = membersResult.Value!;

            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var paginatedResponse = new PaginatedResponse<EstablishmentMemberResponse>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                Data = responseData,
                Links = new PaginatedLinks
                {
                    First = GenerateMemberPaginationLink(1, pageSize, sortBy, sortOrder, establishmentMemberId, establishmentId, employeeId, memberTitle, memberDescription, memberTag),
                    Last = totalPages > 0 ? GenerateMemberPaginationLink(totalPages, pageSize, sortBy, sortOrder, establishmentMemberId, establishmentId, employeeId, memberTitle, memberDescription, memberTag) : null,
                    Next = pageNumber < totalPages ? GenerateMemberPaginationLink(pageNumber + 1, pageSize, sortBy, sortOrder, establishmentMemberId, establishmentId, employeeId, memberTitle, memberDescription, memberTag) : null,
                    Previous = pageNumber > 1 ? GenerateMemberPaginationLink(pageNumber - 1, pageSize, sortBy, sortOrder, establishmentMemberId, establishmentId, employeeId, memberTitle, memberDescription, memberTag) : null
                }
            };
            return Ok(paginatedResponse);
        }
    }

    private string GenerateMemberPaginationLink(
        int targetPageNumber,
        int pageSize,
        string sortBy,
        string sortOrder,
        string? establishmentMemberId = null,
        string? establishmentId = null,
        string? employeeId = null,
        string? memberTitle = null,
        string? memberDescription = null,
        string? memberTag = null)
        => Url.Action(
            nameof(GetEstablishmentMembers),
            "EstablishmentMembers",
            new
            {
                pageNumber = targetPageNumber,
                pageSize,
                sortBy,
                sortOrder,
                establishmentMemberId,
                establishmentId,
                employeeId,
                memberTitle,
                memberDescription,
                memberTag
            },
            protocol: Request.Scheme
        )!;

    private IActionResult HandleErrorResponse(ErrorType errorType, IEnumerable<Error> errors)
    {
        List<Error> safeErrors = errors?.ToList() ?? new List<Error>();

        return errorType switch
        {
            ErrorType.Validation =>
                _errorResponseService.CreateProblemDetails(StatusCodes.Status400BadRequest, "Validation Error", "Validation failed.", new Dictionary<string, object> { { "errors", safeErrors } }),

            ErrorType.NotFound =>
                _errorResponseService.CreateProblemDetails(StatusCodes.Status404NotFound, "Not Found", "Resource not found.", new Dictionary<string, object> { { "errors", safeErrors } }),

            ErrorType.Conflict =>
                _errorResponseService.CreateProblemDetails(StatusCodes.Status409Conflict, "Conflict", "Resource conflict.", new Dictionary<string, object> { { "errors", safeErrors } }),

            ErrorType.Unauthorized =>
                _errorResponseService.CreateProblemDetails(StatusCodes.Status401Unauthorized, "Unauthorized", "You are not authorized to perform this action.", new Dictionary<string, object> { { "errors", safeErrors } }),

            _ => _errorResponseService.CreateProblemDetails(StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };
    }
}
