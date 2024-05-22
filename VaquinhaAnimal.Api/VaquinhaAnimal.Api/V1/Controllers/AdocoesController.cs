using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VaquinhaAnimal.Api.Controllers;
using VaquinhaAnimal.Api.ViewModels;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Interfaces;

namespace VaquinhaAnimal.App.V1.Controllers
{
    [Authorize]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/adocoes")]
    public class AdocoesController : MainController
    {
        #region VARIABLES
        private readonly IAdocaoRepository _adocaoRepository;
        private readonly IAdocaoService _adocaoService;
        private readonly IMapper _mapper;
        private readonly IUser _user;
        #endregion

        #region CONSTRUCTOR
        public AdocoesController(IAdocaoRepository adocaoRepository,
                                 IMapper mapper,
                                 IAdocaoService adocaoService,
                                 IConfiguration configuration,
                                 INotificador notificador, IUser user) : base(notificador, user, configuration)
        {
            _adocaoRepository = adocaoRepository;
            _adocaoService = adocaoService;
            _user = user;
            _mapper = mapper;
        }
        #endregion

        #region CRUD
        [HttpPost]
        public async Task<ActionResult<AdocaoCreateViewModel>> Adicionar(AdocaoCreateViewModel adocao)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var adocaoToAdd = _mapper.Map<Adocao>(adocao);

            if (adocao.Foto != null)
            {
                var arquivoNome = Guid.NewGuid() + "_" + adocao.Foto;

                if (!UploadArquivo(adocao.FotoUpload, arquivoNome))
                {
                    NotificarErro("Erro ao tentar carregar imagem. Verifique se enviou um formato válido.");
                    return CustomResponse();
                }

                adocaoToAdd.Foto = arquivoNome;
            }

            adocaoToAdd.UsuarioId = _user.GetUserId().ToString();

            adocaoToAdd.UrlAdocao = adocao.NomePet.Replace(" ", "-").ToLower();

            var result = await _adocaoService.Adicionar(adocaoToAdd);

            if (!result)
            {
                NotificarErro("Erro ao adicionar adoção");
                return CustomResponse(result);
            }

            return CustomResponse(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<AdocaoCreateViewModel>> Atualizar(Guid id, AdocaoCreateViewModel adocao)
        {
            if (id != adocao.Id)
            {
                NotificarErro("Os ids informados não são iguais!");
                return CustomResponse();
            }

            var adocaoAtualizacao = await ObterAdocao(id);

            //CONFIRMAR SE É O DONO DO PET
            if (_user.GetUserId().ToString() != adocaoAtualizacao.UsuarioId)
            {
                NotificarErro("Somente o dono do pet pode editá-lo.");
                return CustomResponse();
            }

            if (!ModelState.IsValid) return CustomResponse(ModelState);

            //ATUALIZANDO IMAGEM
            if (adocao.Foto != null)
            {
                var arquivoNome = Guid.NewGuid() + "_" + adocao.Foto;

                if (!UploadArquivo(adocao.FotoUpload, arquivoNome))
                {
                    NotificarErro("Erro ao atualizar imagem!");
                    return CustomResponse();
                }

                var imageToDelete = adocaoAtualizacao.Foto;
                adocaoAtualizacao.Foto = arquivoNome;

                if (imageToDelete != null)
                {
                    // REMOVENDO OUTRA POSSÍVEL CAPA
                    var exclusaoImagemDB = RemoverArquivo(imageToDelete);

                    if (!exclusaoImagemDB)
                    {
                        NotificarErro("Erro ao deletar arquivo da imagem da pasta");
                        return CustomResponse();
                    }
                }
            }

            adocaoAtualizacao.Abrigo_Nome = adocao.Abrigo_Nome;
            adocaoAtualizacao.Adotado = adocao.Adotado;
            adocaoAtualizacao.Castrado = adocao.Castrado;
            adocaoAtualizacao.Celular = adocao.Celular;
            adocaoAtualizacao.Email = adocao.Email;
            adocaoAtualizacao.Empresa_Nome = adocao.Empresa_Nome;
            adocaoAtualizacao.NomePet = adocao.NomePet;
            adocaoAtualizacao.TipoPet = adocao.TipoPet;
            adocaoAtualizacao.UsuarioId = adocao.UsuarioId;
            adocaoAtualizacao.Descricao = adocao.Descricao;
            adocaoAtualizacao.FaixaEtaria = adocao.FaixaEtaria;
            adocaoAtualizacao.LinkVideo = adocao.LinkVideo;
            adocaoAtualizacao.Particular_Nome = adocao.Particular_Nome;
            adocaoAtualizacao.TipoAnunciante = adocao.TipoAnunciante;
            adocaoAtualizacao.Instagram = adocao.Instagram;
            adocaoAtualizacao.Facebook = adocao.Facebook;

            var result = await _adocaoService.Atualizar(_mapper.Map<Adocao>(adocaoAtualizacao));

            return CustomResponse(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<CampanhaViewModel>> Excluir(Guid id)
        {
            var adocao = await ObterAdocao(id);

            //CONFIRMAR SE É O DONO DA CAMPANHA
            if (_user.GetUserId().ToString() != adocao.UsuarioId)
            {
                NotificarErro("Somente o criador da adoção pode excluí-la.");
                return CustomResponse(adocao);
            }

            if (adocao == null)
            {
                NotificarErro("O id da adoção não foi encontrado.");
                return CustomResponse(adocao);
            }

            if (adocao.Foto != null)
            {
                var exclusaoImagemDB = RemoverArquivo(adocao.Foto);

                if (!exclusaoImagemDB)
                {
                    NotificarErro("Erro ao deletar arquivo da imagem da pasta");
                    return CustomResponse();
                }
            }

            await _adocaoService.Remover(id);

            return CustomResponse(adocao);
        }

        [HttpPut("marcar-adotado/{id:guid}")]
        public async Task<ActionResult<AdocaoCreateViewModel>> MarcarAdotado(Guid id)
        {
            var adocaoAtualizacao = await ObterAdocao(id);

            //CONFIRMAR SE É O DONO DA CAMPANHA
            if (_user.GetUserId().ToString() != adocaoAtualizacao.UsuarioId)
            {
                NotificarErro("Somente o criador da adoção pode editá-la.");
                return CustomResponse();
            }

            if (!ModelState.IsValid) return CustomResponse(ModelState);

            adocaoAtualizacao.Adotado = true;

            var result = await _adocaoService.Atualizar(_mapper.Map<Adocao>(adocaoAtualizacao));

            return CustomResponse(result);
        }

        [HttpPut("marcar-listado/{id:guid}")]
        public async Task<ActionResult<AdocaoCreateViewModel>> MarcarListado(Guid id)
        {
            var adocaoAtualizacao = await ObterAdocao(id);

            //CONFIRMAR SE É O DONO DA CAMPANHA
            if (_user.GetUserId().ToString() != adocaoAtualizacao.UsuarioId)
            {
                NotificarErro("Somente o criador da adoção pode editá-la.");
                return CustomResponse();
            }

            if (!ModelState.IsValid) return CustomResponse(ModelState);

            adocaoAtualizacao.Adotado = false;

            var result = await _adocaoService.Atualizar(_mapper.Map<Adocao>(adocaoAtualizacao));

            return CustomResponse(result);
        }
        #endregion

        #region METHODS
        [AllowAnonymous]
        [HttpGet("todos-paginado/{PageSize:int}/{PageNumber:int}")]
        public async Task<ActionResult> ObterTodosPaginado(int PageSize, int PageNumber)
        {
            var result = await _adocaoRepository.ListAsync(PageSize, PageNumber);

            return Ok(result);
        }

        [HttpGet("minhas-adocoes-paginado/{PageSize:int}/{PageNumber:int}")]
        public async Task<ActionResult> ObterMinhasAdocoesPaginado(int PageSize, int PageNumber)
        {
            var userId = _user.GetUserId();

            var result = await _adocaoRepository.ListMyAdocoesAsync(PageSize, PageNumber, userId);

            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<ActionResult<AdocaoListViewModel>> ObterAdocaoPorId(Guid id)
        {
            var result = await _adocaoRepository.GetByIdAsync(id);

            var adocao = _mapper.Map<AdocaoListViewModel>(result);

            if (adocao == null) return NotFound();

            return adocao;
        }

        [HttpGet("obter_url/{url_adocao}")]
        [AllowAnonymous]
        public async Task<ActionResult<AdocaoListViewModel>> ObterAdocaoPorUrl(string url_adocao)
        {
            var result = await _adocaoRepository.GetByUrl(url_adocao);

            var adocao = _mapper.Map<AdocaoListViewModel>(result);

            if (adocao == null) return NotFound();

            return adocao;
        }

        private async Task<AdocaoCreateViewModel> ObterAdocao(Guid id)
        {
            var adocaoToReturn = await _adocaoRepository.GetByIdAsync(id);
            return _mapper.Map<AdocaoCreateViewModel>(adocaoToReturn);
        }

        private bool UploadArquivo(string arquivo, string imgNome)
        {
            List<string> validExtensions = new List<string> { ".jpg", ".jpeg", ".png" };

            var ext = Path.GetExtension(imgNome);

            if (!validExtensions.Contains(ext.ToLower()))
            {
                return false;
            }

            if (string.IsNullOrEmpty(arquivo))
            {
                return false;
            }

            var imageDataByteArray = Convert.FromBase64String(arquivo);

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", imgNome);

            if (System.IO.File.Exists(filePath))
            {
                return false;
            }

            System.IO.File.WriteAllBytes(filePath, imageDataByteArray);

            return true;
        }

        private bool RemoverArquivo(string image)
        {
            if (image == null)
            {
                return false;
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", image);

            if (!System.IO.File.Exists(filePath))
            {
                return false;
            }

            System.IO.File.Delete(filePath);

            return true;
        }
        #endregion
    }
}