using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VaquinhaAnimal.Domain.Interfaces;
using VaquinhaAnimal.Domain.Entities.Base;
using VaquinhaAnimal.Domain.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using System;

namespace VaquinhaAnimal.Domain.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsuarioService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<List<UsuarioListDTO>> ObterListaUsuariosAsync()
        {
            var query = _userManager.Users
               .OrderBy(x => x.Name)
               .Select(x => new UsuarioListDTO
               {
                   Id = x.Id,
                   Nome = x.Name
               });
            return await query.ToListAsync();
        }

        public async Task<UsuarioListDTO> ObterUserPeloCustomerIdAsync(string customerId)
        {
            var query = _userManager.Users
               .Where(x => x.Codigo_Pagarme == customerId)
               .Select(x => new UsuarioListDTO
               {
                   Id = x.Id,
                   Nome = x.Name
               });
            return await query.FirstAsync();
        }

        public async Task<UsuarioListDTO> ObterUserPeloDocumentoIdAsync(string document)
        {
            var query = _userManager.Users
               .Where(x => x.Document == document)
               .Select(x => new UsuarioListDTO
               {
                   Id = x.Id,
                   Nome = x.Name
               });
            return await query.FirstAsync();
        }

        public async Task<UsuarioListDTO> GetUserEmailAsync(string email)
        {
            var query = _userManager.Users
               .Where(x => x.Email == email)
               .Select(x => new UsuarioListDTO
               {
                   Id = x.Id,
                   Nome = x.Name
               });

            if (query.Count() >= 1)
            {
                return await query.FirstAsync();
            }
            else
            {
                return null;
            };

        }

        public async Task<UsuarioListDTO> GetUserDocumentAsync(string document)
        {
            var query = _userManager.Users
               .Where(x => x.Document == document)
               .Select(x => new UsuarioListDTO
               {
                   Id = x.Id,
                   Nome = x.Name
               });

            if (query.Count() >= 1)
            {
                return await query.FirstAsync();
            }
            else
            {
                return null;
            };

        }

        public async Task<ApplicationUser> GetEmailById(Guid usuario_id)
        {
            return await _userManager.Users
                .FirstOrDefaultAsync(x => x.Id == usuario_id.ToString());
        }

    }
}
