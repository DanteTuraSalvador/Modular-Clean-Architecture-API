using TestNest.Admin.Application.Specifications.Common;
using TestNest.Admin.Domain.Establishments;
using TestNest.Admin.SharedLibrary.Common.Results;
using TestNest.Admin.SharedLibrary.Dtos.Requests.Establishment;
using TestNest.Admin.SharedLibrary.Dtos.Responses.Establishments;
using TestNest.Admin.SharedLibrary.StronglyTypeIds;

namespace TestNest.Admin.Application.Contracts.Interfaces.Service;


public interface IEstablishmentAddressService
{
    Task<Result<EstablishmentAddressResponse>> CreateEstablishmentAddressAsync(EstablishmentAddressForCreationRequest request); // Updated return type

    Task<Result<EstablishmentAddressResponse>> UpdateEstablishmentAddressAsync( // Updated return type
        EstablishmentAddressId establishmentAddressId,
        EstablishmentAddressForUpdateRequest establishmentAddressForUpdateRequest);

    Task<Result<EstablishmentAddressResponse>> PatchEstablishmentAddressAsync( // Updated return type
        EstablishmentAddressId establishmentAddressId,
        EstablishmentAddressPatchRequest establishmentAddressPatchRequest);

    Task<Result> DeleteEstablishmentAddressAsync(EstablishmentAddressId establishmentAddressId);

    Task<Result<EstablishmentAddressResponse>> GetEstablishmentAddressByIdAsync(EstablishmentAddressId establishmentAddressId);

    Task<Result<IEnumerable<EstablishmentAddressResponse>>> ListAsync(ISpecification<EstablishmentAddress> spec);

    Task<Result<int>> CountAsync(ISpecification<EstablishmentAddress> spec);
}
//public interface IEstablishmentAddressService
//{
//    Task<Result<EstablishmentAddress>> CreateEstablishmentAddressAsync(EstablishmentAddressForCreationRequest request);

//    Task<Result<EstablishmentAddress>> UpdateEstablishmentAddressAsync(
//        EstablishmentAddressId establishmentAddressId,
//        EstablishmentAddressForUpdateRequest establishmentAddressForUpdateRequest);

//    Task<Result<EstablishmentAddress>> PatchEstablishmentAddressAsync(
//        EstablishmentAddressId establishmentAddressId,
//        EstablishmentAddressPatchRequest establishmentAddressPatchRequest);

//    Task<Result> DeleteEstablishmentAddressAsync(EstablishmentAddressId establishmentAddressId);

//    Task<Result<EstablishmentAddress>> GetEstablishmentAddressByIdAsync(EstablishmentAddressId establishmentAddressId);

//    Task<Result<IEnumerable<EstablishmentAddress>>> ListAsync(ISpecification<EstablishmentAddress> spec);

//    Task<Result<int>> CountAsync(ISpecification<EstablishmentAddress> spec);
//}
