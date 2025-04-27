using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using TestNest.Admin.API.Helpers;
using TestNest.Admin.Application.Contracts.Interfaces.Service;
using TestNest.Admin.Application.Specifications.EstablishmentAddressesSpecifications;
using TestNest.Admin.SharedLibrary.Common.Results;
using TestNest.Admin.SharedLibrary.Dtos.Paginations;
using TestNest.Admin.SharedLibrary.Dtos.Requests.Establishment;
using TestNest.Admin.SharedLibrary.Dtos.Responses.Establishments;
using TestNest.Admin.SharedLibrary.Exceptions.Common;
using TestNest.Admin.SharedLibrary.StronglyTypeIds;

namespace TestNest.Admin.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstablishmentAddressesController(
    IEstablishmentAddressService establishmentAddressService,
    IErrorResponseService errorResponseService) : ControllerBase
{
    private readonly IEstablishmentAddressService _establishmentAddressService = establishmentAddressService;
    private readonly IErrorResponseService _errorResponseService = errorResponseService;

    /// <summary>
    /// Creates a new establishment address.
    /// </summary>
    /// <param name="establishmentAddressForCreationRequest">The data for creating the establishment address.</param>
    /// <returns>The newly created establishment address.</returns>
    /// <response code="201">Returns the newly created <see cref="EstablishmentAddressResponse"/>.</response>
    /// <response code="400">If the provided establishment address data is invalid.</response>
    /// <response code="409">If creating the address results in a conflict (e.g., duplicate latitude and longitude for the same establishment).</response>
    /// <response code="500">If an internal server error occurs during the creation process.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(EstablishmentAddressResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateEstablishmentAddress(
        [FromBody] EstablishmentAddressForCreationRequest establishmentAddressForCreationRequest)
    {
        Result<EstablishmentId> establishmentIdResult = IdHelper
            .ValidateAndCreateId<EstablishmentId>(establishmentAddressForCreationRequest.EstablishmentId);
        if (!establishmentIdResult.IsSuccess)
        {
            return HandleErrorResponse(establishmentIdResult.ErrorType, establishmentIdResult.Errors);
        }

        Result<EstablishmentAddressResponse> result = await _establishmentAddressService
            .CreateEstablishmentAddressAsync(establishmentAddressForCreationRequest);

        if (result.IsSuccess)
        {
            return CreatedAtAction(
                nameof(GetAllEstablishmentAddresses),
                new { establishmentAddressId = result.Value!.EstablishmentAddressId }, result.Value);
        }

        return HandleErrorResponse(result.ErrorType, result.Errors);
    }

    /// <summary>
    /// Updates an existing establishment address.
    /// </summary>
    /// <param name="establishmentAddressId">The ID of the establishment address to update.</param>
    /// <param name="establishmentAddressForUpdateRequest">The updated data for the establishment address.</param>
    /// <returns>Returns the updated establishment address.</returns>
    /// <response code="200">Returns the updated <see cref="EstablishmentAddressResponse"/> if the update was successful.</response>
    /// <response code="400">If the provided <paramref name="establishmentAddressId"/> or the updated data is invalid.</response>
    /// <response code="404">If the establishment address with the given <paramref name="establishmentAddressId"/> is not found.</response>
    /// <response code="409">If updating the address results in a conflict (e.g., duplicate latitude and longitude for the same establishment).</response>
    /// <response code="401">If attempting to update the EstablishmentId to a different value than the existing address.</response>
    /// <response code="500">If an internal server error occurs during the update process.</response>
    [HttpPut("{establishmentAddressId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EstablishmentAddressResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateEstablishmentAddress(
        string establishmentAddressId,
        [FromBody] EstablishmentAddressForUpdateRequest establishmentAddressForUpdateRequest)
    {
        Result<EstablishmentAddressId> establishmentAddressIdResult = IdHelper
            .ValidateAndCreateId<EstablishmentAddressId>(establishmentAddressId);
        if (!establishmentAddressIdResult.IsSuccess)
        {
            return HandleErrorResponse(establishmentAddressIdResult.ErrorType, establishmentAddressIdResult.Errors);
        }

        Result<EstablishmentId> establishmentIdResult = IdHelper
            .ValidateAndCreateId<EstablishmentId>(establishmentAddressForUpdateRequest.EstablishmentId);
        if (!establishmentIdResult.IsSuccess)
        {
            return HandleErrorResponse(establishmentIdResult.ErrorType, establishmentIdResult.Errors);
        }

        Result<EstablishmentAddressResponse> updatedAddressResult = await _establishmentAddressService
            .UpdateEstablishmentAddressAsync(establishmentAddressIdResult.Value!, establishmentAddressForUpdateRequest);

        if (updatedAddressResult.IsSuccess)
        {
            return Ok(updatedAddressResult.Value);
        }

        return HandleErrorResponse(updatedAddressResult.ErrorType, updatedAddressResult.Errors);
    }

    /// <summary>
    /// Deletes an establishment address.
    /// </summary>
    /// <param name="establishmentAddressId">The ID of the establishment address to delete.</param>
    /// <returns>Returns an empty response if the deletion was successful.</returns>
    /// <response code="204">If the establishment address was successfully deleted.</response>
    /// <response code="400">If the provided <paramref name="establishmentAddressId"/> is invalid.</response>
    /// <response code="404">If the establishment address with the given <paramref name="establishmentAddressId"/> is not found.</response>
    /// <response code="409">If attempting to delete the primary address for an establishment.</response>
    /// <response code="500">If an internal server error occurs during the deletion process.</response>
    [HttpDelete("{establishmentAddressId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteEstablishmentAddress(
        string establishmentAddressId)
    {
        Result<EstablishmentAddressId> establishmentAddressIdResult = IdHelper
            .ValidateAndCreateId<EstablishmentAddressId>(establishmentAddressId);
        if (!establishmentAddressIdResult.IsSuccess)
        {
            return HandleErrorResponse(establishmentAddressIdResult.ErrorType, establishmentAddressIdResult.Errors);
        }

        Result result = await _establishmentAddressService
            .DeleteEstablishmentAddressAsync(establishmentAddressIdResult.Value!);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return HandleErrorResponse(result.ErrorType, result.Errors);
    }

    /// <summary>
    /// Retrieves a paginated list of establishment addresses based on specified query parameters.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of addresses to retrieve per page (default: 10).</param>
    /// <param name="sortBy">The field to sort the addresses by (default: "Id").</param>
    /// <param name="sortOrder">The sort order ("asc" for ascending, "desc" for descending, default: "asc").</param>
    /// <param name="establishmentId">Filter by establishment ID.</param>
    /// <param name="establishmentAddressId">Filter by establishment address ID.</param>
    /// <param name="city">Filter by city.</param>
    /// <param name="municipality">Filter by municipality.</param>
    /// <param name="province">Filter by province.</param>
    /// <param name="region">Filter by region.</param>
    /// <param name="isPrimary">Filter by whether the address is primary.</param>
    /// <returns>A paginated list of establishment addresses.</returns>
    /// <response code="200">Returns a paginated list of <see cref="EstablishmentAddressResponse"/>.</response>
    /// <response code="400">If the provided query parameters are invalid.</response>
    /// <response code="404">If no establishment addresses are found based on the provided criteria.</response>
    /// <response code="500">If an internal server error occurs during the retrieval process.</response>
    [HttpGet(Name = "GetEstablishmentAddresses")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<EstablishmentAddressResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllEstablishmentAddresses(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "Id",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string establishmentId = null,
        [FromQuery] string establishmentAddressId = null,
        [FromQuery] string city = null,
        [FromQuery] string municipality = null,
        [FromQuery] string province = null,
        [FromQuery] string region = null,
        [FromQuery] bool? isPrimary = null)
    {
        EstablishmentAddressSpecification spec;

        if (!string.IsNullOrEmpty(establishmentAddressId))
        {
            Result<EstablishmentAddressId> addressIdResult = IdHelper
                .ValidateAndCreateId<EstablishmentAddressId>(establishmentAddressId);
            if (!addressIdResult.IsSuccess)
            {
                return HandleErrorResponse(addressIdResult.ErrorType, addressIdResult.Errors);
            }

            Result<EstablishmentAddressResponse> singleAddressResult = await _establishmentAddressService.GetEstablishmentAddressByIdAsync(addressIdResult.Value!);

            return singleAddressResult.IsSuccess && singleAddressResult.Value != null
                ? Ok(singleAddressResult.Value)
                : NotFound();
        }
        else
        {
            spec = new EstablishmentAddressSpecification(
                pageNumber: pageNumber,
                pageSize: pageSize,
                sortBy: sortBy,
                sortOrder: sortOrder,
                establishmentId: establishmentId,
                city: city,
                municipality: municipality,
                province: province,
                region: region,
                isPrimary: isPrimary
            );

            Result<int> countResult = await _establishmentAddressService.CountAsync(spec);
            if (!countResult.IsSuccess)
            { return HandleErrorResponse(countResult.ErrorType, countResult.Errors); }
            int totalCount = countResult.Value;

            Result<IEnumerable<EstablishmentAddressResponse>> addressesResult = await _establishmentAddressService.ListAsync(spec);
            if (!addressesResult.IsSuccess)
            { return HandleErrorResponse(addressesResult.ErrorType, addressesResult.Errors); }
            IEnumerable<EstablishmentAddressResponse> addresses = addressesResult.Value!;

            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var paginatedResponse = new PaginatedResponse<EstablishmentAddressResponse>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                Data = addresses,
                Links = new PaginatedLinks
                {
                    First = GeneratePaginationLink(1, pageSize, sortBy, sortOrder, establishmentId, establishmentAddressId, city, municipality, province, region, isPrimary),
                    Last = totalPages > 0 ? GeneratePaginationLink(totalPages, pageSize, sortBy, sortOrder, establishmentId, establishmentAddressId, city, municipality, province, region, isPrimary) : null,
                    Next = pageNumber < totalPages ? GeneratePaginationLink(pageNumber + 1, pageSize, sortBy, sortOrder, establishmentId, establishmentAddressId, city, municipality, province, region, isPrimary) : null,
                    Previous = pageNumber > 1 ? GeneratePaginationLink(pageNumber - 1, pageSize, sortBy, sortOrder, establishmentId, establishmentAddressId, city, municipality, province, region, isPrimary) : null
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
        string establishmentAddressId = null,
        string city = null,
        string municipality = null,
        string province = null,
        string region = null,
        bool? isPrimary = null)
        => Url.Action(
            nameof(GetAllEstablishmentAddresses),
            "EstablishmentAddresses",
            new
            {
                pageNumber = targetPageNumber,
                pageSize,
                sortBy,
                sortOrder,
                establishmentId,
                establishmentAddressId,
                city,
                municipality,
                province,
                region,
                isPrimary
            },
            protocol: Request.Scheme
        )!;

    [HttpPatch("{establishmentAddressId}")]
    public async Task<IActionResult> PatchEstablishmentAddress(
        string establishmentAddressId,
        [FromBody] JsonPatchDocument<EstablishmentAddressPatchRequest> patchDocument)
    {
        Result<EstablishmentAddressId> establishmentAddressIdResult = IdHelper
            .ValidateAndCreateId<EstablishmentAddressId>(establishmentAddressId);
        if (!establishmentAddressIdResult.IsSuccess)
        {
            return HandleErrorResponse(establishmentAddressIdResult.ErrorType, establishmentAddressIdResult.Errors);
        }

        var establishmentAddressPatchRequest = new EstablishmentAddressPatchRequest();
        patchDocument.ApplyTo(establishmentAddressPatchRequest);

        if (!TryValidateModel(establishmentAddressPatchRequest))
        {
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error("ValidationError", e.ErrorMessage))
                .ToList();
            return HandleErrorResponse(ErrorType.Validation, validationErrors);
        }

        Result<EstablishmentAddressResponse> patchedAddressResult = await _establishmentAddressService
            .PatchEstablishmentAddressAsync(establishmentAddressIdResult.Value!, establishmentAddressPatchRequest);

        if (patchedAddressResult.IsSuccess)
        {
            return Ok(patchedAddressResult.Value);
        }

        return HandleErrorResponse(patchedAddressResult.ErrorType, patchedAddressResult.Errors);
    }

    private IActionResult HandleErrorResponse(ErrorType errorType, IEnumerable<Error> errors)
    {
        List<Error> safeErrors = errors?.ToList() ?? [];

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

