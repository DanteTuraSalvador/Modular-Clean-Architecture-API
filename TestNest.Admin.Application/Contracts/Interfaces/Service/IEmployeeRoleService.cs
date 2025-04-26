//using TestNest.Admin.Application.Specifications.Common;
//using TestNest.Admin.Domain.Employees;
//using TestNest.Admin.SharedLibrary.Common.Results;
//using TestNest.Admin.SharedLibrary.Dtos.Requests.Employee;
//using TestNest.Admin.SharedLibrary.StronglyTypeIds;

//namespace TestNest.Admin.Application.Contracts.Interfaces.Service;

//// Interface defining the contract for an employee role service.
//public interface IEmployeeRoleService
//{
//    Task<Result<EmployeeRole>> CreateEmployeeRoleAsync(
//        EmployeeRoleForCreationRequest employeeRoleForCreationRequest);

//    Task<Result<EmployeeRole>> UpdateEmployeeRoleAsync(
//        EmployeeRoleId employeeRoleId,
//        EmployeeRoleForUpdateRequest employeeRoleForUpdateRequest);

//    Task<Result> DeleteEmployeeRoleAsync(EmployeeRoleId employeeRoleId);

//    Task<Result<EmployeeRole>> GetEmployeeRoleByIdAsync(EmployeeRoleId employeeRoleId);

//    Task<Result<IEnumerable<EmployeeRole>>> GetEmployeeRolessAsync(ISpecification<EmployeeRole> spec);

//    Task<Result<int>> CountAsync(ISpecification<EmployeeRole> spec);
//}
using TestNest.Admin.Application.Specifications.Common;
using TestNest.Admin.Domain.Employees;
using TestNest.Admin.SharedLibrary.Common.Results;
using TestNest.Admin.SharedLibrary.Dtos.Requests.Employee;
using TestNest.Admin.SharedLibrary.StronglyTypeIds;
using TestNest.Admin.SharedLibrary.Dtos.Responses; // Add this using

namespace TestNest.Admin.Application.Contracts.Interfaces.Service;

// Interface defining the contract for an employee role service.
public interface IEmployeeRoleService
{
    Task<Result<EmployeeRole>> CreateEmployeeRoleAsync(
        EmployeeRoleForCreationRequest employeeRoleForCreationRequest);

    Task<Result<EmployeeRole>> UpdateEmployeeRoleAsync(
        EmployeeRoleId employeeRoleId,
        EmployeeRoleForUpdateRequest employeeRoleForUpdateRequest);

    Task<Result> DeleteEmployeeRoleAsync(EmployeeRoleId employeeRoleId);

    Task<Result<EmployeeRoleResponse>> GetEmployeeRoleByIdAsync(EmployeeRoleId employeeRoleId); // Updated return type

    Task<Result<IEnumerable<EmployeeRoleResponse>>> GetEmployeeRolessAsync(ISpecification<EmployeeRole> spec); // Updated return type

    Task<Result<int>> CountAsync(ISpecification<EmployeeRole> spec);
}
