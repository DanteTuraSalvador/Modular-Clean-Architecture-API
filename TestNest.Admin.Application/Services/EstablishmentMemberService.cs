﻿using System.Transactions;
using Microsoft.Extensions.Logging;
using TestNest.Admin.Application.Contracts.Common;
using TestNest.Admin.Application.Contracts.Interfaces.Persistence;
using TestNest.Admin.Application.Contracts.Interfaces.Service;
using TestNest.Admin.Application.Interfaces;
using TestNest.Admin.Application.Services.Base;
using TestNest.Admin.Application.Specifications.Common;
using TestNest.Admin.Domain.Employees;
using TestNest.Admin.Domain.Establishments;
using TestNest.Admin.SharedLibrary.Common.Results;
using TestNest.Admin.SharedLibrary.Dtos.Requests.Establishment;
using TestNest.Admin.SharedLibrary.Exceptions.Common;
using TestNest.Admin.SharedLibrary.Helpers;
using TestNest.Admin.SharedLibrary.StronglyTypeIds;
using TestNest.Admin.SharedLibrary.ValueObjects;

namespace TestNest.Admin.Application.Services;

public class EstablishmentMemberService(
    IEstablishmentMemberRepository establishmentMemberRepository,
    IEstablishmentRepository establishmentRepository,
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork,
    IDatabaseExceptionHandlerFactory exceptionHandlerFactory,
    ILogger<EstablishmentMemberService> logger) : BaseService(unitOfWork, logger, exceptionHandlerFactory), IEstablishmentMemberService
{
    private readonly IEstablishmentMemberRepository _establishmentMemberRepository = establishmentMemberRepository;
    private readonly IEstablishmentRepository _establishmentRepository = establishmentRepository;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly ILogger<EstablishmentMemberService> _logger = logger;

    public async Task<Result<EstablishmentMember>> GetEstablishmentMemberByIdAsync(EstablishmentMemberId establishmentMemberId)
        => await _establishmentMemberRepository.GetByIdAsync(establishmentMemberId);

    public async Task<Result<EstablishmentMember>> CreateEstablishmentMemberAsync(EstablishmentMemberForCreationRequest establishmentMemberForCreationRequest)
    {
        using var scope = new TransactionScope(TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);
        try
        {
            Result<EstablishmentId> establishmentIdResult = IdHelper
                .ValidateAndCreateId<EstablishmentId>(establishmentMemberForCreationRequest.EstablishmentId);
            Result<EmployeeId> employeeIdResult = IdHelper
                .ValidateAndCreateId<EmployeeId>(establishmentMemberForCreationRequest.EmployeeId);
            Result<MemberTitle> memberTitleResult = MemberTitle.Create(establishmentMemberForCreationRequest.MemberTitle);
            Result<MemberDescription> memberDescriptionResult = MemberDescription.Create(establishmentMemberForCreationRequest.MemberDescription);
            Result<MemberTag> memberTagResult = MemberTag.Create(establishmentMemberForCreationRequest.MemberTag);

            var combinedValidationResult = Result.Combine(
                establishmentIdResult.ToResult(),
                employeeIdResult.ToResult(),
                memberTitleResult.ToResult(),
                memberDescriptionResult.ToResult(),
                memberTagResult.ToResult());

            if (!combinedValidationResult.IsSuccess)
            {
                return Result<EstablishmentMember>.Failure(
                    ErrorType.Validation,
                    [.. combinedValidationResult.Errors]);
            }

            Result<Establishment> establishmentResult = await _establishmentRepository
                .GetByIdAsync(establishmentIdResult.Value!);

            if (!establishmentResult.IsSuccess)
            {
                return Result<EstablishmentMember>.Failure(
                    establishmentResult.ErrorType,
                    [.. establishmentResult.Errors]);
            }

            // Fetch the Employee using the EmployeeRepository
            Result<Domain.Employees.Employee> employeeResult = await _employeeRepository.GetByIdAsync(employeeIdResult.Value!);
            if (!employeeResult.IsSuccess)
            {
                return Result<EstablishmentMember>.Failure(
                    ErrorType.NotFound,
                    new Error("NotFound", $"Employee with ID '{employeeIdResult.Value}' not found."));
            }

            // Check if the Employee's EstablishmentId matches the requested EstablishmentId
            if (employeeResult.Value!.EstablishmentId != establishmentIdResult.Value!)
            {
                return Result<EstablishmentMember>.Failure(
                    ErrorType.Validation,
                    new Error("Validation", $"Employee with ID '{employeeIdResult.Value}' does not belong to Establishment with ID '{establishmentIdResult.Value}'."));
            }

            // Check for uniqueness of employee within the establishment
            Result<bool> uniquenessCheckResult = await EstablishmentMemberWithEmployeeExistsAsync(
                employeeIdResult.Value!,
                establishmentIdResult.Value!);

            if (!uniquenessCheckResult.IsSuccess || uniquenessCheckResult.Value)
            {
                return Result<EstablishmentMember>.Failure(
                    ErrorType.Conflict,
                    new Error("Validation", $"Employee with ID '{employeeIdResult.Value}' already exists as a member in this establishment."));
            }

            Result<EstablishmentMember> establishmentMemberResult = EstablishmentMember.Create(
               establishmentId: establishmentIdResult.Value!,
               employeeId: employeeIdResult.Value!,
               title: memberTitleResult.Value!,
               description: memberDescriptionResult.Value!,
               tag: memberTagResult.Value!
           );

            if (!establishmentMemberResult.IsSuccess)
            {
                return establishmentMemberResult;
            }

            EstablishmentMember newMember = establishmentMemberResult.Value!;
            _ = await _establishmentMemberRepository.AddAsync(newMember);
            Result<EstablishmentMember> commitResult = await SafeCommitAsync(() => Result<EstablishmentMember>.Success(newMember));

            if (commitResult.IsSuccess)
            {
                scope.Complete();
                return commitResult;
            }

            return commitResult;
        }
        catch (Exception ex)
        {
            return Result<EstablishmentMember>.Failure(ErrorType.Internal, [new Error("ServiceError", ex.Message)]);
        }
    }

    public async Task<Result<EstablishmentMember>> UpdateEstablishmentMemberAsync(
        EstablishmentMemberId establishmentMemberId,
        EstablishmentMemberForUpdateRequest establishmentMemberForUpdateRequest)
    {
        using var scope = new TransactionScope(TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);
        try
        {
            Result<EstablishmentId> establishmentIdResult = IdHelper
                .ValidateAndCreateId<EstablishmentId>(establishmentMemberForUpdateRequest.EstablishmentId);
            Result<MemberTitle> memberTitleResult = MemberTitle.Create(establishmentMemberForUpdateRequest.MemberTitle!);
            Result<MemberDescription> memberDescriptionResult = MemberDescription.Create(establishmentMemberForUpdateRequest.MemberDescription!);
            Result<MemberTag> memberTagResult = MemberTag.Create(establishmentMemberForUpdateRequest.MemberTag!);

            var combinedValidationResult = Result.Combine(
                establishmentIdResult.ToResult(),
                memberTitleResult.ToResult(),
                memberDescriptionResult.ToResult(),
                memberTagResult.ToResult());

            if (!combinedValidationResult.IsSuccess)
            {
                return Result<EstablishmentMember>.Failure(
                    ErrorType.Validation,
                    [.. combinedValidationResult.Errors]);
            }

            EstablishmentId updateEstablishmentId = establishmentIdResult.Value!;
            bool establishmentExists = await _establishmentRepository.ExistsAsync(updateEstablishmentId);
            if (!establishmentExists)
            {
                return Result<EstablishmentMember>.Failure(
                    ErrorType.NotFound,
                    new Error("NotFound", $"Establishment with ID '{updateEstablishmentId}' not found."));
            }

            Result<EstablishmentMember> existingMemberResult = await _establishmentMemberRepository
                .GetByIdAsync(establishmentMemberId);
            if (!existingMemberResult.IsSuccess)
            {
                return existingMemberResult;
            }
            EstablishmentMember existingMember = existingMemberResult.Value!;
            await _establishmentMemberRepository.DetachAsync(existingMember);

            if (existingMember.EstablishmentId != updateEstablishmentId)
            {
                return Result<EstablishmentMember>.Failure(
                    ErrorType.Unauthorized,
                    new Error("Unauthorized", $"Cannot update member. The provided EstablishmentId '{updateEstablishmentId}' does not match the existing member's EstablishmentId '{existingMember.EstablishmentId}'."));
            }

            Result<Domain.Employees.Employee> employeeResult = await _employeeRepository.GetByIdAsync(existingMember.EmployeeId);
            if (!employeeResult.IsSuccess)
            {
                return Result<EstablishmentMember>.Failure(
                    ErrorType.NotFound,
                    new Error("NotFound", $"Employee with ID '{existingMember.EmployeeId}' not found."));
            }

            if (employeeResult.Value!.EstablishmentId != updateEstablishmentId)
            {
                return Result<EstablishmentMember>.Failure(
                    ErrorType.Validation,
                    new Error("Validation", $"Employee with ID '{existingMember.EmployeeId}' does not belong to Establishment with ID '{updateEstablishmentId}'."));
            }

            Result<EstablishmentMember> updatedMemberResult = existingMember
                .WithTitle(memberTitleResult.Value!)
                .Bind(member => member.WithDescription(memberDescriptionResult.Value!))
                .Bind(member => member.WithTag(memberTagResult.Value!));

            if (!updatedMemberResult.IsSuccess)
            {
                return updatedMemberResult;
            }

            EstablishmentMember updatedMember = updatedMemberResult.Value!;

            Result<bool> uniquenessCheckResult = await EstablishmentMemberWithEmployeeExistsAsync(
                updatedMember.EmployeeId,
                updateEstablishmentId,
                establishmentMemberId);

            if (!uniquenessCheckResult.IsSuccess || uniquenessCheckResult.Value)
            {
                return Result<EstablishmentMember>.Failure(
                    ErrorType.Conflict,
                    new Error("Validation", $"Employee with ID '{updatedMember.EmployeeId}' already exists as a member in this establishment."));
            }

            Result<EstablishmentMember> updateResult = await _establishmentMemberRepository.UpdateAsync(updatedMember);
            Result<EstablishmentMember> commitResult = await SafeCommitAsync(() => updateResult);

            if (commitResult.IsSuccess)
            {
                scope.Complete();
                return commitResult;
            }
            return commitResult;
        }
        catch (Exception ex)
        {
            return Result<EstablishmentMember>.Failure(ErrorType.Internal, new Error("ServiceError", ex.Message));
        }
    }

    //public async Task<Result<EstablishmentMember>> PatchEstablishmentMemberAsync(
    //    EstablishmentMemberId establishmentMemberId,
    //    EstablishmentMemberPatchRequest establishmentMemberPatchRequest)
    //{
    //    using var scope = new TransactionScope(TransactionScopeOption.Required,
    //        new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
    //        TransactionScopeAsyncFlowOption.Enabled);
    //    try
    //    {
    //        Result<EstablishmentMember> existingMemberResult = await _establishmentMemberRepository
    //            .GetByIdAsync(establishmentMemberId);
    //        if (!existingMemberResult.IsSuccess)
    //        {
    //            return existingMemberResult;
    //        }
    //        EstablishmentMember existingMember = existingMemberResult.Value!;
    //        await _establishmentMemberRepository.DetachAsync(existingMember);

    //        var currentMemberResult = Result<EstablishmentMember>.Success(existingMember);

    //        if (establishmentMemberPatchRequest.MemberTitle is not null)
    //        {
    //            currentMemberResult = currentMemberResult.Bind(member => member.WithTitle(MemberTitle.Create(establishmentMemberPatchRequest.MemberTitle).Value!));
    //            if (!currentMemberResult.IsSuccess)
    //            {
    //                return currentMemberResult;
    //            }
    //        }

    //        if (establishmentMemberPatchRequest.MemberDescription is not null)
    //        {
    //            currentMemberResult = currentMemberResult.Bind(member => member.WithDescription(MemberDescription.Create(establishmentMemberPatchRequest.MemberDescription).Value!));
    //            if (!currentMemberResult.IsSuccess)
    //            {
    //                return currentMemberResult;
    //            }
    //        }

    //        if (establishmentMemberPatchRequest.MemberTag is not null)
    //        {
    //            currentMemberResult = currentMemberResult.Bind(member => member.WithTag(MemberTag.Create(establishmentMemberPatchRequest.MemberTag).Value!));
    //            if (!currentMemberResult.IsSuccess)
    //            {
    //                return currentMemberResult;
    //            }
    //        }

    //        EstablishmentMember updatedMember = currentMemberResult.Value!;
    //        Result<EstablishmentMember> updateResult = await _establishmentMemberRepository.UpdateAsync(updatedMember);
    //        Result<EstablishmentMember> commitResult = await SafeCommitAsync(() => updateResult);

    //        if (commitResult.IsSuccess)
    //        {
    //            scope.Complete();
    //            return commitResult;
    //        }
    //        return commitResult;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error patching establishment member {EstablishmentMemberId}.", establishmentMemberId);
    //        return Result<EstablishmentMember>.Failure(ErrorType.Internal, [new Error("ServiceError", ex.Message)]);
    //    }
    //}

    //public async Task<Result<EstablishmentMember>> PatchEstablishmentMemberAsync(
    //    EstablishmentMemberId establishmentMemberId,
    //    EstablishmentMemberPatchRequest establishmentMemberPatchRequest)
    //{
    //    using var scope = new TransactionScope(TransactionScopeOption.Required,
    //        new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
    //        TransactionScopeAsyncFlowOption.Enabled);
    //    try
    //    {
    //        Result<EstablishmentMember> existingMemberResult = await _establishmentMemberRepository
    //            .GetByIdAsync(establishmentMemberId);
    //        if (!existingMemberResult.IsSuccess)
    //        {
    //            return existingMemberResult;
    //        }
    //        EstablishmentMember existingMember = existingMemberResult.Value!;
    //        await _establishmentMemberRepository.DetachAsync(existingMember);

    //        var currentMemberResult = Result<EstablishmentMember>.Success(existingMember);
    //        EstablishmentId? updatedEstablishmentId = null;
    //        EmployeeId? updatedEmployeeId = null;

    //        // Check if EstablishmentId is being patched
    //        if (establishmentMemberPatchRequest.EstablishmentId is not null)
    //        {
    //            var establishmentIdResult = IdHelper.ValidateAndCreateId<EstablishmentId>(establishmentMemberPatchRequest.EstablishmentId);
    //            if (!establishmentIdResult.IsSuccess)
    //            {
    //                return Result<EstablishmentMember>.Failure(ErrorType.Validation, establishmentIdResult.Errors);
    //            }
    //            updatedEstablishmentId = establishmentIdResult.Value!;
    //            currentMemberResult = currentMemberResult.Bind(member => member.WithEstablishmentId(updatedEstablishmentId));
    //            if (!currentMemberResult.IsSuccess)
    //                return currentMemberResult;
    //        }

    //        // Check if EmployeeId is being patched
    //        if (establishmentMemberPatchRequest.EmployeeId is not null)
    //        {
    //            var employeeIdResult = IdHelper.ValidateAndCreateId<EmployeeId>(establishmentMemberPatchRequest.EmployeeId);
    //            if (!employeeIdResult.IsSuccess)
    //            {
    //                return Result<EstablishmentMember>.Failure(ErrorType.Validation, employeeIdResult.Errors);
    //            }
    //            updatedEmployeeId = employeeIdResult.Value!;
    //            currentMemberResult = currentMemberResult.Bind(member => member.WithEmployeeId(updatedEmployeeId));
    //            if (!currentMemberResult.IsSuccess)
    //                return currentMemberResult;
    //        }

    //        if (establishmentMemberPatchRequest.MemberTitle is not null)
    //        {
    //            currentMemberResult = currentMemberResult.Bind(member => member.WithTitle(MemberTitle.Create(establishmentMemberPatchRequest.MemberTitle).Value!));
    //            if (!currentMemberResult.IsSuccess)
    //                return currentMemberResult;
    //        }

    //        if (establishmentMemberPatchRequest.MemberDescription is not null)
    //        {
    //            currentMemberResult = currentMemberResult.Bind(member => member.WithDescription(MemberDescription.Create(establishmentMemberPatchRequest.MemberDescription).Value!));
    //            if (!currentMemberResult.IsSuccess)
    //                return currentMemberResult;
    //        }

    //        if (establishmentMemberPatchRequest.MemberTag is not null)
    //        {
    //            currentMemberResult = currentMemberResult.Bind(member => member.WithTag(MemberTag.Create(establishmentMemberPatchRequest.MemberTag).Value!));
    //            if (!currentMemberResult.IsSuccess)
    //                return currentMemberResult;
    //        }

    //        EstablishmentMember updatedMember = currentMemberResult.Value!;

    //        // Validation logic based on which IDs are being patched
    //        if (updatedEmployeeId.HasValue)
    //        {
    //            var employeeResult = await _employeeRepository.GetByIdAsync(updatedEmployeeId.Value);
    //            if (!employeeResult.IsSuccess)
    //            {
    //                return Result<EstablishmentMember>.Failure(
    //                    ErrorType.NotFound,
    //                    new Error("NotFound", $"Employee with ID '{updatedEmployeeId.Value}' not found."));
    //            }

    //            if (updatedEstablishmentId.HasValue)
    //            {
    //                // Both EmployeeId and EstablishmentId are being patched
    //                if (employeeResult.Value!.EstablishmentId != updatedEstablishmentId.Value)
    //                {
    //                    return Result<EstablishmentMember>.Failure(
    //                        ErrorType.Validation,
    //                        new Error("Validation", $"Employee with ID '{updatedEmployeeId.Value}' does not belong to Establishment with ID '{updatedEstablishmentId.Value}'."));
    //                }
    //            }
    //            else
    //            {
    //                // Only EmployeeId is being patched, validate against the existing EstablishmentId
    //                if (employeeResult.Value!.EstablishmentId != updatedMember.EstablishmentId)
    //                {
    //                    return Result<EstablishmentMember>.Failure(
    //                        ErrorType.Validation,
    //                        new Error("Validation", $"Employee with ID '{updatedEmployeeId.Value}' does not belong to Establishment with ID '{updatedMember.EstablishmentId}'."));
    //                }
    //            }
    //        }
    //        else if (updatedEstablishmentId.HasValue)
    //        {
    //            // Only EstablishmentId is being patched, validate the existing employee against the new EstablishmentId
    //            var employeeResult = await _employeeRepository.GetByIdAsync(updatedMember.EmployeeId);
    //            if (!employeeResult.IsSuccess)
    //            {
    //                return Result<EstablishmentMember>.Failure(
    //                    ErrorType.NotFound,
    //                    new Error("NotFound", $"Employee with ID '{updatedMember.EmployeeId}' not found."));
    //            }

    //            if (employeeResult.Value!.EstablishmentId != updatedEstablishmentId.Value)
    //            {
    //                return Result<EstablishmentMember>.Failure(
    //                    ErrorType.Validation,
    //                    new Error("Validation", $"Employee with ID '{updatedMember.EmployeeId}' does not belong to Establishment with ID '{updatedEstablishmentId.Value}'."));
    //            }
    //        }
    //        // If neither updatedEmployeeId nor updatedEstablishmentId have a value, no affiliation validation needed

    //        Result<EstablishmentMember> updateResult = await _establishmentMemberRepository.UpdateAsync(updatedMember);
    //        Result<EstablishmentMember> commitResult = await SafeCommitAsync(() => updateResult);

    //        if (commitResult.IsSuccess)
    //        {
    //            scope.Complete();
    //            return commitResult;
    //        }
    //        return commitResult;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error patching establishment member {EstablishmentMemberId}.", establishmentMemberId);
    //        return Result<EstablishmentMember>.Failure(ErrorType.Internal, [new Error("ServiceError", ex.Message)]);
    //    }
    //}

    //public async Task<Result<EstablishmentMember>> PatchEstablishmentMemberAsync(
    //     EstablishmentMemberId establishmentMemberId,
    //     EstablishmentMemberPatchRequest establishmentMemberPatchRequest)
    //{
    //    using var scope = new TransactionScope(TransactionScopeOption.Required,
    //        new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
    //        TransactionScopeAsyncFlowOption.Enabled);
    //    try
    //    {
    //        Result<EstablishmentMember> existingMemberResult = await _establishmentMemberRepository
    //            .GetByIdAsync(establishmentMemberId);
    //        if (!existingMemberResult.IsSuccess)
    //        {
    //            return existingMemberResult;
    //        }
    //        EstablishmentMember existingMember = existingMemberResult.Value!;
    //        await _establishmentMemberRepository.DetachAsync(existingMember);

    //        EstablishmentId updatedEstablishmentId = existingMember.EstablishmentId;
    //        EmployeeId updatedEmployeeId = existingMember.EmployeeId;
    //        MemberTitle updatedTitle = existingMember.MemberTitle;
    //        MemberDescription updatedDescription = existingMember.MemberDescription;
    //        MemberTag updatedTag = existingMember.MemberTag;

    //        // Apply patches
    //        if (establishmentMemberPatchRequest.EstablishmentId is not null)
    //        {
    //            Result<EstablishmentId> establishmentIdResult = IdHelper.ValidateAndCreateId<EstablishmentId>(establishmentMemberPatchRequest.EstablishmentId);
    //            if (!establishmentIdResult.IsSuccess)
    //            {
    //                return Result<EstablishmentMember>.Failure(ErrorType.Validation, establishmentIdResult.Errors);
    //            }
    //            updatedEstablishmentId = establishmentIdResult.Value!;
    //        }

    //        if (establishmentMemberPatchRequest.EmployeeId is not null)
    //        {
    //            Result<EmployeeId> employeeIdResult = IdHelper.ValidateAndCreateId<EmployeeId>(establishmentMemberPatchRequest.EmployeeId);
    //            if (!employeeIdResult.IsSuccess)
    //            {
    //                return Result<EstablishmentMember>.Failure(ErrorType.Validation, employeeIdResult.Errors);
    //            }
    //            updatedEmployeeId = employeeIdResult.Value!;
    //        }

    //        if (establishmentMemberPatchRequest.MemberTitle is not null)
    //        {
    //            Result<MemberTitle> titleResult = MemberTitle.Create(establishmentMemberPatchRequest.MemberTitle);
    //            if (!titleResult.IsSuccess)
    //            {
    //                return Result<EstablishmentMember>.Failure(ErrorType.Validation, titleResult.Errors);
    //            }

    //            updatedTitle = titleResult.Value!;
    //        }

    //        if (establishmentMemberPatchRequest.MemberDescription is not null)
    //        {
    //            Result<MemberDescription> descriptionResult = MemberDescription.Create(establishmentMemberPatchRequest.MemberDescription);
    //            if (!descriptionResult.IsSuccess)
    //            {
    //                return Result<EstablishmentMember>.Failure(ErrorType.Validation, descriptionResult.Errors);
    //            }

    //            updatedDescription = descriptionResult.Value!;
    //        }

    //        if (establishmentMemberPatchRequest.MemberTag is not null)
    //        {
    //            Result<MemberTag> tagResult = MemberTag.Create(establishmentMemberPatchRequest.MemberTag);
    //            if (!tagResult.IsSuccess)
    //            {
    //                return Result<EstablishmentMember>.Failure(ErrorType.Validation, tagResult.Errors);
    //            }

    //            updatedTag = tagResult.Value!;
    //        }

    //        // Validate employee's affiliation with the establishment
    //        Result<Employee> employeeResult = await _employeeRepository.GetByIdAsync(updatedEmployeeId);
    //        if (!employeeResult.IsSuccess)
    //        {
    //            return Result<EstablishmentMember>.Failure(
    //                ErrorType.NotFound,
    //                new Error("NotFound", $"Employee with ID '{updatedEmployeeId}' not found."));
    //        }

    //        if (employeeResult.Value!.EstablishmentId != updatedEstablishmentId)
    //        {
    //            return Result<EstablishmentMember>.Failure(
    //                ErrorType.Validation,
    //                new Error("Validation", $"Employee with ID '{updatedEmployeeId}' does not belong to Establishment with ID '{updatedEstablishmentId}'."));
    //        }

    //        // Create the updated EstablishmentMember instance
    //        Result<EstablishmentMember> updatedMemberResult = EstablishmentMember.Create(
    //            updatedEstablishmentId,
    //            updatedEmployeeId,
    //            updatedDescription,
    //            updatedTag,
    //            updatedTitle);

    //        if (!updatedMemberResult.IsSuccess)
    //        {
    //            return updatedMemberResult;
    //        }

    //        EstablishmentMember updatedMember = updatedMemberResult.Value!;

    //        Result<EstablishmentMember> updateResult = await _establishmentMemberRepository.UpdateAsync(updatedMember);
    //        Result<EstablishmentMember> commitResult = await SafeCommitAsync(() => updateResult);

    //        if (commitResult.IsSuccess)
    //        {
    //            scope.Complete();
    //            return commitResult;
    //        }
    //        return commitResult;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error patching establishment member {EstablishmentMemberId}.", establishmentMemberId);
    //        return Result<EstablishmentMember>.Failure(ErrorType.Internal, [new Error("ServiceError", ex.Message)]);
    //    }
    //}

    public async Task<Result<EstablishmentMember>> PatchEstablishmentMemberAsync(
        EstablishmentMemberId establishmentMemberId,
        EstablishmentMemberPatchRequest establishmentMemberPatchRequest)
    {
        using var scope = new TransactionScope(TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);
        try
        {
            Result<EstablishmentMember> existingMemberResult = await _establishmentMemberRepository
                .GetByIdAsync(establishmentMemberId);
            if (!existingMemberResult.IsSuccess)
            {
                return existingMemberResult;
            }
            EstablishmentMember existingMember = existingMemberResult.Value!;
            await _establishmentMemberRepository.DetachAsync(existingMember);

            EstablishmentId updatedEstablishmentId = existingMember.EstablishmentId;
            EmployeeId updatedEmployeeId = existingMember.EmployeeId;
            MemberTitle updatedTitle = existingMember.MemberTitle;
            MemberDescription updatedDescription = existingMember.MemberDescription;
            MemberTag updatedTag = existingMember.MemberTag;

            // Apply patches
            if (establishmentMemberPatchRequest.EstablishmentId is not null)
            {
                Result<EstablishmentId> establishmentIdResult = IdHelper.ValidateAndCreateId<EstablishmentId>(establishmentMemberPatchRequest.EstablishmentId);
                if (!establishmentIdResult.IsSuccess)
                {
                    return Result<EstablishmentMember>.Failure(ErrorType.Validation, establishmentIdResult.Errors);
                }
                updatedEstablishmentId = establishmentIdResult.Value!;
            }

            if (establishmentMemberPatchRequest.EmployeeId is not null)
            {
                Result<EmployeeId> employeeIdResult = IdHelper.ValidateAndCreateId<EmployeeId>(establishmentMemberPatchRequest.EmployeeId);
                if (!employeeIdResult.IsSuccess)
                {
                    return Result<EstablishmentMember>.Failure(ErrorType.Validation, employeeIdResult.Errors);
                }
                updatedEmployeeId = employeeIdResult.Value!;
            }

            if (establishmentMemberPatchRequest.MemberTitle is not null)
            {
                Result<MemberTitle> titleResult = MemberTitle.Create(establishmentMemberPatchRequest.MemberTitle);
                if (!titleResult.IsSuccess)
                {
                    return Result<EstablishmentMember>.Failure(ErrorType.Validation, titleResult.Errors);
                }

                updatedTitle = titleResult.Value!;
            }

            if (establishmentMemberPatchRequest.MemberDescription is not null)
            {
                Result<MemberDescription> descriptionResult = MemberDescription.Create(establishmentMemberPatchRequest.MemberDescription);
                if (!descriptionResult.IsSuccess)
                {
                    return Result<EstablishmentMember>.Failure(ErrorType.Validation, descriptionResult.Errors);
                }

                updatedDescription = descriptionResult.Value!;
            }

            if (establishmentMemberPatchRequest.MemberTag is not null)
            {
                Result<MemberTag> tagResult = MemberTag.Create(establishmentMemberPatchRequest.MemberTag);
                if (!tagResult.IsSuccess)
                {
                    return Result<EstablishmentMember>.Failure(ErrorType.Validation, tagResult.Errors);
                }

                updatedTag = tagResult.Value!;
            }

            // Validate employee's affiliation with the establishment
            Result<Employee> employeeResult = await _employeeRepository.GetByIdAsync(updatedEmployeeId);
            if (!employeeResult.IsSuccess)
            {
                return Result<EstablishmentMember>.Failure(
                    ErrorType.NotFound,
                    new Error("NotFound", $"Employee with ID '{updatedEmployeeId}' not found."));
            }

            if (employeeResult.Value!.EstablishmentId != updatedEstablishmentId)
            {
                return Result<EstablishmentMember>.Failure(
                    ErrorType.Validation,
                    new Error("Validation", $"Employee with ID '{updatedEmployeeId}' does not belong to Establishment with ID '{updatedEstablishmentId}'."));
            }

            // Create the updated EstablishmentMember instance
            Result<EstablishmentMember> updatedMemberResult = EstablishmentMember.Create(
                updatedEstablishmentId,
                updatedEmployeeId,
                updatedDescription,
                updatedTag,
                updatedTitle);

            if (!updatedMemberResult.IsSuccess)
            {
                return updatedMemberResult;
            }

            EstablishmentMember updatedMember = updatedMemberResult.Value!;

            Result<EstablishmentMember> updateResult = await _establishmentMemberRepository.UpdateAsync(updatedMember);
            Result<EstablishmentMember> commitResult = await SafeCommitAsync(() => updateResult);

            if (commitResult.IsSuccess)
            {
                scope.Complete();
                return commitResult;
            }
            return commitResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching establishment member {EstablishmentMemberId}.", establishmentMemberId);
            return Result<EstablishmentMember>.Failure(ErrorType.Internal, [new Error("ServiceError", ex.Message)]);
        }
    }

    public async Task<Result> DeleteEstablishmentMemberAsync(EstablishmentMemberId establishmentMemberId)
    {
        using var scope = new TransactionScope(TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);
        try
        {
            Result<EstablishmentMember> existingMemberResult = await _establishmentMemberRepository
                .GetByIdAsync(establishmentMemberId);
            if (!existingMemberResult.IsSuccess)
            {
                return Result.Failure(ErrorType.NotFound,
                    new Error("NotFound", $"EstablishmentMember with ID '{establishmentMemberId}' not found."));
            }

            Result deleteResult = await _establishmentMemberRepository.DeleteAsync(establishmentMemberId);
            Result<bool> commitResult = await SafeCommitAsync(() => Result<bool>.Success(true));

            if (commitResult.IsSuccess)
            {
                scope.Complete();
                return Result.Success();
            }
            return Result.Failure(commitResult.ErrorType, commitResult.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting establishment member.");
            return Result.Failure(ErrorType.Internal, [new Error("ServiceError", ex.Message)]);
        }
    }

    public async Task<Result<IEnumerable<EstablishmentMember>>> ListAsync(ISpecification<EstablishmentMember> spec)
        => await _establishmentMemberRepository.ListAsync(spec);

    public async Task<Result<int>> CountAsync(ISpecification<EstablishmentMember> spec)
        => await _establishmentMemberRepository.CountAsync(spec);

    public async Task<Result<bool>> EstablishmentMemberWithEmployeeExistsAsync(
        EmployeeId employeeId,
        EstablishmentId establishmentId,
        EstablishmentMemberId? excludedMemberId = null)
    {
        Result<EstablishmentMemberId> idResult = excludedMemberId == null
            ? IdHelper.ValidateAndCreateId<EstablishmentMemberId>(Guid.NewGuid().ToString()) // Dummy ID for creation check
            : IdHelper.ValidateAndCreateId<EstablishmentMemberId>(excludedMemberId.Value.ToString());

        if (!idResult.IsSuccess)
        {
            return Result<bool>.Failure(
                idResult.ErrorType,
                idResult.Errors);
        }

        EstablishmentMemberId idToCheck = idResult.Value!;

        bool exists = await _establishmentMemberRepository.MemberExistsForEmployeeInEstablishment(
            idToCheck,
            employeeId,
            establishmentId);

        return Result<bool>.Success(exists);
    }
}
