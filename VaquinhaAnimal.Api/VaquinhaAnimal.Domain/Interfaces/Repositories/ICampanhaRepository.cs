using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VaquinhaAnimal.Domain.Interfaces
{
    public interface ICampanhaRepository : IRepository<Campanha>
    {
        Task<Campanha> GetByIdWithImagesAsync(Guid id);
        Task<Campanha> GetByUrlWithImagesAsync(string url_campanha);
        Task<Campanha> GetByIdWithImagesAndDonationsAsync(Guid id);
        Task<List<Campanha>> GetAllMyCampaignsAsync(Guid usuario_id);
        Task<List<Campanha>> GetAllCampaignsAndImagesAsync(string email);
        Task<List<Campanha>> GetThreeCampaignsAndImagesAsync();
        Task<List<Campanha>> GetThreePremiumCampaignsAndImagesAsync();
        
        // TESTE DE PAGINAÇÃO
        Task<PagedResult<Campanha>> ListAsync(int _PageSize, int _PageNumber);
        Task<PagedResult<Campanha>> ListMyCampaignsAsync(int _PageSize, int _PageNumber, Guid userId);
    }
}
