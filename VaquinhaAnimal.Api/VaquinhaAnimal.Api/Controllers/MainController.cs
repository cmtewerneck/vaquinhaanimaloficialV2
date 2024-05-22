using VaquinhaAnimal.Domain.Interfaces;
using VaquinhaAnimal.Domain.Notificacoes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace VaquinhaAnimal.Api.Controllers
{
    [ApiController]
    public abstract class MainController : ControllerBase
    {
        #region VARIABLES
        private readonly INotificador _notificador;
        public readonly IUser AppUser;
        private readonly IConfiguration _configuration;
        public HttpClient client = new HttpClient();
        public string urlPagarme = "https://api.pagar.me/core/v5/";
        protected Guid UsuarioId { get; set; }
        protected bool UsuarioAutenticado { get; set; }
        #endregion

        #region CONSTRUCTOR
        public MainController(INotificador notificador, IUser appUser, IConfiguration configuration)
        {
            _notificador = notificador;
            AppUser = appUser;
            _configuration = configuration;

            if (appUser.IsAuthenticated())
            {
                UsuarioId = appUser.GetUserId();
                UsuarioAutenticado = true;
            }
        }
        #endregion

        #region METHODS
        protected bool OperacaoValida()
        {
            return !_notificador.TemNotificacao();
        }

        protected ActionResult CustomResponse(object result = null)
        {
            if (OperacaoValida())
            {
                return Ok(new
                {
                    success = true,
                    data = result
                });
            }

            return BadRequest(new
            {
                success = false,
                errors = _notificador.ObterNotificacoes().Select(n => n.Mensagem)
            });
        }

        protected ActionResult CustomResponse(ModelStateDictionary modelState)
        {
            if (!modelState.IsValid) NotificarErroModelInvalida(modelState);
            return CustomResponse();
        }

        protected void NotificarErroModelInvalida(ModelStateDictionary modelState)
        {
            var erros = modelState.Values.SelectMany(e => e.Errors);
            foreach (var erro in erros)
            {
                var errorMsg = erro.Exception == null ? erro.ErrorMessage : erro.Exception.Message;
                NotificarErro(errorMsg);
            }
        }

        protected void NotificarErro(string mensagem)
        {
            _notificador.Handle(new Notificacao(mensagem));
        }

        protected void AddHeaderPagarme()
        {
            var userPagarme = _configuration["PagarMe:AppKey"];
            var password = "";
            var base64String = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userPagarme}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64String);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        #endregion
    }
}
