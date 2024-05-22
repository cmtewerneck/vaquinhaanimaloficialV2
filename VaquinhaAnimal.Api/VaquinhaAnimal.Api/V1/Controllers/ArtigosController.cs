using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VaquinhaAnimal.Api.Controllers;
using VaquinhaAnimal.Api.ViewModels;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Entities.Base;
using VaquinhaAnimal.Domain.Interfaces;

namespace VaquinhaAnimal.App.V1.Controllers
{
    [Authorize]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/artigos")]
    public class ArtigosController : MainController
    {
        #region VARIABLES
        private readonly IArtigoRepository _artigoRepository;
        private readonly IArtigoService _artigoService;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUser _user;
        #endregion

        #region CONSTRUCTOR
        public ArtigosController(IArtigoRepository artigoRepository,
                                  IArtigoService artigoService,
                                  IMapper mapper,
                                  IConfiguration configuration,
                                  UserManager<ApplicationUser> userManager,
                                  INotificador notificador,
                                  IUser user) : base(notificador, user, configuration)
        {
            _artigoRepository = artigoRepository;
            _mapper = mapper;
            _artigoService = artigoService;
            _userManager = userManager;
            _user = user;
        }
        #endregion

        #region CRUD
        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 3145728)]
        [RequestSizeLimit(3145728)]
        public async Task<ActionResult<ArtigoViewModel>> Adicionar(ArtigoViewModel artigoViewModel)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var artigoToAdd = _mapper.Map<Artigo>(artigoViewModel);

            artigoToAdd.Titulo.ToUpper();

            if (artigoViewModel.FotoCapa != null)
            {
                var arquivoNome = Guid.NewGuid() + "_" + artigoViewModel.FotoCapa;

                if (!UploadArquivo(artigoViewModel.FotoCapaUpload, arquivoNome))
                {
                    NotificarErro("Erro ao tentar carregar imagem. Verifique se enviou um formato válido.");
                    return CustomResponse();
                }

                artigoToAdd.FotoCapa = arquivoNome;
            }

            artigoToAdd.UrlArtigo = artigoViewModel.Titulo.Replace(" ", "-").ToLower();

            var result = await _artigoService.Adicionar(artigoToAdd);

            if (!result)
            {
                NotificarErro("Erro ao adicionar artigo");
                return CustomResponse(result);
            }

            return CustomResponse(result);
        }

        [HttpPut("{id:guid}")]
        [RequestFormLimits(MultipartBodyLengthLimit = 3145728)]
        [RequestSizeLimit(3145728)]
        public async Task<ActionResult<ArtigoViewModel>> Atualizar(Guid id, ArtigoViewModel artigoViewModel)
        {
            if (id != artigoViewModel.Id)
            {
                NotificarErro("Os ids informados não são iguais!");
                return CustomResponse();
            }

            var artigoAtualizacao = await _artigoRepository.GetByIdAsync(id);

            var userLogado = await _userManager.FindByIdAsync(_user.GetUserId().ToString());

            //CONFIRMAR SE É O DONO DO ARTIGO
            if (userLogado.Email != "contato@vaquinhaanimal.com.br")
            {
                NotificarErro("Somente o administrador pode atualizar o artigo.");
                return CustomResponse();
            }

            if (!ModelState.IsValid) return CustomResponse(ModelState);

            if (artigoViewModel.FotoCapa != null)
            {
                var arquivoNome = Guid.NewGuid() + "_" + artigoViewModel.FotoCapa;

                if (!UploadArquivo(artigoViewModel.FotoCapaUpload, arquivoNome))
                {
                    NotificarErro("Erro ao tentar carregar imagem. Verifique se enviou um formato válido.");
                    return CustomResponse();
                }

                var imageToDelete = artigoAtualizacao.FotoCapa;
                artigoAtualizacao.FotoCapa = arquivoNome;

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

            artigoAtualizacao.EscritoPor = artigoViewModel.EscritoPor;
            artigoAtualizacao.Html = artigoViewModel.Html;
            artigoAtualizacao.Resumo = artigoViewModel.Resumo;
            artigoAtualizacao.Titulo = artigoViewModel.Titulo;
            artigoAtualizacao.UrlArtigo = artigoViewModel.UrlArtigo;

            var result = await _artigoService.Atualizar(_mapper.Map<Artigo>(artigoAtualizacao));

            return CustomResponse(result);
        }
        #endregion

        #region METHODS
        [AllowAnonymous]
        [HttpGet]
        public async Task<List<ArtigoViewModel>> ObterTodos()
        {
            var result = await _artigoRepository.GetAllAsync();
            return _mapper.Map<List<ArtigoViewModel>>(result);
        }

        private bool RemoverArquivo(string image)
        {
            if (string.IsNullOrWhiteSpace(image))
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

        [AllowAnonymous]
        [HttpGet("todos-paginado/{PageSize:int}/{PageNumber:int}")]
        public async Task<ActionResult> ObterTodosPaginado(int PageSize, int PageNumber)
        {
            var result = await _artigoRepository.ListAsync(PageSize, PageNumber);

            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<ActionResult<ArtigoViewModel>> ObterArtigoPorId(Guid id)
        {
            var result = await _artigoRepository.GetByIdAsync(id);

            var artigo = _mapper.Map<ArtigoViewModel>(result);

            if (artigo == null) return NotFound();

            return artigo;
        }

        [HttpGet("obter_url/{url_artigo}")]
        [AllowAnonymous]
        public async Task<ActionResult<ArtigoViewModel>> ObterArtigoPorUrl(string url_artigo)
        {
            var result = await _artigoRepository.GetByUrl(url_artigo);

            var artigo = _mapper.Map<ArtigoViewModel>(result);

            if (artigo == null) return NotFound();

            return artigo;
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
        #endregion
    }
}