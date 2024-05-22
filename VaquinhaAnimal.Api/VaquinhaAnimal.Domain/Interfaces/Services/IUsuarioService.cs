using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VaquinhaAnimal.Domain.DTOs;
using VaquinhaAnimal.Domain.Entities.Base;

namespace VaquinhaAnimal.Domain.Interfaces
{
    public interface IUsuarioService 
    {
        Task<List<UsuarioListDTO>> ObterListaUsuariosAsync();
        Task<UsuarioListDTO> GetUserEmailAsync(string email);
        Task<UsuarioListDTO> GetUserDocumentAsync(string document);
        Task<ApplicationUser> GetEmailById(Guid usuario_id);
        Task<UsuarioListDTO> ObterUserPeloCustomerIdAsync(string customerId);
        Task<UsuarioListDTO> ObterUserPeloDocumentoIdAsync(string document);
    }
}
