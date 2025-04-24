using Microsoft.EntityFrameworkCore;
using TestNest.Admin.Application.Contracts.Interfaces.Persistence;
using TestNest.Admin.Application.Specifications.Common;
using TestNest.Admin.Domain.Employees;
using TestNest.Admin.Infrastructure.Persistence.Common;
using TestNest.Admin.SharedLibrary.Common.Results;
using TestNest.Admin.SharedLibrary.Exceptions;
using TestNest.Admin.SharedLibrary.Exceptions.Common;
using TestNest.Admin.SharedLibrary.StronglyTypeIds;

namespace TestNest.Admin.Infrastructure.Persistence.Repositories;

public class EmployeeRoleRepository(ApplicationDbContext employeeRoleRedbContext) : IEmployeeRoleRepository
{
    private readonly ApplicationDbContext _employeeRoleDbContext = employeeRoleRedbContext;

    public async Task<Result> AddAsync(EmployeeRole employeeRole)
    {
        try
        {
            await _employeeRoleDbContext.EmployeeRoles.AddAsync(employeeRole);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ErrorType.Internal, new Error("AddFailed", ex.Message));
        }
    }

    public async Task<Result> UpdateAsync(EmployeeRole employeeRole)
    {
        try
        {
            var exists = await _employeeRoleDbContext.EmployeeRoles
                .AsNoTracking()
                .AnyAsync(x => x.Id == employeeRole.Id);

            if (!exists)
            {
                var exception = EmployeeRoleException.NotFound();
                return Result.Failure(ErrorType.NotFound, new Error(exception.Code.ToString(), exception.Message.ToString()));
            }

            _employeeRoleDbContext.Update(employeeRole);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ErrorType.Internal, new Error("UpdateFailed", ex.Message));
        }
    }

    public async Task<Result> DeleteAsync(EmployeeRoleId employeeRoleId)
    {
        try
        {
            var rowsDeleted = await _employeeRoleDbContext.EmployeeRoles
                .Where(p => p.Id == employeeRoleId)
                .ExecuteDeleteAsync();

            return rowsDeleted > 0
                ? Result.Success()
                : Result.Failure(ErrorType.NotFound, new Error(EmployeeRoleException.NotFound().Code.ToString(), EmployeeRoleException.NotFound().Message.ToString()));
        }
        catch (Exception ex)
        {
            return Result.Failure(ErrorType.Internal, new Error("DeleteFailed", ex.Message));
        }
    }

    public async Task<Result<EmployeeRole>> GetByIdAsync(EmployeeRoleId employeeRoleId)
    {
        try
        {
            var employeeRole = await _employeeRoleDbContext.EmployeeRoles
                .FirstOrDefaultAsync(x => x.Id == employeeRoleId);

            if (employeeRole == null)
            {
                var exception = EmployeeRoleException.NotFound();
                return Result<EmployeeRole>.Failure(
                    ErrorType.NotFound,
                    new Error(exception.Code.ToString(), exception.Message.ToString()));
            }

            return Result<EmployeeRole>.Success(employeeRole);
        }
        catch (Exception ex)
        {
            return Result<EmployeeRole>.Failure(ErrorType.Internal, new Error("GetByIdFailed", ex.Message));
        }
    }

    public async Task<Result<EmployeeRole>> GetEmployeeRoleByNameAsync(string roleName)
    {
        try
        {
            var employeeRole = await _employeeRoleDbContext.EmployeeRoles
                .FirstOrDefaultAsync(x => x.RoleName.Name == roleName);

            if (employeeRole == null)
            {
                var exception = EmployeeRoleException.NotFound();
                return Result<EmployeeRole>.Failure(ErrorType.NotFound, new Error(exception.Code.ToString(), exception.Message.ToString()));
            }

            return Result<EmployeeRole>.Success(employeeRole);
        }
        catch (Exception ex)
        {
            return Result<EmployeeRole>.Failure(ErrorType.Internal, new Error("GetByNameFailed", ex.Message));
        }
    }

    public async Task DetachAsync(EmployeeRole employeeRole)
    {
        _employeeRoleDbContext.Entry(employeeRole).State = EntityState.Detached;
        await Task.CompletedTask;
    }

    public async Task<bool> RoleIdExists(EmployeeRoleId roleId)
        => await _employeeRoleDbContext.EmployeeRoles.AnyAsync(r => r.Id == roleId);

    public async Task<Result<IEnumerable<EmployeeRole>>> ListAsync(ISpecification<EmployeeRole> spec)
    {
        try
        {
            var query = _employeeRoleDbContext.EmployeeRoles
                .Include(x => x.RoleName)
                .AsQueryable();

            var employeeRoleSpec = (BaseSpecification<EmployeeRole>)spec;
            query = SpecificationEvaluator<EmployeeRole>.GetQuery(query, employeeRoleSpec);

            var employeeRoles = await query.ToListAsync();
            return Result<IEnumerable<EmployeeRole>>.Success(employeeRoles);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<EmployeeRole>>.Failure(ErrorType.Internal, new Error("DatabaseError", ex.Message));
        }
    }

    public async Task<Result<int>> CountAsync(ISpecification<EmployeeRole> spec)
    {
        try
        {
            var query = SpecificationEvaluator<EmployeeRole>.GetQuery(
                _employeeRoleDbContext.EmployeeRoles,
                (BaseSpecification<EmployeeRole>)spec);

            var count = await query.CountAsync();
            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure(ErrorType.Internal, new Error("DatabaseError", ex.Message));
        }
    }
}
