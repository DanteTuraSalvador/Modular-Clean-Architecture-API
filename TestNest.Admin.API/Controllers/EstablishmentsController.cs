using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using TestNest.Admin.API.Helpers;
using TestNest.Admin.Application.Contracts.Interfaces.Service;
using TestNest.Admin.Application.Specifications.EstablishmentSpecifications;
using TestNest.Admin.Domain.Establishments;
using TestNest.Admin.SharedLibrary.Common.Results;
using TestNest.Admin.SharedLibrary.Dtos.Paginations;
using TestNest.Admin.SharedLibrary.Dtos.Requests.Establishment;
using TestNest.Admin.SharedLibrary.Dtos.Responses.Establishments;
using TestNest.Admin.SharedLibrary.Exceptions;
using TestNest.Admin.SharedLibrary.Exceptions.Common;
using TestNest.Admin.SharedLibrary.StronglyTypeIds;
using TestNest.Admin.Application.Mappings;

namespace TestNest.Admin.API.Controllers;


[ApiController]
[Route("api/[controller]")]
public class EstablishmentsController(
    IEstablishmentService establishmentService,
    IErrorResponseService errorResponseService) : ControllerBase
{
    private readonly IEstablishmentService _establishmentService = establishmentService;
    private readonly IErrorResponseService _errorResponseService = errorResponseService;

    /// <summary>
    /// Creates a new establishment.
    /// </summary>
    /// <param name="establishmentForCreationRequest">The data for creating the establishment.</param>
    /// <returns>The newly created establishment.</returns>
    /// <response code="201">Returns the newly created <see cref="EstablishmentResponse"/>.</response>
    /// <response code="400">If the provided establishment data is invalid.</response>
    /// <response code="500">If an internal server error occurs during the creation process.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(EstablishmentResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateEstablishment(
        [FromBody] EstablishmentForCreationRequest establishmentForCreationRequest)
    {
        Result<Establishment> result = await _establishmentService
            .CreateEstablishmentAsync(establishmentForCreationRequest);

        if (result.IsSuccess)
        {
            EstablishmentResponse dto = result.Value!.ToEstablishmentResponse(); // Use the extension here
            return CreatedAtAction(
                nameof(GetAllEstablishments),
                new { establishmentId = dto.EstablishmentId },
                dto);
        }

        return HandleErrorResponse(result.ErrorType, result.Errors);
    }

    /// <summary>
    /// Updates an existing establishment.
    /// </summary>
    /// <param name="establishmentId">The ID of the establishment to update.</param>
    /// <param name="establishmentForUpdateRequest">The updated data for the establishment.</param>
    /// <returns>Returns the updated establishment.</returns>
    /// <response code="200">Returns the updated <see cref="EstablishmentResponse"/> if the update was successful.</response>
    /// <response code="400">If the provided <paramref name="establishmentId"/> or the updated data is invalid.</response>
    /// <response code="404">If the establishment with the given <paramref name="establishmentId"/> is not found.</response>
    /// <response code="500">If an internal server error occurs during the update process.</response>
    [HttpPut("{establishmentId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EstablishmentResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateEstablishment(
        string establishmentId,
        [FromBody] EstablishmentForUpdateRequest establishmentForUpdateRequest)
    {
        Result<EstablishmentId> establishmentIdValidatedResult = IdHelper
            .ValidateAndCreateId<EstablishmentId>(establishmentId);

        if (!establishmentIdValidatedResult.IsSuccess)
        {
            return HandleErrorResponse(
                establishmentIdValidatedResult.ErrorType,
                establishmentIdValidatedResult.Errors);
        }

        Result<Establishment> updatedEstablishment = await _establishmentService
            .UpdateEstablishmentAsync(
                establishmentIdValidatedResult.Value!,
                establishmentForUpdateRequest);

        if (updatedEstablishment.IsSuccess)
        {
            return Ok(updatedEstablishment.Value!.ToEstablishmentResponse()); // Use the extension here
        }

        return HandleErrorResponse(
            updatedEstablishment.ErrorType,
            updatedEstablishment.Errors);
    }

    /// <summary>
    /// Partially updates an existing establishment.
    /// </summary>
    /// <param name="establishmentId">The ID of the establishment to update.</param>
    /// <param name="patchDocument">A JSON Patch document containing the operations to apply to the establishment.</param>
    /// <returns>Returns the updated establishment.</returns>
    /// <response code="200">Returns the updated <see cref="EstablishmentResponse"/> if the update was successful.</response>
    /// <response code="400">If the provided <paramref name="establishmentId"/> is invalid, the patch document is invalid, or if the validation of the patched entity fails.</response>
    /// <response code="404">If the establishment with the given <paramref name="establishmentId"/> is not found.</response>
    /// <response code="500">If an internal server error occurs during the update process.</response>
    [HttpPatch("{establishmentId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EstablishmentResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PatchEstablishment(
        string establishmentId,
        [FromBody] JsonPatchDocument<EstablishmentPatchRequest> patchDocument)
    {
        Result<EstablishmentId> establishmentIdValidatedResult = IdHelper.ValidateAndCreateId<EstablishmentId>(establishmentId);

        if (!establishmentIdValidatedResult.IsSuccess)
        {
            return HandleErrorResponse(
                establishmentIdValidatedResult.ErrorType,
                establishmentIdValidatedResult.Errors);
        }

        var establishmentPatchRequest = new EstablishmentPatchRequest();
        patchDocument.ApplyTo(establishmentPatchRequest);

        if (!TryValidateModel(establishmentPatchRequest))
        {
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error("ValidationError", e.ErrorMessage))
                .ToList();

            return HandleErrorResponse(ErrorType.Validation, validationErrors);
        }

        Result<Establishment> result = await _establishmentService
            .PatchEstablishmentAsync(establishmentIdValidatedResult.Value!,
                establishmentPatchRequest);

        if (result.IsSuccess)
        {
            return Ok(result.Value!.ToEstablishmentResponse()); // Use the extension here
        }

        return HandleErrorResponse(result.ErrorType, result.Errors);
    }

    /// <summary>
    /// Deletes an establishment.
    /// </summary>
    /// <param name="establishmentId">The ID of the establishment to delete.</param>
    /// <returns>Returns an empty response if the deletion was successful.</returns>
    /// <response code="204">If the establishment was successfully deleted.</response>
    /// <response code="400">If the provided <paramref name="establishmentId"/> is invalid.</response>
    /// <response code="404">If the establishment with the given <paramref name="establishmentId"/> is not found.</response>
    /// <response code="500">If an internal server error occurs during the deletion process.</response>
    [HttpDelete("{establishmentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteEstablishment(string establishmentId)
    {
        Result<EstablishmentId> establishmentIdValidatedResult = IdHelper
            .ValidateAndCreateId<EstablishmentId>(establishmentId);

        if (!establishmentIdValidatedResult.IsSuccess)
        {
            return HandleErrorResponse(
                ErrorType.Validation,
                establishmentIdValidatedResult.Errors);
        }

        Result result = await _establishmentService.DeleteEstablishmentAsync(establishmentIdValidatedResult.Value!);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return HandleErrorResponse(result.ErrorType, result.Errors);
    }

    /// <summary>
    /// Retrieves a paginated list of establishments based on specified query parameters.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of establishments to retrieve per page (default: 10).</param>
    /// <param name="sortBy">The field to sort the establishments by (default: "EstablishmentId").</param>
    /// <param name="sortOrder">The sort order ("asc" for ascending, "desc" for descending, default: "asc").</param>
    /// <param name="establishmentId">Filter by establishment ID.</param>
    /// <param name="establishmentName">Filter by establishment name.</param>
    /// <param name="establishmentEmail">Filter by establishment email.</param>
    /// <param name="establishmentStatusId">Filter by establishment status ID.</param>
    /// <returns>A paginated list of establishments.</returns>
    /// <response code="200">Returns a paginated list of <see cref="EstablishmentResponse"/>.</response>
    /// <response code="400">If the provided query parameters are invalid.</response>
    /// <response code="404">If no establishments are found based on the provided criteria.</response>
    /// <response code="500">If an internal server error occurs during the retrieval process.</response>
    [HttpGet(Name = "GetEstablishments")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<EstablishmentResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllEstablishments(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "EstablishmentId",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string establishmentId = null,
        [FromQuery] string establishmentName = null,
        [FromQuery] string establishmentEmail = null,
        [FromQuery] int? establishmentStatusId = null)
    {
        if (!string.IsNullOrEmpty(establishmentId))
        {
            Result<EstablishmentId> establishmentIdValidatedResult = IdHelper
                .ValidateAndCreateId<EstablishmentId>(establishmentId);
            if (!establishmentIdValidatedResult.IsSuccess)
            {
                return HandleErrorResponse(establishmentIdValidatedResult.ErrorType, establishmentIdValidatedResult.Errors);
            }

            Result<EstablishmentResponse> result = await _establishmentService
                .GetEstablishmentByIdAsync(establishmentIdValidatedResult.Value!);

            return result.IsSuccess
                ? Ok(result.Value!)
                : HandleErrorResponse(result.ErrorType, result.Errors);
        }
        else
        {
            var spec = new EstablishmentSpecification(
                establishmentName: establishmentName,
                establishmentEmail: establishmentEmail,
                establishmentStatusId: establishmentStatusId,
                sortBy: sortBy,
                sortDirection: sortOrder,
                pageNumber: pageNumber,
                pageSize: pageSize,
                establishmentId: establishmentId // Included for completeness, though it's null here
            );

            Result<int> countResult = await _establishmentService.CountAsync(spec);
            if (!countResult.IsSuccess)
            {
                return HandleErrorResponse(countResult.ErrorType, countResult.Errors);
            }
            int totalCount = countResult.Value;

            Result<IEnumerable<EstablishmentResponse>> establishmentsResult = await _establishmentService.GetEstablishmentsAsync(spec);
            if (!establishmentsResult.IsSuccess)
            {
                return HandleErrorResponse(establishmentsResult.ErrorType, establishmentsResult.Errors);
            }
            IEnumerable<EstablishmentResponse> establishments = establishmentsResult.Value!;

            if (establishments == null || !establishments.Any())
            {
                var exception = EstablishmentException.NotFound();
                return HandleErrorResponse(
                    ErrorType.NotFound,
                    [new Error(exception.Code.ToString(), exception.Message.ToString())]);
            }

            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var paginatedResponse = new PaginatedResponse<EstablishmentResponse>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                Data = establishments,
                Links = new PaginatedLinks
                {
                    First = GenerateEstablishmentPaginationLink(1, pageSize, sortBy, sortOrder, establishmentId, establishmentName, establishmentEmail, establishmentStatusId),
                    Last = totalPages > 0 ? GenerateEstablishmentPaginationLink(totalPages, pageSize, sortBy, sortOrder, establishmentId, establishmentName, establishmentEmail, establishmentStatusId) : null,
                    Next = pageNumber < totalPages ? GenerateEstablishmentPaginationLink(pageNumber + 1, pageSize, sortBy, sortOrder, establishmentId, establishmentName, establishmentEmail, establishmentStatusId) : null,
                    Previous = pageNumber > 1 ? GenerateEstablishmentPaginationLink(pageNumber - 1, pageSize, sortBy, sortOrder, establishmentId, establishmentName, establishmentEmail, establishmentStatusId) : null
                }
            };

            return Ok(paginatedResponse);
        }
    }

    private string GenerateEstablishmentPaginationLink(
        int targetPageNumber,
        int pageSize,
        string sortBy,
        string sortOrder,
        string establishmentId = null,
        string establishmentName = null,
        string establishmentEmail = null,
        int? establishmentStatusId = null)
        => Url.Action(
            nameof(GetAllEstablishments),
            "Establishments",
            new
            {
                pageNumber = targetPageNumber,
                pageSize,
                sortBy,
                sortOrder,
                establishmentId,
                establishmentName,
                establishmentEmail,
                establishmentStatusId
            },
            protocol: Request.Scheme
        )!;

    private IActionResult HandleErrorResponse(ErrorType errorType, IEnumerable<Error> errors)
    {
        List<Error> safeErrors = errors?.ToList() ?? [];

        return errorType switch
        {
            ErrorType.Validation =>
                _errorResponseService.CreateProblemDetails(
                    StatusCodes.Status400BadRequest,
                    "Validation Error", "Validation failed.",
                    new Dictionary<string, object> { { "errors", safeErrors } }),

            ErrorType.NotFound =>
                _errorResponseService.CreateProblemDetails(
                    StatusCodes.Status404NotFound,
                    "Not Found", "Resource not found.",
                    new Dictionary<string, object> { { "errors", safeErrors } }),

            ErrorType.Conflict =>
                _errorResponseService.CreateProblemDetails(
                    StatusCodes.Status409Conflict,
                    "Conflict", "Resource conflict.",
                    new Dictionary<string, object> { { "errors", safeErrors } }),

            _ => _errorResponseService.CreateProblemDetails(
                    StatusCodes.Status500InternalServerError,
                    "Internal Server Error", "An unexpected error occurred.")
        };
    }
}

//[ApiController]
//[Route("api/[controller]")]
//public class EstablishmentsController(
//    IEstablishmentService establishmentService,
//    IMapper mapper,
//    IErrorResponseService errorResponseService) : ControllerBase
//{
//    private readonly IEstablishmentService _establishmentService = establishmentService;
//    private readonly IMapper _mapper = mapper;
//    private readonly IErrorResponseService _errorResponseService = errorResponseService;

//    /// <summary>
//    /// Creates a new establishment.
//    /// </summary>
//    /// <param name="establishmentForCreationRequest">The data for creating the establishment.</param>
//    /// <returns>The newly created establishment.</returns>
//    /// <response code="201">Returns the newly created <see cref="EstablishmentResponse"/>.</response>
//    /// <response code="400">If the provided establishment data is invalid.</response>
//    /// <response code="500">If an internal server error occurs during the creation process.</response>
//    [HttpPost]
//    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(EstablishmentResponse))]
//    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
//    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//    public async Task<IActionResult> CreateEstablishment(
//        [FromBody] EstablishmentForCreationRequest establishmentForCreationRequest)
//    {
//        Result<Establishment> result = await _establishmentService
//            .CreateEstablishmentAsync(establishmentForCreationRequest);

//        if (result.IsSuccess)
//        {
//            EstablishmentResponse dto = _mapper.Map<EstablishmentResponse>(result.Value!);
//            return CreatedAtAction(
//                nameof(GetAllEstablishments),
//                new { establishmentId = dto.EstablishmentId },
//                dto);
//        }

//        return HandleErrorResponse(result.ErrorType, result.Errors);
//    }

//    /// <summary>
//    /// Updates an existing establishment.
//    /// </summary>
//    /// <param name="establishmentId">The ID of the establishment to update.</param>
//    /// <param name="establishmentForUpdateRequest">The updated data for the establishment.</param>
//    /// <returns>Returns the updated establishment.</returns>
//    /// <response code="200">Returns the updated <see cref="EstablishmentResponse"/> if the update was successful.</response>
//    /// <response code="400">If the provided <paramref name="establishmentId"/> or the updated data is invalid.</response>
//    /// <response code="404">If the establishment with the given <paramref name="establishmentId"/> is not found.</response>
//    /// <response code="500">If an internal server error occurs during the update process.</response>
//    [HttpPut("{establishmentId}")]
//    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EstablishmentResponse))]
//    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
//    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
//    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//    public async Task<IActionResult> UpdateEstablishment(
//        string establishmentId,
//        [FromBody] EstablishmentForUpdateRequest establishmentForUpdateRequest)
//    {
//        Result<EstablishmentId> establishmentIdValidatedResult = IdHelper
//            .ValidateAndCreateId<EstablishmentId>(establishmentId);

//        if (!establishmentIdValidatedResult.IsSuccess)
//        {
//            return HandleErrorResponse(
//                establishmentIdValidatedResult.ErrorType,
//                establishmentIdValidatedResult.Errors);
//        }

//        Result<Establishment> updatedEstablishment = await _establishmentService
//            .UpdateEstablishmentAsync(
//                establishmentIdValidatedResult.Value!,
//                establishmentForUpdateRequest);

//        if (updatedEstablishment.IsSuccess)
//        {
//            return Ok(_mapper
//                .Map<EstablishmentResponse>(
//                    updatedEstablishment.Value!));
//        }

//        return HandleErrorResponse(
//            updatedEstablishment.ErrorType,
//            updatedEstablishment.Errors);
//    }

//    /// <summary>
//    /// Partially updates an existing establishment.
//    /// </summary>
//    /// <param name="establishmentId">The ID of the establishment to update.</param>
//    /// <param name="patchDocument">A JSON Patch document containing the operations to apply to the establishment.</param>
//    /// <returns>Returns the updated establishment.</returns>
//    /// <response code="200">Returns the updated <see cref="EstablishmentResponse"/> if the update was successful.</response>
//    /// <response code="400">If the provided <paramref name="establishmentId"/> is invalid, the patch document is invalid, or if the validation of the patched entity fails.</response>
//    /// <response code="404">If the establishment with the given <paramref name="establishmentId"/> is not found.</response>
//    /// <response code="500">If an internal server error occurs during the update process.</response>
//    [HttpPatch("{establishmentId}")]
//    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EstablishmentResponse))]
//    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
//    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
//    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//    public async Task<IActionResult> PatchEstablishment(
//        string establishmentId,
//        [FromBody] JsonPatchDocument<EstablishmentPatchRequest> patchDocument)
//    {
//        Result<EstablishmentId> establishmentIdValidatedResult = IdHelper.ValidateAndCreateId<EstablishmentId>(establishmentId);

//        if (!establishmentIdValidatedResult.IsSuccess)
//        {
//            return HandleErrorResponse(
//                establishmentIdValidatedResult.ErrorType,
//                establishmentIdValidatedResult.Errors);
//        }

//        var establishmentPatchRequest = new EstablishmentPatchRequest();
//        patchDocument.ApplyTo(establishmentPatchRequest);

//        if (!TryValidateModel(establishmentPatchRequest))
//        {
//            var validationErrors = ModelState.Values
//                .SelectMany(v => v.Errors)
//                .Select(e => new Error("ValidationError", e.ErrorMessage))
//                .ToList();

//            return HandleErrorResponse(ErrorType.Validation, validationErrors);
//        }

//        Result<Establishment> result = await _establishmentService
//            .PatchEstablishmentAsync(establishmentIdValidatedResult.Value!,
//                establishmentPatchRequest);

//        if (result.IsSuccess)
//        {
//            return Ok(_mapper.Map<EstablishmentResponse>(result.Value!));
//        }

//        return HandleErrorResponse(result.ErrorType, result.Errors);
//    }

//    /// <summary>
//    /// Deletes an establishment.
//    /// </summary>
//    /// <param name="establishmentId">The ID of the establishment to delete.</param>
//    /// <returns>Returns an empty response if the deletion was successful.</returns>
//    /// <response code="204">If the establishment was successfully deleted.</response>
//    /// <response code="400">If the provided <paramref name="establishmentId"/> is invalid.</response>
//    /// <response code="404">If the establishment with the given <paramref name="establishmentId"/> is not found.</response>
//    /// <response code="500">If an internal server error occurs during the deletion process.</response>
//    [HttpDelete("{establishmentId}")]
//    [ProducesResponseType(StatusCodes.Status204NoContent)]
//    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
//    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
//    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//    public async Task<IActionResult> DeleteEstablishment(string establishmentId)
//    {
//        Result<EstablishmentId> establishmentIdValidatedResult = IdHelper
//            .ValidateAndCreateId<EstablishmentId>(establishmentId);

//        if (!establishmentIdValidatedResult.IsSuccess)
//        {
//            return HandleErrorResponse(
//                ErrorType.Validation,
//                establishmentIdValidatedResult.Errors);
//        }

//        Result result = await _establishmentService.DeleteEstablishmentAsync(establishmentIdValidatedResult.Value!);

//        if (result.IsSuccess)
//        {
//            return NoContent();
//        }

//        return HandleErrorResponse(result.ErrorType, result.Errors);
//    }

//    /// <summary>
//    /// Retrieves a paginated list of establishments based on specified query parameters.
//    /// </summary>
//    /// <param name="pageNumber">The page number to retrieve (default: 1).</param>
//    /// <param name="pageSize">The number of establishments to retrieve per page (default: 10).</param>
//    /// <param name="sortBy">The field to sort the establishments by (default: "EstablishmentId").</param>
//    /// <param name="sortOrder">The sort order ("asc" for ascending, "desc" for descending, default: "asc").</param>
//    /// <param name="establishmentId">Filter by establishment ID.</param>
//    /// <param name="establishmentName">Filter by establishment name.</param>
//    /// <param name="establishmentEmail">Filter by establishment email.</param>
//    /// <param name="establishmentStatusId">Filter by establishment status ID.</param>
//    /// <returns>A paginated list of establishments.</returns>
//    /// <response code="200">Returns a paginated list of <see cref="EstablishmentResponse"/>.</response>
//    /// <response code="400">If the provided query parameters are invalid.</response>
//    /// <response code="404">If no establishments are found based on the provided criteria.</response>
//    /// <response code="500">If an internal server error occurs during the retrieval process.</response>
//    [HttpGet(Name = "GetEstablishments")]
//    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<EstablishmentResponse>))]
//    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
//    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
//    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//    public async Task<IActionResult> GetAllEstablishments(
//        [FromQuery] int pageNumber = 1,
//        [FromQuery] int pageSize = 10,
//        [FromQuery] string sortBy = "EstablishmentId",
//        [FromQuery] string sortOrder = "asc",
//        [FromQuery] string establishmentId = null,
//        [FromQuery] string establishmentName = null,
//        [FromQuery] string establishmentEmail = null,
//        [FromQuery] int? establishmentStatusId = null)
//    {
//        if (!string.IsNullOrEmpty(establishmentId))
//        {
//            Result<EstablishmentId> establishmentIdValidatedResult = IdHelper
//                .ValidateAndCreateId<EstablishmentId>(establishmentId);
//            if (!establishmentIdValidatedResult.IsSuccess)
//            {
//                return HandleErrorResponse(establishmentIdValidatedResult.ErrorType, establishmentIdValidatedResult.Errors);
//            }

//            Result<Establishment> result = await _establishmentService
//                .GetEstablishmentByIdAsync(establishmentIdValidatedResult.Value!);

//            return result.IsSuccess
//                ? Ok(_mapper.Map<EstablishmentResponse>(result.Value!))
//                : HandleErrorResponse(result.ErrorType, result.Errors);
//        }
//        else
//        {
//            var countSpec = new EstablishmentSpecification(
//                establishmentName: establishmentName,
//                establishmentEmail: establishmentEmail,
//                establishmentStatusId: establishmentStatusId,
//                sortBy: sortBy,
//                sortDirection: sortOrder,
//                establishmentId: establishmentId
//            );

//            Result<int> countResult = await _establishmentService.CountAsync(countSpec);
//            if (!countResult.IsSuccess)
//            { return HandleErrorResponse(countResult.ErrorType, countResult.Errors); }
//            int totalCount = countResult.Value;

//            var listSpec = new EstablishmentSpecification(
//                establishmentName: establishmentName,
//                establishmentEmail: establishmentEmail,
//                establishmentStatusId: establishmentStatusId,
//                sortBy: sortBy,
//                sortDirection: sortOrder,
//                pageNumber: pageNumber,
//                pageSize: pageSize,
//                establishmentId: establishmentId
//            );

//            Result<IEnumerable<Establishment>> establishmentsResult = await _establishmentService.GetEstablishmentsAsync(listSpec);
//            if (!establishmentsResult.IsSuccess)
//            { return HandleErrorResponse(establishmentsResult.ErrorType, establishmentsResult.Errors); }
//            IEnumerable<Establishment> establishments = establishmentsResult.Value!;

//            if (establishments == null || !establishments.Any())
//            {
//                return HandleErrorResponse(ErrorType.NotFound, [new Error("NotFound", "No establishments found on this page.")]);
//            }

//            IEnumerable<EstablishmentResponse> responseData = _mapper.Map<IEnumerable<EstablishmentResponse>>(establishments);
//            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

//            var paginatedResponse = new PaginatedResponse<EstablishmentResponse>
//            {
//                TotalCount = totalCount,
//                PageNumber = pageNumber,
//                PageSize = pageSize,
//                TotalPages = totalPages,
//                Data = responseData,
//                Links = new PaginatedLinks
//                {
//                    First = GenerateEstablishmentPaginationLink(1, pageSize, sortBy, sortOrder, establishmentId, establishmentName, establishmentEmail, establishmentStatusId),
//                    Last = totalPages > 0 ? GenerateEstablishmentPaginationLink(totalPages, pageSize, sortBy, sortOrder, establishmentId, establishmentName, establishmentEmail, establishmentStatusId) : null,
//                    Next = pageNumber < totalPages ? GenerateEstablishmentPaginationLink(pageNumber + 1, pageSize, sortBy, sortOrder, establishmentId, establishmentName, establishmentEmail, establishmentStatusId) : null,
//                    Previous = pageNumber > 1 ? GenerateEstablishmentPaginationLink(pageNumber - 1, pageSize, sortBy, sortOrder, establishmentId, establishmentName, establishmentEmail, establishmentStatusId) : null
//                }
//            };

//            return Ok(paginatedResponse);
//        }
//    }

//    private string GenerateEstablishmentPaginationLink(
//        int targetPageNumber,
//        int pageSize,
//        string sortBy,
//        string sortOrder,
//        string establishmentId,
//        string establishmentName,
//        string establishmentEmail,
//        int? establishmentStatusId)
//        => Url.Action(
//            nameof(GetAllEstablishments),
//            "Establishments",
//            new
//            {
//                pageNumber = targetPageNumber,
//                pageSize,
//                sortBy,
//                sortOrder,
//                EstablishmentId = establishmentId,
//                EstablishmentName = establishmentName,
//                EstablismentEmail = establishmentEmail,
//                EstablishmentStatusId = establishmentStatusId
//            },
//            protocol: Request.Scheme
//        )!;

//    private IActionResult HandleErrorResponse(ErrorType errorType, IEnumerable<Error> errors)
//    {
//        List<Error> safeErrors = errors?.ToList() ?? [];

//        return errorType switch
//        {
//            ErrorType.Validation =>
//                _errorResponseService.CreateProblemDetails(
//                    StatusCodes.Status400BadRequest,
//                    "Validation Error", "Validation failed.",
//                    new Dictionary<string, object> { { "errors", safeErrors } }),

//            ErrorType.NotFound =>
//                _errorResponseService.CreateProblemDetails(
//                    StatusCodes.Status404NotFound,
//                    "Not Found", "Resource not found.",
//                    new Dictionary<string, object> { { "errors", safeErrors } }),

//            ErrorType.Conflict =>
//                _errorResponseService.CreateProblemDetails(
//                    StatusCodes.Status409Conflict,
//                    "Conflict", "Resource conflict.",
//                    new Dictionary<string, object> { { "errors", safeErrors } }),

//            _ => _errorResponseService.CreateProblemDetails(
//                    StatusCodes.Status500InternalServerError,
//                    "Internal Server Error", "An unexpected error occurred.")
//        };
//    }
//}
