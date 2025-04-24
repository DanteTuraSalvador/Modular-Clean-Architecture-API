using System.Transactions;
using Microsoft.Extensions.Logging;
using TestNest.Admin.Application.Contracts.Common;
using TestNest.Admin.Application.Contracts.Interfaces.Persistence;
using TestNest.Admin.Application.Contracts.Interfaces.Service;
using TestNest.Admin.Application.Interfaces;
using TestNest.Admin.Application.Services.Base;
using TestNest.Admin.Application.Specifications.Common;
using TestNest.Admin.Domain.SocialMedias;
using TestNest.Admin.SharedLibrary.Common.Results;
using TestNest.Admin.SharedLibrary.Dtos.Requests.SocialMediaPlatform;
using TestNest.Admin.SharedLibrary.Exceptions;
using TestNest.Admin.SharedLibrary.Exceptions.Common;
using TestNest.Admin.SharedLibrary.StronglyTypeIds;
using TestNest.Admin.SharedLibrary.ValueObjects;

namespace TestNest.Admin.Application.Services;

public class SocialMediaPlatformService : BaseService, ISocialMediaPlatformService
{
    private readonly ISocialMediaPlatformRepository _socialMediaRepository;
    private readonly ILogger<SocialMediaPlatformService> _logger;

    public SocialMediaPlatformService(
        ISocialMediaPlatformRepository socialMediaRepository,
        IUnitOfWork unitOfWork,
        IDatabaseExceptionHandlerFactory exceptionHandlerFactory,
        ILogger<SocialMediaPlatformService> logger) : base(unitOfWork, logger, exceptionHandlerFactory)
    {
        _socialMediaRepository = socialMediaRepository;
        _logger = logger;
    }

    public async Task<Result<SocialMediaPlatform>> CreateSocialMediaPlatformAsync(
        SocialMediaPlatformForCreationRequest socialMediaPlatformForCreationRequest)
    {
        try
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                TransactionScopeAsyncFlowOption.Enabled))
            {
                var socialMediaNameResult = SocialMediaName
                    .Create(socialMediaPlatformForCreationRequest.Name,
                        socialMediaPlatformForCreationRequest.PlatformURL);

                if (!socialMediaNameResult.IsSuccess)
                {
                    return Result<SocialMediaPlatform>.Failure(
                        ErrorType.Validation,
                        socialMediaNameResult.Errors.ToArray());
                }

                var existingPlatformResult = await _socialMediaRepository
                    .GetSocialMediaPlatformByNameAsync(socialMediaNameResult.Value!.Name);

                if (existingPlatformResult.IsSuccess)
                {
                    var exception = SocialMediaPlatformException.DuplicateResource();
                    return Result<SocialMediaPlatform>.Failure(
                        ErrorType.Conflict,
                        new Error(exception.Code.ToString(), exception.Message.ToString()));
                }

                var socialMediaPlatformResult = SocialMediaPlatform
                    .Create(socialMediaNameResult.Value!);

                if (!socialMediaPlatformResult.IsSuccess)
                {
                    return Result<SocialMediaPlatform>.Failure(
                        ErrorType.Validation,
                        socialMediaPlatformResult.Errors.ToArray());
                }

                var socialMediaPlatform = socialMediaPlatformResult.Value!;
                await _socialMediaRepository.AddAsync(socialMediaPlatform);

                var commitResult = await SafeCommitAsync(
                    () => Result<SocialMediaPlatform>.Success(socialMediaPlatform));
                if (commitResult.IsSuccess)
                {
                    scope.Complete();
                    return commitResult;
                }
                return commitResult;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating social media platform.");
            return Result<SocialMediaPlatform>.Failure(ErrorType.Internal, new[] { new Error("ServiceError", ex.Message) });
        }
    }

    public async Task<Result<SocialMediaPlatform>> UpdateSocialMediaPlatformAsync(
        SocialMediaId socialMediaId,
        SocialMediaPlatformForUpdateRequest socialMediaPlatformForUpdateRequest)
    {
        try
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required,
               new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
               TransactionScopeAsyncFlowOption.Enabled))
            {
                var validatedSocialMediaPlatform = await _socialMediaRepository.GetByIdAsync(socialMediaId);
                if (!validatedSocialMediaPlatform.IsSuccess)
                {
                    return validatedSocialMediaPlatform;
                }

                var socialMediaPlatform = validatedSocialMediaPlatform.Value!;
                await _socialMediaRepository.DetachAsync(socialMediaPlatform);

                var socialMediaName = SocialMediaName.Create(
                    socialMediaPlatformForUpdateRequest.Name,
                    socialMediaPlatformForUpdateRequest.PlatformURL);

                if (!socialMediaName.IsSuccess)
                {
                    return Result<SocialMediaPlatform>.Failure(
                        ErrorType.Validation,
                        socialMediaName.Errors.ToArray());
                }

                var updatedSocialMediaPlatformResult = socialMediaPlatform
                    .WithSocialMediaName(socialMediaName.Value!);

                if (!updatedSocialMediaPlatformResult.IsSuccess)
                {
                    return updatedSocialMediaPlatformResult;
                }

                var updateResult = await _socialMediaRepository
                    .UpdateAsync(updatedSocialMediaPlatformResult.Value!);
                if (!updateResult.IsSuccess)
                {
                    return Result<SocialMediaPlatform>.Failure(
                        updateResult.ErrorType,
                        updateResult.Errors);
                }

                var commitResult = await SafeCommitAsync();
                if (commitResult.IsSuccess)
                {
                    scope.Complete();
                    return Result<SocialMediaPlatform>.Success(updatedSocialMediaPlatformResult.Value!);
                }
                return Result<SocialMediaPlatform>.Failure(commitResult.ErrorType, commitResult.Errors);
            }
        }
        catch (Exception ex)
        {
            return Result<SocialMediaPlatform>.Failure(ErrorType.Internal, new[] { new Error("ServiceError", ex.Message) });
        }
    }

    public async Task<Result> DeleteSocialMediaPlatformAsync(SocialMediaId socialMediaId)
    {
        try
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var result = await _socialMediaRepository.DeleteAsync(socialMediaId);
                if (!result.IsSuccess)
                {
                    return result;
                }

                var commitResult = await SafeCommitAsync();
                if (commitResult.IsSuccess)
                {
                    scope.Complete();
                    return Result.Success();
                }
                return Result.Failure(commitResult.ErrorType, commitResult.Errors);
            }
        }
        catch (Exception ex)
        {
            return Result.Failure(ErrorType.Internal, new[] { new Error("ServiceError", ex.Message) });
        }
    }

    public async Task<Result<SocialMediaPlatform>> GetSocialMediaPlatformByIdAsync(
        SocialMediaId socialMediaId)
    {
        try
        {
            var result = await _socialMediaRepository
                .GetByIdAsync(socialMediaId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving social media platform by ID.");
            return Result<SocialMediaPlatform>.Failure(ErrorType.Internal, new[] { new Error("ServiceError", ex.Message) });
        }
    }

    public async Task<Result<IEnumerable<SocialMediaPlatform>>> GetAllSocialMediaPlatformsAsync()
    {
        try
        {
            var repositoryResult = await _socialMediaRepository.GetAllAsync();
            return repositoryResult;
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<SocialMediaPlatform>>.Failure(ErrorType.Internal, new[] { new Error("ServiceError", ex.Message) });
        }
    }

    public async Task<Result<IEnumerable<SocialMediaPlatform>>> GetAllSocialMediaPlatformsAsync(ISpecification<SocialMediaPlatform> spec)
    {
        try
        {
            return await _socialMediaRepository.ListAsync(spec);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<SocialMediaPlatform>>.Failure(ErrorType.Internal, new[] { new Error("ServiceError", ex.Message) });
        }
    }

    public async Task<Result<int>> CountAsync(ISpecification<SocialMediaPlatform> spec)
    {
        try
        {
            return await _socialMediaRepository.CountAsync(spec);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure(ErrorType.Internal, new[] { new Error("ServiceError", ex.Message) });
        }
    }
}
