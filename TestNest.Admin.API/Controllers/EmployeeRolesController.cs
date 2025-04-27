
using Microsoft.AspNetCore.Mvc;
using TestNest.Admin.API.Helpers;
using TestNest.Admin.Application.Contracts.Interfaces.Service;
using TestNest.Admin.Application.Mappings; 
using TestNest.Admin.Application.Specifications.EmployeeRoleSpecifications;
using TestNest.Admin.Domain.Employees;
using TestNest.Admin.SharedLibrary.Common.Results;
using TestNest.Admin.SharedLibrary.Dtos.Paginations;
using TestNest.Admin.SharedLibrary.Dtos.Requests.Employee;
using TestNest.Admin.SharedLibrary.Dtos.Responses; 
using TestNest.Admin.SharedLibrary.Exceptions;
using TestNest.Admin.SharedLibrary.Exceptions.Common;
using TestNest.Admin.SharedLibrary.StronglyTypeIds;

namespace TestNest.Admin.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeRolesController(
    IEmployeeRoleService employeeRoleService,
    IErrorResponseService errorResponseService) : ControllerBase
{
    private readonly IEmployeeRoleService _employeeRoleService = employeeRoleService;
    private readonly IErrorResponseService _errorResponseService = errorResponseService;

    /// <summary>
    /// Creates a new employee role.
    /// </summary>
    /// <param name="employeeRoleForCreationRequest">The data for the new employee role.</param>
    /// <returns>The newly created employee role.</returns>
    /// <response code="201">Returns the newly created employee role.</response>
    /// <response code="400">If the request is invalid or validation fails.</response>
    /// <response code="409">If an employee role with the same name already exists.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(EmployeeRoleResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateEmployeeRole(
        [FromBody] EmployeeRoleForCreationRequest employeeRoleForCreationRequest)
    {
        Result<EmployeeRoleResponse> result = await _employeeRoleService
            .CreateEmployeeRoleAsync(employeeRoleForCreationRequest);

        if (result.IsSuccess)
        {
            EmployeeRoleResponse dto = result.Value!;
            return CreatedAtAction(
                nameof(GetAllEmployeeRoles),
                new { employeeRoleId = dto.Id }, dto);
        }

        return HandleErrorResponse(result.ErrorType, result.Errors);
    }

    /// <summary>
    /// Updates an existing employee role.
    /// </summary>
    /// <param name="employeeRoleId">The ID of the employee role to update.</param>
    /// <param name="employeeRoleForUpdateRequest">The updated data for the employee role.</param>
    /// <returns>The updated employee role.</returns>
    /// <response code="200">Returns the updated employee role.</response>
    /// <response code="400">If the request is invalid or validation fails.</response>
    /// <response code="404">If the employee role with the given ID is not found.</response>
    /// <response code="409">If an employee role with the same name already exists (excluding the one being updated).</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPut("{employeeRoleId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EmployeeRoleResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateEmployeeRole(
        string employeeRoleId,
        [FromBody] EmployeeRoleForUpdateRequest employeeRoleForUpdateRequest)
    {
        Result<EmployeeRoleId> employeeRoleIdValidatedResult = IdHelper
            .ValidateAndCreateId<EmployeeRoleId>(employeeRoleId);

        if (!employeeRoleIdValidatedResult.IsSuccess)
        {
            return HandleErrorResponse(
                employeeRoleIdValidatedResult.ErrorType,
                employeeRoleIdValidatedResult.Errors);
        }

        Result<EmployeeRole> updatedEmployeeRole = await _employeeRoleService
            .UpdateEmployeeRoleAsync(
                employeeRoleIdValidatedResult.Value!,
                employeeRoleForUpdateRequest);

        if (updatedEmployeeRole.IsSuccess)
        {
            return Ok(updatedEmployeeRole.Value!.ToEmployeeRoleResponse());
        }

        return HandleErrorResponse(
            updatedEmployeeRole.ErrorType,
            updatedEmployeeRole.Errors);
    }

    /// <summary>
    /// Deletes an employee role.
    /// </summary>
    /// <param name="employeeRoleId">The ID of the employee role to delete.</param>
    /// <returns>No content if the deletion was successful.</returns>
    /// <response code="204">If the employee role was successfully deleted.</response>
    /// <response code="400">If the provided employeeRoleId is invalid.</response>
    /// <response code="404">If the employee role with the given ID is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpDelete("{employeeRoleId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteEmployeeRole(string employeeRoleId)
    {
        Result<EmployeeRoleId> employeeRoleIdValidatedResult = IdHelper.ValidateAndCreateId<EmployeeRoleId>(employeeRoleId);

        if (!employeeRoleIdValidatedResult.IsSuccess)
        {
            return HandleErrorResponse(
                ErrorType.Validation,
                employeeRoleIdValidatedResult.Errors);
        }

        Result result = await _employeeRoleService
            .DeleteEmployeeRoleAsync(employeeRoleIdValidatedResult.Value!);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return HandleErrorResponse(result.ErrorType, result.Errors);
    }

    /// <summary>
    /// Gets a list of employee roles with optional filtering, sorting, and pagination, or a single employee role by ID.
    /// </summary>
    /// <param name="pageNumber">The page number for pagination (default: 1).</param>
    /// <param name="pageSize">The page size for pagination (default: 10).</param>
    /// <param name="sortBy">The field to sort by (default: EmployeeRoleId).</param>
    /// <param name="sortOrder">The sort order ("asc" or "desc", default: "asc").</param>
    /// <param name="roleName">Optional filter by role name.</param>
    /// <param name="employeeRoleId">Optional filter by employee role ID to get a single role.</param>
    /// <returns>A list of employee roles or a single employee role.</returns>
    /// <response code="200">Returns a list of employee roles or a single employee role.</response>
    /// <response code="400">If the provided employeeRoleId is invalid.</response>
    /// <response code="404">If no employee roles are found matching the criteria or the requested ID.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet(Name = "GetEmployeeRoles")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<EmployeeRoleResponse>))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EmployeeRoleResponse))] // For single role by ID
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllEmployeeRoles(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "EmployeeRoleId",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string roleName = null,
        [FromQuery] string employeeRoleId = null)
    {
        if (!string.IsNullOrEmpty(employeeRoleId))
        {
            Result<EmployeeRoleId> employeeRoleIdValidatedResult = IdHelper
                .ValidateAndCreateId<EmployeeRoleId>(employeeRoleId);

            if (!employeeRoleIdValidatedResult.IsSuccess)
            {
                return HandleErrorResponse(employeeRoleIdValidatedResult.ErrorType, employeeRoleIdValidatedResult.Errors);
            }

            Result<EmployeeRoleResponse> result = await _employeeRoleService
                .GetEmployeeRoleByIdAsync(employeeRoleIdValidatedResult.Value!);

            return result.IsSuccess ? Ok(result.Value!)
                : HandleErrorResponse(result.ErrorType, result.Errors);
        }
        else
        {
            var spec = new EmployeeRoleSpecification(
                roleName: roleName,
                employeeRoleId: employeeRoleId,
                sortBy: sortBy,
                sortDirection: sortOrder,
                pageNumber: pageNumber,
                pageSize: pageSize
            );

            Result<int> countResult = await _employeeRoleService.CountAsync(spec);
            if (!countResult.IsSuccess)
            { return HandleErrorResponse(countResult.ErrorType, countResult.Errors); }
            int totalCount = countResult.Value;

            Result<IEnumerable<EmployeeRoleResponse>> employeeRolesResult = await _employeeRoleService.GetEmployeeRolessAsync(spec);
            if (!employeeRolesResult.IsSuccess)
            {
                return HandleErrorResponse(employeeRolesResult.ErrorType, employeeRolesResult.Errors);
            }

            IEnumerable<EmployeeRoleResponse> employeeRoles = employeeRolesResult.Value!;

            if (employeeRoles == null || !employeeRoles.Any())
            {
                var exception = EmployeeRoleException.NotFound();
                return HandleErrorResponse(
                    ErrorType.NotFound,
                    [new Error(exception.Code.ToString(), exception.Message.ToString())]);
            }

            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var paginatedResponse = new PaginatedResponse<EmployeeRoleResponse>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                Data = employeeRoles,
                Links = new PaginatedLinks
                {
                    First = GeneratePaginationLink(1, pageSize, sortBy, sortOrder, roleName),
                    Last = totalPages > 0 ? GeneratePaginationLink(totalPages, pageSize, sortBy, sortOrder, roleName) : null,
                    Next = pageNumber < totalPages ? GeneratePaginationLink(pageNumber + 1, pageSize, sortBy, sortOrder, roleName) : null,
                    Previous = pageNumber > 1 ? GeneratePaginationLink(pageNumber - 1, pageSize, sortBy, sortOrder, roleName) : null
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
       string roleName)
       => Url.Action(
           nameof(GetAllEmployeeRoles),
           "EmployeeRoles",
           new
           {
               pageNumber = targetPageNumber,
               pageSize,
               sortBy,
               sortOrder,
               roleName
           },
           protocol: Request.Scheme
       )!;

    private IActionResult HandleErrorResponse(ErrorType errorType, IEnumerable<Error> errors)
    {
        List<Error> safeErrors = errors?.ToList() ?? new List<Error>();

        return errorType switch
        {
            ErrorType.Validation => _errorResponseService.CreateProblemDetails(StatusCodes.Status400BadRequest, "Validation Error", "Validation failed.", new Dictionary<string, object> { { "errors", safeErrors } }),
            ErrorType.NotFound => _errorResponseService.CreateProblemDetails(StatusCodes.Status404NotFound, "Not Found", "Resource not found.", new Dictionary<string, object> { { "errors", safeErrors } }),
            ErrorType.Conflict => _errorResponseService.CreateProblemDetails(StatusCodes.Status409Conflict, "Conflict", "Resource conflict.", new Dictionary<string, object> { { "errors", safeErrors } }),
            _ => _errorResponseService.CreateProblemDetails(StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };
    }
}
