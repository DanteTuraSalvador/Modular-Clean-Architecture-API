using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using TestNest.Admin.API.Helpers;
using TestNest.Admin.Application.Contracts.Interfaces.Service;
using TestNest.Admin.Application.Specifications.EstablishmentContactSpecifications;
using TestNest.Admin.SharedLibrary.Common.Results;
using TestNest.Admin.SharedLibrary.Dtos.Paginations;
using TestNest.Admin.SharedLibrary.Dtos.Requests.Establishment;
using TestNest.Admin.SharedLibrary.Dtos.Responses.Establishments;
using TestNest.Admin.SharedLibrary.Exceptions.Common;
using TestNest.Admin.SharedLibrary.StronglyTypeIds;

namespace TestNest.Admin.API.Controllers;


[ApiController]
[Route("api/[controller]")]
public class EstablishmentContactsController(
    IEstablishmentContactService establishmentContactService,
    IErrorResponseService errorResponseService) : ControllerBase
{
    private readonly IEstablishmentContactService _establishmentContactService = establishmentContactService;
    private readonly IErrorResponseService _errorResponseService = errorResponseService;

    /// <summary>
    /// Creates a new establishment contact.
    /// </summary>
    /// <param name="establishmentContactForCreationRequest">The data for creating the establishment contact.</param>
    /// <returns>The newly created establishment contact.</returns>
    /// <response code="201">Returns the newly created <see cref="EstablishmentContactResponse"/>.</response>
    /// <response code="400">If the provided establishment contact data is invalid.</response>
    /// <response code="409">If creating the contact results in a conflict (e.g., duplicate contact details for the same establishment).</response>
    /// <response code="500">If an internal server error occurs during the creation process.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(EstablishmentContactResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateEstablishmentContact(
        [FromBody] EstablishmentContactForCreationRequest establishmentContactForCreationRequest)
    {
        Result<EstablishmentContactResponse> result = await _establishmentContactService
            .CreateEstablishmentContactAsync(establishmentContactForCreationRequest);

        if (result.IsSuccess)
        {
            return CreatedAtAction(
                nameof(GetAllEstablishmentContacts),
                new { establishmentContactId = result.Value!.EstablishmentContactId }, result.Value);
        }

        return HandleErrorResponse(result.ErrorType, result.Errors);
    }

    /// <summary>
    /// Updates an existing establishment contact.
    /// </summary>
    /// <param name="establishmentContactId">The ID of the establishment contact to update.</param>
    /// <param name="establishmentContactForUpdateRequest">The updated data for the establishment contact.</param>
    /// <returns>Returns the updated establishment contact.</returns>
    /// <response code="200">Returns the updated <see cref="EstablishmentContactResponse"/> if the update was successful.</response>
    /// <response code="400">If the provided <paramref name="establishmentContactId"/> or the updated data is invalid.</response>
    /// <response code="404">If the establishment contact with the given <paramref name="establishmentContactId"/> is not found.</response>
    /// <response code="409">If updating the contact results in a conflict (e.g., duplicate contact details for the same establishment).</response>
    /// <response code="401">If attempting to update the EstablishmentId to a different value than the existing contact.</response>
    /// <response code="500">If an internal server error occurs during the update process.</response>
    [HttpPut("{establishmentContactId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EstablishmentContactResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateEstablishmentContact(
        string establishmentContactId,
        [FromBody] EstablishmentContactForUpdateRequest establishmentContactForUpdateRequest)
    {
        Result<EstablishmentContactId> establishmentContactIdResult = IdHelper
            .ValidateAndCreateId<EstablishmentContactId>(establishmentContactId);
        if (!establishmentContactIdResult.IsSuccess)
        {
            return HandleErrorResponse(establishmentContactIdResult.ErrorType, establishmentContactIdResult.Errors);
        }

        Result<EstablishmentContactResponse> updatedContact = await _establishmentContactService
            .UpdateEstablishmentContactAsync(establishmentContactIdResult.Value!, establishmentContactForUpdateRequest);

        if (updatedContact.IsSuccess)
        {
            return Ok(updatedContact.Value);
        }

        return HandleErrorResponse(updatedContact.ErrorType, updatedContact.Errors);
    }

    /// <summary>
    /// Partially updates an existing establishment contact.
    /// </summary>
    /// <param name="establishmentContactId">The ID of the establishment contact to update.</param>
    /// <param name="patchDocument">A JSON Patch document containing the operations to apply to the establishment contact.</param>
    /// <returns>Returns the updated establishment contact.</returns>
    /// <response code="200">Returns the updated <see cref="EstablishmentContactResponse"/> if the update was successful.</response>
    /// <response code="400">If the provided <paramref name="establishmentContactId"/> is invalid, the patch document is invalid, or if the validation of the patched entity fails.</response>
    /// <response code="404">If the establishment contact with the given <paramref name="establishmentContactId"/> is not found.</response>
    /// <response code="409">If updating the contact results in a conflict (e.g., duplicate contact details for the same establishment).</response>
    /// <response code="500">If an internal server error occurs during the update process.</response>
    [HttpPatch("{establishmentContactId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EstablishmentContactResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PatchEstablishmentContact(
        string establishmentContactId,
        [FromBody] JsonPatchDocument<EstablishmentContactPatchRequest> patchDocument)
    {
        Result<EstablishmentContactId> establishmentContactIdResult = IdHelper
            .ValidateAndCreateId<EstablishmentContactId>(establishmentContactId);
        if (!establishmentContactIdResult.IsSuccess)
        {
            return HandleErrorResponse(establishmentContactIdResult.ErrorType, establishmentContactIdResult.Errors);
        }

        var establishmentContactPatchRequest = new EstablishmentContactPatchRequest();
        patchDocument.ApplyTo(establishmentContactPatchRequest);

        if (!TryValidateModel(establishmentContactPatchRequest))
        {
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error("ValidationError", e.ErrorMessage))
                .ToList();
            return HandleErrorResponse(ErrorType.Validation, validationErrors);
        }

        Result<EstablishmentContactResponse> patchedContact = await _establishmentContactService
            .PatchEstablishmentContactAsync(establishmentContactIdResult.Value!, establishmentContactPatchRequest);

        if (patchedContact.IsSuccess)
        {
            return Ok(patchedContact.Value);
        }

        return HandleErrorResponse(patchedContact.ErrorType, patchedContact.Errors);
    }

    /// <summary>
    /// Deletes an establishment contact.
    /// </summary>
    /// <param name="establishmentContactId">The ID of the establishment contact to delete.</param>
    /// <returns>Returns an empty response if the deletion was successful.</returns>
    /// <response code="204">If the establishment contact was successfully deleted.</response>
    /// <response code="400">If the provided <paramref name="establishmentContactId"/> is invalid.</response>
    /// <response code="404">If the establishment contact with the given <paramref name="establishmentContactId"/> is not found.</response>
    /// <response code="409">If attempting to delete the primary contact for an establishment.</response>
    /// <response code="500">If an internal server error occurs during the deletion process.</response>
    [HttpDelete("{establishmentContactId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteEstablishmentContact(
        string establishmentContactId)
    {
        Result<EstablishmentContactId> establishmentContactIdResult = IdHelper
            .ValidateAndCreateId<EstablishmentContactId>(establishmentContactId);
        if (!establishmentContactIdResult.IsSuccess)
        {
            return HandleErrorResponse(establishmentContactIdResult.ErrorType, establishmentContactIdResult.Errors);
        }

        Result result = await _establishmentContactService
            .DeleteEstablishmentContactAsync(establishmentContactIdResult.Value!);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return HandleErrorResponse(result.ErrorType, result.Errors);
    }

    /// <summary>
    /// Retrieves a paginated list of establishment contacts based on specified query parameters.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of contacts to retrieve per page (default: 10).</param>
    /// <param name="sortBy">The field to sort the contacts by (default: "Id").</param>
    /// <param name="sortOrder">The sort order ("asc" for ascending, "desc" for descending, default: "asc").</param>
    /// <param name="establishmentId">Filter by establishment ID.</param>
    /// <param name="establishmentContactId">Filter by establishment contact ID.</param>
    /// <param name="contactPersonFirstName">Filter by contact person's first name.</param>
    /// <param name="contactPersonMiddleName">Filter by contact person's middle name.</param>
    /// <param name="contactPersonLastName">Filter by contact person's last name.</param>
    /// <param name="contactPhoneNumber">Filter by contact phone number.</param>
    /// <param name="isPrimary">Filter by whether the contact is primary.</param>
    /// <returns>A paginated list of establishment contacts.</returns>
    /// <response code="200">Returns a paginated list of <see cref="EstablishmentContactResponse"/>.</response>
    /// <response code="400">If the provided query parameters are invalid.</response>
    /// <response code="404">If no establishment contacts are found based on the provided criteria.</response>
    /// <response code="500">If an internal server error occurs during the retrieval process.</response>
    [HttpGet(Name = "GetEstablishmentContacts")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<EstablishmentContactResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllEstablishmentContacts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "Id",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string establishmentId = null,
        [FromQuery] string establishmentContactId = null,
        [FromQuery] string contactPersonFirstName = null,
        [FromQuery] string contactPersonMiddleName = null,
        [FromQuery] string contactPersonLastName = null,
        [FromQuery] string contactPhoneNumber = null,
        [FromQuery] bool? isPrimary = null)
    {
        EstablishmentContactSpecification spec;

        if (!string.IsNullOrEmpty(establishmentContactId))
        {
            Result<EstablishmentContactId> contactIdResult = IdHelper
                .ValidateAndCreateId<EstablishmentContactId>(establishmentContactId);
            if (!contactIdResult.IsSuccess)
            {
                return HandleErrorResponse(contactIdResult.ErrorType, contactIdResult.Errors);
            }

            Result<EstablishmentContactResponse> singleContactResult = await _establishmentContactService.GetEstablishmentContactByIdAsync(contactIdResult.Value!);

            return singleContactResult.IsSuccess && singleContactResult.Value != null
                ? Ok(singleContactResult.Value)
                : NotFound();
        }
        else
        {
            spec = new EstablishmentContactSpecification(
                pageNumber: pageNumber,
                pageSize: pageSize,
                sortBy: sortBy,
                sortOrder: sortOrder,
                establishmentId: establishmentId,
                contactPersonFirstName: contactPersonFirstName,
                contactPersonMiddleName: contactPersonMiddleName,
                contactPersonLastName: contactPersonLastName,
                contactPhoneNumber: contactPhoneNumber,
                isPrimary: isPrimary
            );

            Result<int> countResult = await _establishmentContactService.CountAsync(spec);
            if (!countResult.IsSuccess)
            { return HandleErrorResponse(countResult.ErrorType, countResult.Errors); }
            int totalCount = countResult.Value;

            Result<IEnumerable<EstablishmentContactResponse>> contactsResult = await _establishmentContactService.GetEstablishmentContactsAsync(spec);
            if (!contactsResult.IsSuccess)
            { return HandleErrorResponse(contactsResult.ErrorType, contactsResult.Errors); }
            IEnumerable<EstablishmentContactResponse> contacts = contactsResult.Value!;

            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var paginatedResponse = new PaginatedResponse<EstablishmentContactResponse>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                Data = contacts,
                Links = new PaginatedLinks
                {
                    First = GeneratePaginationLink(1, pageSize, sortBy, sortOrder, establishmentId, establishmentContactId, contactPersonFirstName, contactPersonMiddleName, contactPersonLastName, contactPhoneNumber, isPrimary),
                    Last = totalPages > 0 ? GeneratePaginationLink(totalPages, pageSize, sortBy, sortOrder, establishmentId, establishmentContactId, contactPersonFirstName, contactPersonMiddleName, contactPersonLastName, contactPhoneNumber, isPrimary) : null,
                    Next = pageNumber < totalPages ? GeneratePaginationLink(pageNumber + 1, pageSize, sortBy, sortOrder, establishmentId, establishmentContactId, contactPersonFirstName, contactPersonMiddleName, contactPersonLastName, contactPhoneNumber, isPrimary) : null,
                    Previous = pageNumber > 1 ? GeneratePaginationLink(pageNumber - 1, pageSize, sortBy, sortOrder, establishmentId, establishmentContactId, contactPersonFirstName, contactPersonMiddleName, contactPersonLastName, contactPhoneNumber, isPrimary) : null
                }
            };
            return Ok(paginatedResponse);
        }
    }

    private string GeneratePaginationLink(
            int targetPageNumber,
            int pageSize,
            string sortBy,
            string sortOrder,
            string establishmentId = null,
            string establishmentContactId = null,
            string contactPersonFirstName = null,
            string? contactPersonMiddleName = null,
            string? contactPersonLastName = null,
            string contactPhoneNumber = null,
            bool? isPrimary = null)
            => Url.Action(
                nameof(GetAllEstablishmentContacts),
                "EstablishmentContacts",
                new
                {
                    pageNumber = targetPageNumber,
                    pageSize,
                    sortBy,
                    sortOrder,
                    establishmentId,
                    establishmentContactId,
                    contactPersonFirstName,
                    contactPersonMiddleName,
                    contactPersonLastName,
                    contactPhoneNumber,
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
