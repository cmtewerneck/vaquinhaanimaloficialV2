using VaquinhaAnimal.Domain.Interfaces;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Data.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using VaquinhaAnimal.Data.Repositories;
using System.Linq;
using System.Collections.Generic;
using VaquinhaAnimal.Domain.Helpers;

namespace VaquinhaAnimal.Data.Repository
{
    public class DoacaoRepository : Repository<Doacao>, IDoacaoRepository
    {
        public DoacaoRepository(VaquinhaDbContext context) : base(context) { }

        public async Task<Doacao> GetDonationsByOrderIdAsync(string orderId)
        {
            return await Db.Doacoes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Transacao_Id == orderId);
        }

        public async Task<Doacao> GetDonationsWithCampaignAsync(Guid doacaoId)
        {
            return await Db.Doacoes
                .AsNoTracking()
                .Include(c => c.Campanha)
                .FirstOrDefaultAsync(p => p.Id == doacaoId);
        }

        public async Task<List<Doacao>> GetAllMyDonationsAsync(Guid id)
        {
            return await Db.Doacoes
                .AsNoTracking()
                .Where(c => c.Usuario_Id == id.ToString())
                .Include(c => c.Campanha)
                .OrderByDescending(p => p.Data)
                .ToListAsync();
        }

        public async Task<List<Doacao>> ObterDoacoesDaCampanha(Guid campanhaId)
        {
            return await Db.Doacoes
                .AsNoTracking()
                .Include(c => c.Campanha)
                .Where(c => c.Campanha_Id == campanhaId && c.Status == "paid")
                .OrderByDescending(p => p.Data)
                .ToListAsync();
        }

        public async Task<Doacao> ObterDoacaoPelaCobranca(string charge_id)
        {
            return await Db.Doacoes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Charge_Id == charge_id);
        }

        public async Task<int> ObterTotalDoadoresPorCampanha(Guid campanhaId)
        {
            var result = await Db.Doacoes
                .AsNoTracking()
                .Where(x => x.Campanha_Id == campanhaId)
                .Where(x => x.Status == "paid")
                .CountAsync();
            
            return result;
        }

        // TESTE DE PAGINAÇÃO
        public async Task<PagedResult<Doacao>> ListAsync(int _PageSize, int _PageNumber)
        {
            var totalResults = Db.Doacoes
                .AsNoTracking()
                .Count();

            var result = await Db.Doacoes
                .AsNoTracking()
                //.Include(c => c.Campanha)
                .Skip((_PageNumber - 1) * _PageSize)
                .Take(_PageSize)
                .OrderBy(p => p.Data)
                .ToListAsync();

            var resultPaginado = new PagedResult<Doacao>
            {
                PageNumber = _PageNumber,
                PageSize = _PageSize,
                TotalRecords = totalResults,
                Data = result
            };

            return resultPaginado;
        }
        public async Task<PagedResult<Doacao>> ListMyDonationsAsync(int _PageSize, int _PageNumber, Guid userId)
        {
            var totalResults = Db.Doacoes
                .AsNoTracking()
                .Where(c => c.Usuario_Id == userId.ToString())
                .Count();

            var result = await Db.Doacoes
                .AsNoTracking()
                .Where(c => c.Usuario_Id == userId.ToString())
                .Include(c => c.Campanha)
                .OrderByDescending(p => p.Data)
                .Skip((_PageNumber - 1) * _PageSize)
                .Take(_PageSize)
                .ToListAsync();

            var resultPaginado = new PagedResult<Doacao>
            {
                PageNumber = _PageNumber,
                PageSize = _PageSize,
                TotalRecords = totalResults,
                Data = result
            };

            return resultPaginado;
        }
    }
}
