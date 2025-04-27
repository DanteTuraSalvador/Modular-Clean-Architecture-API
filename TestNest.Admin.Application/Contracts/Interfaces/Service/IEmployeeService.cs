using TestNest.Admin.Application.Specifications.EmployeeSpecifications;
using TestNest.Admin.SharedLibrary.Common.Results;
using TestNest.Admin.SharedLibrary.Dtos.Requests.Employee;
using TestNest.Admin.SharedLibrary.Dtos.Responses;
using TestNest.Admin.SharedLibrary.StronglyTypeIds;

namespace TestNest.Admin.Application.Contracts.Interfaces.Service;


public interface IEmployeeService
{
    Task<Result<EmployeeResponse>> CreateEmployeeAsync(EmployeeForCreationRequest employeeForCreationRequest);

    Task<Result<EmployeeResponse>> UpdateEmployeeAsync(EmployeeId employeeId, EmployeeForUpdateRequest employeeForUpdateRequest);

    Task<Result> DeleteEmployeeAsync(EmployeeId employeeId);

    Task<Result<EmployeeResponse>> GetEmployeeByIdAsync(EmployeeId employeeId);

    Task<Result<EmployeeResponse>> PatchEmployeeAsync(EmployeeId employeeId, EmployeePatchRequest employeePatchRequest);

    Task<Result<IEnumerable<EmployeeResponse>>> GetAllEmployeesAsync(EmployeeSpecification spec);

    Task<Result<int>> CountAsync(EmployeeSpecification spec);
}

