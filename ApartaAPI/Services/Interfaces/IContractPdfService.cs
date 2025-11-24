using ApartaAPI.DTOs.Contracts;

namespace ApartaAPI.Services.Interfaces
{
    public interface IContractPdfService
    {
        byte[] GenerateContractPdf(ContractDto contract);
    }
}
