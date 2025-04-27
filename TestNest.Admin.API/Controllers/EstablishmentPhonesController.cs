
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using TestNest.Admin.API.Helpers;
using TestNest.Admin.Application.Contracts.Interfaces.Service;
using TestNest.Admin.Application.Specifications.EstablishmentPhoneSpecifications;
using TestNest.Admin.SharedLibrary.Common.Results;
using TestNest.Admin.SharedLibrary.Dtos.Paginations;
using TestNest.Admin.SharedLibrary.Dtos.Requests.Establishment;
using TestNest.Admin.SharedLibrary.Dtos.Responses.Establishments;
using TestNest.Admin.SharedLibrary.Exceptions.Common;
using TestNest.Admin.SharedLibrary.StronglyTypeIds;

namespace TestNest.Admin.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstablishmentPhonesController(
    IEstablishmentPhoneService establishmentPhoneService,
    IErrorResponseService errorResponseService) : ControllerBase
{
    private readonly IEstablishmentPhoneService _establishmentPhoneService = establishmentPhoneService;
    private readonly IErrorResponseService _errorResponseService = errorResponseService;

    /// <summary>
    /// Creates a new phone number for an establishment.
    /// </summary>
    /// <param name="creationRequest">The data for creating the establishment phone number.</param>
    /// <returns>The newly created establishment phone number.</returns>
    /// <response code="201">Returns the newly created <see cref="EstablishmentPhoneResponse"/>.</response>
    /// <response code="400">If the provided establishment phone data is invalid.</response>
    /// <response code="409">If creating the phone number results in a conflict (e.g., the phone number already exists for this establishment).</response>
    /// <response code="500">If an internal server error occurs during the creation process.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(EstablishmentPhoneResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateEstablishmentPhone(
        [FromBody] EstablishmentPhoneForCreationRequest creationRequest)
    {
        Result<EstablishmentPhoneResponse> result = await _establishmentPhoneService
            .CreateEstablishmentPhoneAsync(creationRequest);

        if (result.IsSuccess)
        {
            EstablishmentPhoneResponse dto = result.Value!;
            return CreatedAtAction(
                nameof(GetAllEstablishmentPhones),
                new { establishmentPhoneId = dto.EstablishmentId }, dto);
        }

        return HandleErrorResponse(result.ErrorType, result.Errors);
    }

    /// <summary>
    /// Updates an existing establishment phone number.
    /// </summary>
    /// <param name="establishmentPhoneId">The ID of the establishment phone number to update.</param>
    /// <param name="updateRequest">The updated data for the establishment phone number.</param>
    /// <returns>Returns the updated establishment phone number.</returns>
    /// <response code="200">Returns the updated <see cref="EstablishmentPhoneResponse"/> if the update was successful.</response>
    /// <response code="400">If the provided <paramref name="establishmentPhoneId"/> or the updated data is invalid.</response>
    /// <response code="404">If the establishment phone number with the given <paramref name="establishmentPhoneId"/> is not found.</response>
    /// <response code="409">If updating the phone number results in a conflict (e.g., the phone number already exists for this establishment).</response>
    /// <response code="401">If attempting to update the EstablishmentId to a different value than the existing phone number.</response>
    /// <response code="500">If an internal server error occurs during the update process.</response>
    [HttpPut("{establishmentPhoneId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EstablishmentPhoneResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateEstablishmentPhone(
        string establishmentPhoneId,
        [FromBody] EstablishmentPhoneForUpdateRequest updateRequest)
    {
        Result<EstablishmentPhoneId> phoneIdResult = IdHelper
            .ValidateAndCreateId<EstablishmentPhoneId>(establishmentPhoneId);
        if (!phoneIdResult.IsSuccess)
        {
            return HandleErrorResponse(phoneIdResult.ErrorType, phoneIdResult.Errors);
        }

        Result<EstablishmentPhoneResponse> updatedPhoneResult = await _establishmentPhoneService
            .UpdateEstablishmentPhoneAsync(phoneIdResult.Value!, updateRequest);

        if (updatedPhoneResult.IsSuccess)
        {
            return Ok(updatedPhoneResult.Value);
        }

        return HandleErrorResponse(updatedPhoneResult.ErrorType, updatedPhoneResult.Errors);
    }

    /// <summary>
    /// Partially updates an existing establishment phone number.
    /// </summary>
    /// <param name="establishmentPhoneId">The ID of the establishment phone number to update.</param>
    /// <param name="patchDocument">A JSON Patch document containing the operations to apply to the establishment phone number.</param>
    /// <returns>Returns the updated establishment phone number.</returns>
    /// <response code="200">Returns the updated <see cref="EstablishmentPhoneResponse"/> if the update was successful.</response>
    /// <response code="400">If the provided <paramref name="establishmentPhoneId"/> is invalid, the patch document is invalid, or if the validation of the patched entity fails.</response>
    /// <response code="404">If the establishment phone number with the given <paramref name="establishmentPhoneId"/> is not found.</response>
    /// <response code="409">If updating the phone number results in a conflict (e.g., the phone number already exists for this establishment).</response>
    /// <response code="500">If an internal server error occurs during the update process.</response>
    [HttpPatch("{establishmentPhoneId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EstablishmentPhoneResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PatchEstablishmentPhone(
        string establishmentPhoneId,
        [FromBody] JsonPatchDocument<EstablishmentPhonePatchRequest> patchDocument)
    {
        Result<EstablishmentPhoneId> phoneIdResult = IdHelper.ValidateAndCreateId<EstablishmentPhoneId>(establishmentPhoneId);
        if (!phoneIdResult.IsSuccess)
        {
            return HandleErrorResponse(phoneIdResult.ErrorType, phoneIdResult.Errors);
        }

        var patchRequest = new EstablishmentPhonePatchRequest();
        patchDocument.ApplyTo(patchRequest);

        if (!TryValidateModel(patchRequest))
        {
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error("ValidationError", e.ErrorMessage))
                .ToList();
            return HandleErrorResponse(ErrorType.Validation, validationErrors);
        }

        Result<EstablishmentPhoneResponse> patchedPhoneResult = await _establishmentPhoneService.PatchEstablishmentPhoneAsync(phoneIdResult.Value!, patchRequest);

        if (patchedPhoneResult.IsSuccess)
        {
            return Ok(patchedPhoneResult.Value);
        }

        return HandleErrorResponse(patchedPhoneResult.ErrorType, patchedPhoneResult.Errors);
    }

    /// <summary>
    /// Deletes an establishment phone number.
    /// </summary>
    /// <param name="establishmentPhoneId">The ID of the establishment phone number to delete.</param>
    /// <returns>Returns an empty response if the deletion was successful.</returns>
    /// <response code="204">If the establishment phone number was successfully deleted.</response>
    /// <response code="400">If the provided <paramref name="establishmentPhoneId"/> is invalid.</response>
    /// <response code="404">If the establishment phone number with the given <paramref name="establishmentPhoneId"/> is not found.</response>
    /// <response code="500">If an internal server error occurs during the deletion process.</response>
    [HttpDelete("{establishmentPhoneId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteEstablishmentPhone(string establishmentPhoneId)
    {
        Result<EstablishmentPhoneId> phoneIdResult = IdHelper.ValidateAndCreateId<EstablishmentPhoneId>(establishmentPhoneId);
        if (!phoneIdResult.IsSuccess)
        {
            return HandleErrorResponse(phoneIdResult.ErrorType, phoneIdResult.Errors);
        }

        Result result = await _establishmentPhoneService.DeleteEstablishmentPhoneAsync(phoneIdResult.Value!);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return HandleErrorResponse(result.ErrorType, result.Errors);
    }

    /// <summary>
    /// Retrieves a paginated list of establishment phone numbers based on specified query parameters.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of phone numbers to retrieve per page (default: 10).</param>
    /// <param name="sortBy">The field to sort the phone numbers by (default: "Id").</param>
    /// <param name="sortOrder">The sort order ("asc" for ascending, "desc" for descending, default: "asc").</param>
    /// <param name="establishmentId">Filter by establishment ID.</param>
    /// <param name="establishmentPhoneId">Filter by establishment phone ID.</param>
    /// <param name="phoneNumber">Filter by phone number.</param>
    /// <param name="isPrimary">Filter by whether the phone number is primary.</param>
    /// <returns>A paginated list of establishment phone numbers.</returns>
    /// <response code="200">Returns a paginated list of <see cref="EstablishmentPhoneResponse"/>.</response>
    /// <response code="400">If the provided query parameters are invalid.</response>
    /// <response code="404">If no establishment phone numbers are found based on the provided criteria.</response>
    /// <response code="500">If an internal server error occurs during the retrieval process.</response>
    [HttpGet(Name = "GetEstablishmentPhones")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<EstablishmentPhoneResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllEstablishmentPhones(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string sortBy = "Id",
    [FromQuery] string sortOrder = "asc",
    [FromQuery] string establishmentId = null,
    [FromQuery] string establishmentPhoneId = null,
    [FromQuery] string? phoneNumber = null,
    [FromQuery] bool? isPrimary = null)
    {
        if (!string.IsNullOrEmpty(establishmentPhoneId))
        {
            Result<EstablishmentPhoneId> phoneIdResult = IdHelper
                .ValidateAndCreateId<EstablishmentPhoneId>(establishmentPhoneId);
            if (!phoneIdResult.IsSuccess)
            {
                return HandleErrorResponse(phoneIdResult.ErrorType, phoneIdResult.Errors);
            }

            Result<EstablishmentPhoneResponse> singlePhoneResult = await _establishmentPhoneService.GetEstablishmentPhoneByIdAsync(phoneIdResult.Value!);

            return singlePhoneResult.IsSuccess && singlePhoneResult.Value != null
                ? Ok(singlePhoneResult.Value)
                : NotFound();
        }
        else
        {
            var spec = new EstablishmentPhoneSpecification(
                pageNumber: pageNumber,
                pageSize: pageSize,
                sortBy: sortBy,
                sortOrder: sortOrder,
                establishmentId: establishmentId,
                phoneNumber: phoneNumber,
                isPrimary: isPrimary
            );

            Result<int> countResult = await _establishmentPhoneService.CountAsync(spec);
            if (!countResult.IsSuccess)
            { return HandleErrorResponse(countResult.ErrorType, countResult.Errors); }
            int totalCount = countResult.Value;

            Result<IEnumerable<EstablishmentPhoneResponse>> phonesResult = await _establishmentPhoneService.ListAsync(spec);
            if (!phonesResult.IsSuccess)
            { return HandleErrorResponse(phonesResult.ErrorType, phonesResult.Errors); }
            IEnumerable<EstablishmentPhoneResponse> phones = phonesResult.Value!;

            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var paginatedResponse = new PaginatedResponse<EstablishmentPhoneResponse>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                Data = phones,
                Links = new PaginatedLinks
                {
                    First = GeneratePhonePaginationLink(1, pageSize, sortBy, sortOrder, establishmentId, establishmentPhoneId, phoneNumber, isPrimary),
                    Last = totalPages > 0 ? GeneratePhonePaginationLink(totalPages, pageSize, sortBy, sortOrder, establishmentId, establishmentPhoneId, phoneNumber, isPrimary) : null,
                    Next = pageNumber < totalPages ? GeneratePhonePaginationLink(pageNumber + 1, pageSize, sortBy, sortOrder, establishmentId, establishmentPhoneId, phoneNumber, isPrimary) : null,
                    Previous = pageNumber > 1 ? GeneratePhonePaginationLink(pageNumber - 1, pageSize, sortBy, sortOrder, establishmentId, establishmentPhoneId, phoneNumber, isPrimary) : null
                }
            };
            return Ok(paginatedResponse);
        }
    }

    private string GeneratePhonePaginationLink(
            int targetPageNumber,
            int pageSize,
            string sortBy,
            string sortOrder,
            string establishmentId = null,
            string establishmentPhoneId = null,
            string? phoneNumber = null,
            bool? isPrimary = null)
            => Url.Action(
                nameof(GetAllEstablishmentPhones),
                "EstablishmentPhones",
                new
                {
                    pageNumber = targetPageNumber,
                    pageSize,
                    sortBy,
                    sortOrder,
                    establishmentId,
                    establishmentPhoneId,
                    phoneNumber,
                    isPrimary
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
