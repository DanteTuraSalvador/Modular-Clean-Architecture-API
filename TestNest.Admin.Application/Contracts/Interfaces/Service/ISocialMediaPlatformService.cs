//using TestNest.Admin.Application.Specifications.Common;
//using TestNest.Admin.Domain.SocialMedias;
//using TestNest.Admin.SharedLibrary.Common.Results;
//using TestNest.Admin.SharedLibrary.Dtos.Requests.SocialMediaPlatform;
//using TestNest.Admin.SharedLibrary.StronglyTypeIds;

//namespace TestNest.Admin.Application.Contracts.Interfaces.Service;

//public interface ISocialMediaPlatformService
//{
//    Task<Result<SocialMediaPlatform>> CreateSocialMediaPlatformAsync(
//        SocialMediaPlatformForCreationRequest socialMediaPlatformForCreationRequest);

//    Task<Result<SocialMediaPlatform>> UpdateSocialMediaPlatformAsync(
//        SocialMediaId socialMediaId,
//        SocialMediaPlatformForUpdateRequest socialMediaPlatformUpdateDto);

//    Task<Result> DeleteSocialMediaPlatformAsync(SocialMediaId socialMediaId);

//    Task<Result<SocialMediaPlatform>> GetSocialMediaPlatformByIdAsync(SocialMediaId socialMediaId);

//    Task<Result<IEnumerable<SocialMediaPlatform>>> GetAllSocialMediaPlatformsAsync(ISpecification<SocialMediaPlatform> spec);

//    Task<Result<int>> CountAsync(ISpecification<SocialMediaPlatform> spec);
//}


using TestNest.Admin.Application.Specifications.Common;
using TestNest.Admin.Domain.SocialMedias;
using TestNest.Admin.SharedLibrary.Common.Results;
using TestNest.Admin.SharedLibrary.Dtos.Requests.SocialMediaPlatform;
using TestNest.Admin.SharedLibrary.Dtos.Responses; // Make sure this is included
using TestNest.Admin.SharedLibrary.StronglyTypeIds;

namespace TestNest.Admin.Application.Contracts.Interfaces.Service;

public interface ISocialMediaPlatformService
{
    Task<Result<SocialMediaPlatform>> CreateSocialMediaPlatformAsync(
        SocialMediaPlatformForCreationRequest socialMediaPlatformForCreationRequest);

    Task<Result<SocialMediaPlatform>> UpdateSocialMediaPlatformAsync(
        SocialMediaId socialMediaId,
        SocialMediaPlatformForUpdateRequest socialMediaPlatformUpdateDto);

    Task<Result> DeleteSocialMediaPlatformAsync(SocialMediaId socialMediaId);

    Task<Result<SocialMediaPlatformResponse>> GetSocialMediaPlatformByIdAsync(SocialMediaId socialMediaId); // Updated return type

    Task<Result<IEnumerable<SocialMediaPlatformResponse>>> GetAllSocialMediaPlatformsAsync(ISpecification<SocialMediaPlatform> spec); // Updated return type

    Task<Result<int>> CountAsync(ISpecification<SocialMediaPlatform> spec);
}
