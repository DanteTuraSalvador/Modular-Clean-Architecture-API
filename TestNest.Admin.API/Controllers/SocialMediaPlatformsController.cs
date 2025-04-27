
using Microsoft.AspNetCore.Mvc;
using TestNest.Admin.API.Helpers;
using TestNest.Admin.Application.Contracts.Interfaces.Service;
using TestNest.Admin.Application.Mappings; 
using TestNest.Admin.Application.Specifications.SoicalMediaPlatfomrSpecifications;
using TestNest.Admin.Domain.SocialMedias;
using TestNest.Admin.SharedLibrary.Common.Results;
using TestNest.Admin.SharedLibrary.Dtos.Paginations;
using TestNest.Admin.SharedLibrary.Dtos.Requests.SocialMediaPlatform;
using TestNest.Admin.SharedLibrary.Dtos.Responses;
using TestNest.Admin.SharedLibrary.Exceptions;
using TestNest.Admin.SharedLibrary.Exceptions.Common;
using TestNest.Admin.SharedLibrary.StronglyTypeIds;

namespace TestNest.Admin.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SocialMediaPlatformsController(
    ISocialMediaPlatformService socialMediaPlatformService,
    IErrorResponseService errorResponseService) : ControllerBase
{
    private readonly ISocialMediaPlatformService _socialMediaPlatformService = socialMediaPlatformService;
    private readonly IErrorResponseService _errorResponseService = errorResponseService;

    /// <summary>
    /// Creates a new social media platform.
    /// </summary>
    /// <param name="socialMediaPlatformForCreationRequest">The data for creating the social media platform.</param>
    /// <returns>The newly created social media platform.</returns>
    /// <response code="201">Returns the newly created <see cref="SocialMediaPlatformResponse"/>.</response>
    /// <response code="400">If the provided social media platform data is invalid.</response>
    /// <response code="409">If a social media platform with the same name already exists.</response>
    /// <response code="500">If an internal server error occurs during the creation process.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(SocialMediaPlatformResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSocialMediaPlatform(
        [FromBody] SocialMediaPlatformForCreationRequest socialMediaPlatformForCreationRequest)
    {
        Result<SocialMediaPlatformResponse> result = await _socialMediaPlatformService
            .CreateSocialMediaPlatformAsync(socialMediaPlatformForCreationRequest);

        if (result.IsSuccess)
        {
            SocialMediaPlatformResponse dto = result.Value!;
            return CreatedAtAction(
                nameof(GetAllSocialMediaPlatforms),
                new { socialMediaId = dto.Id },
                dto);
        }
        return HandleErrorResponse(result.ErrorType, result.Errors);
    }

    /// <summary>
    /// Updates an existing social media platform.
    /// </summary>
    /// <param name="socialMediaId">The ID of the social media platform to update.</param>
    /// <param name="socialMediaPlatformForUpdateRequest">The updated data for the social media platform.</param>
    /// <returns>Returns the updated social media platform.</returns>
    /// <response code="200">Returns the updated <see cref="SocialMediaPlatformResponse"/> if the update was successful.</response>
    /// <response code="400">If the provided <paramref name="socialMediaId"/> or the updated data is invalid.</response>
    /// <response code="404">If the social media platform with the given <paramref name="socialMediaId"/> is not found.</response>
    /// <response code="500">If an internal server error occurs during the update process.</response>
    [HttpPut("{socialMediaId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SocialMediaPlatformResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateSocialMediaPlatform(
        string socialMediaId,
        [FromBody] SocialMediaPlatformForUpdateRequest socialMediaPlatformForUpdateRequest)
    {
        Result<SocialMediaId> socialMediaIdValidatedResult = IdHelper
            .ValidateAndCreateId<SocialMediaId>(socialMediaId);

        if (!socialMediaIdValidatedResult.IsSuccess)
        {
            return HandleErrorResponse(
                socialMediaIdValidatedResult.ErrorType,
                socialMediaIdValidatedResult.Errors);
        }

        Result<SocialMediaPlatformResponse> updatedSocialMediaPlatform = await _socialMediaPlatformService
            .UpdateSocialMediaPlatformAsync(
                socialMediaIdValidatedResult.Value!,
                socialMediaPlatformForUpdateRequest);

        if (updatedSocialMediaPlatform.IsSuccess)
        {
            return Ok(updatedSocialMediaPlatform.Value!);
        }

        return HandleErrorResponse(
            updatedSocialMediaPlatform.ErrorType,
            updatedSocialMediaPlatform.Errors);
    }

    /// <summary>
    /// Deletes a social media platform.
    /// </summary>
    /// <param name="socialMediaId">The ID of the social media platform to delete.</param>
    /// <returns>Returns an empty response if the deletion was successful.</returns>
    /// <response code="204">If the social media platform was successfully deleted.</response>
    /// <response code="400">If the provided <paramref name="socialMediaId"/> is invalid.</response>
    /// <response code="404">If the social media platform with the given <paramref name="socialMediaId"/> is not found.</response>
    /// <response code="500">If an internal server error occurs during the deletion process.</response>
    [HttpDelete("{socialMediaId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteSocialMediaPlatform(string socialMediaId)
    {
        Result<SocialMediaId> socialMediaIdValidatedResult = IdHelper
            .ValidateAndCreateId<SocialMediaId>(socialMediaId);

        if (!socialMediaIdValidatedResult.IsSuccess)
        {
            return HandleErrorResponse(
                ErrorType.Validation,
                socialMediaIdValidatedResult.Errors);
        }

        Result result = await _socialMediaPlatformService.DeleteSocialMediaPlatformAsync(socialMediaIdValidatedResult.Value!);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return HandleErrorResponse(result.ErrorType, result.Errors);
    }

    /// <summary>
    /// Retrieves a paginated list of social media platforms based on specified query parameters.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of social media platforms to retrieve per page (default: 10).</param>
    /// <param name="sortBy">The field to sort the social media platforms by (default: "Id").</param>
    /// <param name="sortOrder">The sort order ("asc" for ascending, "desc" for descending, default: "asc").</param>
    /// <param name="name">Filter by social media platform name.</param>
    /// <param name="platformURL">Filter by social media platform URL.</param>
    /// <param name="socialMediaId">Filter by social media platform ID.</param>
    /// <returns>A paginated list of social media platforms.</returns>
    /// <response code="200">Returns a paginated list of <see cref="SocialMediaPlatformResponse"/>.</response>
    /// <response code="400">If the provided query parameters are invalid.</response>
    /// <response code="404">If no social media platforms are found based on the provided criteria.</response>
    /// <response code="500">If an internal server error occurs during the retrieval process.</response>
    [HttpGet(Name = "GetSocialMediaPlatforms")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<SocialMediaPlatformResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllSocialMediaPlatforms(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "Id",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string name = null,
        [FromQuery] string platformURL = null,
        [FromQuery] string socialMediaId = null)
    {
        if (!string.IsNullOrEmpty(socialMediaId))
        {
            Result<SocialMediaId> socialMediaIdValidatedResult = IdHelper.ValidateAndCreateId<SocialMediaId>(socialMediaId);
            if (!socialMediaIdValidatedResult.IsSuccess)
            {
                return HandleErrorResponse(socialMediaIdValidatedResult.ErrorType, socialMediaIdValidatedResult.Errors);
            }

            Result<SocialMediaPlatformResponse> singlePlatformResult = await _socialMediaPlatformService
                .GetSocialMediaPlatformByIdAsync(socialMediaIdValidatedResult.Value!);

            return singlePlatformResult.IsSuccess
                ? Ok(singlePlatformResult.Value!)
                : HandleErrorResponse(singlePlatformResult.ErrorType, singlePlatformResult.Errors);
        }
        else
        {
            var spec = new SocialMediaPlatformSpecification(
                name: name,
                platformURL: platformURL,
                sortBy: sortBy,
                sortDirection: sortOrder,
                pageNumber: pageNumber,
                pageSize: pageSize
            );

            Result<int> countResult = await _socialMediaPlatformService.CountAsync(spec);
            if (!countResult.IsSuccess)
            {
                return HandleErrorResponse(countResult.ErrorType, countResult.Errors);
            }
            int totalCount = countResult.Value;

            Result<IEnumerable<SocialMediaPlatformResponse>> socialMediaPlatformsResult = await _socialMediaPlatformService
                .GetAllSocialMediaPlatformsAsync(spec);
            if (!socialMediaPlatformsResult.IsSuccess)
            {
                return HandleErrorResponse(socialMediaPlatformsResult.ErrorType, socialMediaPlatformsResult.Errors);
            }

            IEnumerable<SocialMediaPlatformResponse> socialMediaPlatforms = socialMediaPlatformsResult.Value!;

            if (socialMediaPlatforms == null || !socialMediaPlatforms.Any())
            {
                var exception = SocialMediaPlatformException.NotFound();
                return HandleErrorResponse(
                    ErrorType.NotFound,
                    new[] { new Error(exception.Code.ToString(), exception.Message.ToString()) });
            }

            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var paginatedResponse = new PaginatedResponse<SocialMediaPlatformResponse>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                Data = socialMediaPlatforms,
                Links = new PaginatedLinks
                {
                    First = GeneratePaginationLink(1, pageSize, sortBy, sortOrder, name, platformURL),
                    Last = totalPages > 0 ? GeneratePaginationLink(totalPages, pageSize, sortBy, sortOrder, name, platformURL) : null,
                    Next = pageNumber < totalPages ? GeneratePaginationLink(pageNumber + 1, pageSize, sortBy, sortOrder, name, platformURL) : null,
                    Previous = pageNumber > 1 ? GeneratePaginationLink(pageNumber - 1, pageSize, sortBy, sortOrder, name, platformURL) : null
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
          string name = null,
          string platformURL = null)
          => Url.Action(
              nameof(GetAllSocialMediaPlatforms),
              "SocialMediaPlatforms",
              new
              {
                  pageNumber = targetPageNumber,
                  pageSize,
                  sortBy,
                  sortOrder,
                  name,
                  platformURL
              },
              protocol: Request.Scheme
          )!;

    private IActionResult HandleErrorResponse(ErrorType errorType, IEnumerable<Error> errors)
    {
        List<Error> safeErrors = errors?.ToList() ?? new List<Error>();

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
