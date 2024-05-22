using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using VaquinhaAnimal.Api.Controllers;
using VaquinhaAnimal.Api.ViewModels;
using VaquinhaAnimal.Domain.Interfaces;

namespace VaquinhaAnimal.App.V1.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/contatos")]
    public class ContatosController : MainController
    {
        public ContatosController(INotificador notificador, IUser user, IConfiguration configuration) : base(notificador, user, configuration) 
        { }

        [HttpPost]
        public ActionResult SendEmail(ContatoViewModel model)
        {
            try
            {
                MailMessage message = new MailMessage("contato@doadoresespeciais.com.br", "contato@doadoresespeciais.com.br");
                message.Subject = model.Subject;
                message.Body = "<h3>Formulário de Contato </h3><br/> Nome: " + model.Name + "<br/> E-mail: " + model.Email + "<br/> Mensagem: " + model.MessageBody;

                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.zoho.com";
                smtp.Port = 587;
                smtp.Credentials = new NetworkCredential("contato@doadoresespeciais.com.br", "Vasco10@");
                smtp.EnableSsl = true;

                message.IsBodyHtml = true;
                message.Priority = MailPriority.Normal;
                smtp.Send(message);

                return Ok(model);
            }

            catch (Exception ex)
            {
                return BadRequest("Erro: " + ex);
            }

        }

    }    
}