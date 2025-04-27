using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using TestNest.Admin.API.Helpers;
using TestNest.Admin.Application.Contracts.Interfaces.Service;
using TestNest.Admin.Application.Specifications.EmployeeSpecifications;
using TestNest.Admin.Domain.Employees;
using TestNest.Admin.SharedLibrary.Common.Results;
using TestNest.Admin.SharedLibrary.Dtos.Paginations;
using TestNest.Admin.SharedLibrary.Dtos.Requests.Employee;
using TestNest.Admin.SharedLibrary.Dtos.Responses;
using TestNest.Admin.SharedLibrary.Exceptions;
using TestNest.Admin.SharedLibrary.Exceptions.Common;
using TestNest.Admin.SharedLibrary.StronglyTypeIds;
using TestNest.Admin.Application.Mappings;

namespace TestNest.Admin.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController(
    IEmployeeService employeeService,
    IErrorResponseService errorResponseService) : ControllerBase
{
    private readonly IEmployeeService _employeeService = employeeService;
    private readonly IErrorResponseService _errorResponseService = errorResponseService;

    /// <summary>
    /// Creates a new employee.
    /// </summary>
    /// <param name="employeeForCreationRequest">The data for creating the employee.</param>
    /// <returns>The newly created employee.</returns>
    /// <response code="201">Returns the newly created <see cref="EmployeeResponse"/>.</response>
    /// <response code="400">If the provided employee data is invalid.</response>
    /// <response code="409">If creating the employee results in a conflict (e.g., duplicate employee number or email).</response>
    /// <response code="500">If an internal server error occurs during the creation process.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(EmployeeResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateEmployee(
        [FromBody] EmployeeForCreationRequest employeeForCreationRequest)
    {
        Result<EmployeeResponse> result = await _employeeService
            .CreateEmployeeAsync(employeeForCreationRequest);

        if (result.IsSuccess)
        {
            var dto = result.Value!;
            return CreatedAtAction(
                nameof(GetAllEmployees),
                new { employeeId = dto.EmployeeId },
                dto);
        }

        return HandleErrorResponse(result.ErrorType, result.Errors);
    }

    /// <summary>
    /// Partially updates an existing employee.
    /// </summary>
    /// <param name="employeeId">The ID of the employee to update.</param>
    /// <param name="patchDocument">A JSON Patch document containing the operations to apply to the employee.</param>
    /// <returns>Returns the updated employee.</returns>
    /// <response code="200">Returns the updated <see cref="EmployeeResponse"/> if the update was successful.</response>
    /// <response code="400">If the provided <paramref name="employeeId"/> is invalid, the patch document is invalid, or if the validation of the patched entity fails.</response>
    /// <response code="404">If the employee with the given <paramref name="employeeId"/> is not found.</response>
    /// <response code="409">If updating the employee results in a conflict (e.g., duplicate employee number or email for a different employee).</response>
    /// <response code="500">If an internal server error occurs during the update process.</response>
    [HttpPatch("{employeeId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EmployeeResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PatchEmployee(
        string employeeId,
        [FromBody] JsonPatchDocument<EmployeePatchRequest> patchDocument)
    {
        Result<EmployeeId> employeeIdValidatedResult = IdHelper.ValidateAndCreateId<EmployeeId>(employeeId);

        if (!employeeIdValidatedResult.IsSuccess)
        {
            return HandleErrorResponse(
                employeeIdValidatedResult.ErrorType,
                employeeIdValidatedResult.Errors);
        }

        var employeePatchRequest = new EmployeePatchRequest();
        patchDocument.ApplyTo(employeePatchRequest);

        if (!TryValidateModel(employeePatchRequest))
        {
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error("ValidationError", e.ErrorMessage))
                .ToList();

            return HandleErrorResponse(ErrorType.Validation, validationErrors);
        }

        Result<EmployeeResponse> result = await _employeeService
            .PatchEmployeeAsync(employeeIdValidatedResult.Value!,
                employeePatchRequest);

        if (result.IsSuccess)
        {
            return Ok(result.Value!); // Use the extension here
        }

        return HandleErrorResponse(result.ErrorType, result.Errors);
    }

    /// <summary>
    /// Updates an existing employee.
    /// </summary>
    /// <param name="employeeId">The ID of the employee to update.</param>
    /// <param name="employeeForUpdateRequest">The updated data for the employee.</param>
    /// <returns>Returns the updated employee.</returns>
    /// <response code="200">Returns the updated <see cref="EmployeeResponse"/> if the update was successful.</response>
    /// <response code="400">If the provided <paramref name="employeeId"/> is invalid or the updated employee data is invalid.</response>
    /// <response code="404">If the employee with the given <paramref name="employeeId"/> is not found.</response>
    /// <response code="409">If updating the employee results in a conflict (e.g., duplicate employee number or email for a different employee).</response>
    /// <response code="500">If an internal server error occurs during the update process.</response>
    [HttpPut("{employeeId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EmployeeResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateEmployee(
        string employeeId,
        [FromBody] EmployeeForUpdateRequest employeeForUpdateRequest)
    {
        Result<EmployeeId> employeeIdValidatedResult = IdHelper
            .ValidateAndCreateId<EmployeeId>(employeeId);

        if (!employeeIdValidatedResult.IsSuccess)
        {
            return HandleErrorResponse(
                employeeIdValidatedResult.ErrorType,
                employeeIdValidatedResult.Errors);
        }

        Result<EmployeeResponse> updatedEmployee = await _employeeService
            .UpdateEmployeeAsync(
                employeeIdValidatedResult.Value!,
                employeeForUpdateRequest);

        if (updatedEmployee.IsSuccess)
        {
            return Ok(updatedEmployee.Value!); // Use the extension here
        }

        return HandleErrorResponse(
            updatedEmployee.ErrorType,
            updatedEmployee.Errors);
    }

    /// <summary>
    /// Deletes an employee.
    /// </summary>
    /// <param name="employeeId">The ID of the employee to delete.</param>
    /// <returns>Returns an empty response if the deletion was successful.</returns>
    /// <response code="204">If the employee was successfully deleted.</response>
    /// <response code="400">If the provided <paramref name="employeeId"/> is invalid.</response>
    /// <response code="404">If the employee with the given <paramref name="employeeId"/> is not found.</response>
    /// <response code="500">If an internal server error occurs during the deletion process.</response>
    [HttpDelete("{employeeId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteEmployee(string employeeId)
    {
        Result<EmployeeId> employeeIdValidatedResult = IdHelper
            .ValidateAndCreateId<EmployeeId>(employeeId);

        if (!employeeIdValidatedResult.IsSuccess)
        {
            return HandleErrorResponse(
                ErrorType.Validation,
                employeeIdValidatedResult.Errors);
        }

        Result result = await _employeeService.DeleteEmployeeAsync(employeeIdValidatedResult.Value!);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return HandleErrorResponse(result.ErrorType, result.Errors);
    }

    /// <summary>
    /// Retrieves a paginated list of employees based on specified query parameters.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of employees to retrieve per page (default: 10).</param>
    /// <param name="sortBy">The field to sort the employees by (default: "EmployeeId").</param>
    /// <param name="sortOrder">The sort order ("asc" for ascending, "desc" for descending, default: "asc").</param>
    /// <param name="employeeId">Filter by employee ID.</param>
    /// <param name="employeeNumber">Filter by employee number.</param>
    /// <param name="firstName">Filter by first name.</param>
    /// <param name="middleName">Filter by middle name.</param>
    /// <param name="lastName">Filter by last name.</param>
    /// <param name="emailAddress">Filter by email address.</param>
    /// <param name="employeeStatusId">Filter by employee status ID.</param>
    /// <param name="employeeRoleId">Filter by employee role ID.</param>
    /// <param name="establishmentId">Filter by establishment ID.</param>
    /// <returns>A paginated list of employees.</returns>
    /// <response code="200">Returns a paginated list of <see cref="EmployeeResponse"/>.</response>
    /// <response code="400">If the provided query parameters are invalid.</response>
    /// <response code="404">If no employees are found based on the provided criteria.</response>
    /// <response code="500">If an internal server error occurs during the retrieval process.</response>
    [HttpGet(Name = "GetEmployees")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<EmployeeResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllEmployees(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "EmployeeId",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string employeeId = null,
        [FromQuery] string employeeNumber = null,
        [FromQuery] string firstName = null,
        [FromQuery] string middleName = null,
        [FromQuery] string lastName = null,
        [FromQuery] string emailAddress = null,
        [FromQuery] int? employeeStatusId = null,
        [FromQuery] string employeeRoleId = null,
        [FromQuery] string establishmentId = null)
    {
        if (!string.IsNullOrEmpty(employeeId))
        {
            Result<EmployeeId> employeeIdValidatedResult = IdHelper.ValidateAndCreateId<EmployeeId>(employeeId);
            if (!employeeIdValidatedResult.IsSuccess)
            {
                return HandleErrorResponse(employeeIdValidatedResult.ErrorType, employeeIdValidatedResult.Errors);
            }

            Result<EmployeeResponse> result = await _employeeService.GetEmployeeByIdAsync(employeeIdValidatedResult.Value!);
            return result.IsSuccess
                ? Ok(result.Value!)
                : HandleErrorResponse(result.ErrorType, result.Errors);
        }
        else
        {
            var spec = new EmployeeSpecification(
                employeeNumber: employeeNumber,
                firstName: firstName,
                middleName: middleName,
                lastName: lastName,
                emailAddress: emailAddress,
                employeeStatusId: employeeStatusId,
                employeeRoleId: employeeRoleId,
                establishmentId: establishmentId,
                sortBy: sortBy,
                sortDirection: sortOrder,
                pageNumber: pageNumber,
                pageSize: pageSize
            );

            Result<int> countResult = await _employeeService.CountAsync(spec);
            if (!countResult.IsSuccess)
            {
                return HandleErrorResponse(countResult.ErrorType, countResult.Errors);
            }
            int totalCount = countResult.Value;

            Result<IEnumerable<EmployeeResponse>> employeesResult = await _employeeService.GetAllEmployeesAsync(spec);
            if (!employeesResult.IsSuccess)
            {
                return HandleErrorResponse(employeesResult.ErrorType, employeesResult.Errors);
            }
            IEnumerable<EmployeeResponse> employees = employeesResult.Value!;

            if (employees == null || !employees.Any())
            {
                var exception = EmployeeException.NotFound();
                return HandleErrorResponse(
                    ErrorType.NotFound,
                    [new Error(exception.Code.ToString(), exception.Message.ToString())]);
            }

            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var paginatedResponse = new PaginatedResponse<EmployeeResponse>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                Data = employees,
                Links = new PaginatedLinks
                {
                    First = GenerateEmployeePaginationLink(1, pageSize, sortBy, sortOrder, employeeId, employeeNumber, firstName, middleName, lastName, emailAddress, employeeStatusId, employeeRoleId, establishmentId),
                    Last = totalPages > 0 ? GenerateEmployeePaginationLink(totalPages, pageSize, sortBy, sortOrder, employeeId, employeeNumber, firstName, middleName, lastName, emailAddress, employeeStatusId, employeeRoleId, establishmentId) : null,
                    Next = pageNumber < totalPages ? GenerateEmployeePaginationLink(pageNumber + 1, pageSize, sortBy, sortOrder, employeeId, employeeNumber, firstName, middleName, lastName, emailAddress, employeeStatusId, employeeRoleId, establishmentId) : null,
                    Previous = pageNumber > 1 ? GenerateEmployeePaginationLink(pageNumber - 1, pageSize, sortBy, sortOrder, employeeId, employeeNumber, firstName, middleName, lastName, emailAddress, employeeStatusId, employeeRoleId, establishmentId) : null
                }
            };

            return Ok(paginatedResponse);
        }
    }

    private string GenerateEmployeePaginationLink(
        int targetPageNumber,
        int pageSize,
        string sortBy,
        string sortOrder,
        string employeeId = null,
        string employeeNumber = null,
        string firstName = null,
        string middleName = null,
        string lastName = null,
        string emailAddress = null,
        int? employeeStatusId = null,
        string employeeRoleId = null,
        string establishmentId = null)
        => Url.Action(
            nameof(GetAllEmployees),
            "Employees",
            new
            {
                pageNumber = targetPageNumber,
                pageSize,
                sortBy,
                sortOrder,
                employeeId,
                employeeNumber,
                firstName,
                middleName,
                lastName,
                emailAddress,
                employeeStatusId,
                employeeRoleId,
                establishmentId
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

