using AutoMapper;
using VaquinhaAnimal.Api.Controllers;
using VaquinhaAnimal.Api.ViewModels;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace VaquinhaAnimal.App.V1.Controllers
{
    [Authorize]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/imagens")]
    public class ImagensController : MainController
    {
        #region VARIABLES
        private readonly IImagemRepository _imagemRepository;
        private readonly IImagemService _imagemService;
        private readonly IMapper _mapper;
        #endregion

        #region CONSTRUCTOR
        public ImagensController(IImagemRepository imagemRepository,
                                 IImagemService imagemService,
                                 IMapper mapper,
                                 IConfiguration configuration,
                                 INotificador notificador, IUser user) : base(notificador, user, configuration)
        {
            _imagemRepository = imagemRepository;
            _mapper = mapper;
            _imagemService = imagemService;
        }
        #endregion

        #region CRUD
        [HttpPost]
        public async Task<ActionResult<ImagemViewModel>> Adicionar(ImagemViewModel imagemViewModel)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            await _imagemService.Adicionar(_mapper.Map<Imagem>(imagemViewModel));

            return CustomResponse(imagemViewModel);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ImagemViewModel>> Atualizar(Guid id, ImagemViewModel imagemViewModel)
        {
            if (id != imagemViewModel.id)
            {
                NotificarErro("Os ids informados não são iguais!");
                return CustomResponse();
            }

            var imagemAtualizacao = await ObterImagem(id);

            if (!ModelState.IsValid) return CustomResponse(ModelState);

            imagemAtualizacao.tipo = imagemViewModel.tipo;
            imagemAtualizacao.arquivo = imagemViewModel.arquivo;
            imagemAtualizacao.campanha_id = imagemViewModel.campanha_id;
            
            await _imagemService.Atualizar(_mapper.Map<Imagem>(imagemAtualizacao));

            return CustomResponse(imagemViewModel);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ImagemViewModel>> Excluir(Guid id)
        {
            var imagem = await ObterImagem(id);

            if (imagem == null)
            {
                NotificarErro("O id da imagem não foi encontrado.");
                return CustomResponse(imagem);
            }

            await _imagemService.Remover(id);

            return CustomResponse(imagem);
        }
        #endregion

        #region METHODS
        [HttpGet]
        public async Task<List<ImagemViewModel>> ObterTodos()
        {
            return _mapper.Map<List<ImagemViewModel>>(await _imagemRepository.GetAllAsync());
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ImagemViewModel>> ObterImagemPorId(Guid id)
        {
            var imagem = _mapper.Map<ImagemViewModel>(await _imagemRepository.GetByIdAsync(id));

            if (imagem == null) return NotFound();

            return imagem;
        }

        private async Task<ImagemViewModel> ObterImagem(Guid id)
        {
            return _mapper.Map<ImagemViewModel>(await _imagemRepository.GetByIdAsync(id));
        }
        #endregion
    }
}