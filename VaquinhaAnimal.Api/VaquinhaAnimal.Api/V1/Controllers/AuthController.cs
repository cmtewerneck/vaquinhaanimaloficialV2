using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using VaquinhaAnimal.Api.Controllers;
using VaquinhaAnimal.Api.Extensions;
using VaquinhaAnimal.Api.ViewModels;
using VaquinhaAnimal.Domain.Entities.Base;
using VaquinhaAnimal.Domain.Entities.Pagarme;
using VaquinhaAnimal.Domain.Entities.Validations.Documents;
using VaquinhaAnimal.Domain.Interfaces;

namespace VaquinhaAnimal.Api.V1.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/")]
    public class AuthController : MainController
    {
        #region VARIABLES
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppSettings _appSettings;
        private readonly IConfiguration _configuration;
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly IUser _user;
        private readonly IIdentityRepository _identityRepository;
        #endregion

        #region CONSTRUCTOR
        public AuthController(INotificador notificador,
                              SignInManager<ApplicationUser> signInManager,
                              IIdentityRepository identityRepository,
                              UserManager<ApplicationUser> userManager,
                              IOptions<AppSettings> appSettings, IUser user,
                              ILogger<AuthController> logger,
                              IMapper mapper,
                              IConfiguration configuration,
                              IUsuarioService usuarioService) : base(notificador, user, configuration)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _appSettings = appSettings.Value;
            _logger = logger;
            _configuration = configuration;
            _mapper = mapper;
            _user = user;
            _usuarioService = usuarioService;
            _identityRepository = identityRepository;
        }

        #endregion

        #region CRUD
        [HttpPost("nova-conta")]
        public async Task<ActionResult> RegistrarComConfirmacao(RegisterUserViewModel registerUser)
        {
            // TESTA O RECAPTCHA
            var url = "https://www.google.com/recaptcha/api/siteverify";

            var formValues = new Dictionary<string, string>
            {
                ["secret"] = _configuration["GoogleRecaptcha:Key"],
                ["response"] = registerUser.Recaptcha,
                ["remoteip"] = ""
            };

            var formData = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(formValues)
            };

            var resultGoogle = await client.SendAsync(formData);
            string responseBody = await resultGoogle.Content.ReadAsStringAsync();
            JObject obj = JObject.Parse(responseBody);
            var sucesso = (bool)obj["success"];

            if (!sucesso)
            {
                NotificarErro("Erro no recaptcha.");
                return CustomResponse();
            }

            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var userSameEmail = await _usuarioService.GetUserEmailAsync(registerUser.Email);
            if (userSameEmail != null)
            {
                NotificarErro("Já existe um usuário com este e-mail");
                return CustomResponse();
            }

            var userSameDocument = await _usuarioService.GetUserDocumentAsync(registerUser.Document);
            if (userSameDocument != null)
            {
                NotificarErro("Já existe um usuário com este documento");
                return CustomResponse();
            }

            if (registerUser.Type == "individual")
            {
                if (registerUser.Document.Length != CpfValidacao.TamanhoCpf)
                {
                    NotificarErro("Quantidade de caracteres inválida.");
                    return CustomResponse();
                }

                var cpfValido = CpfValidacao.Validar(registerUser.Document);

                if (cpfValido == false)
                {
                    NotificarErro("CPF inválido.");
                    return CustomResponse();
                }
            }
            else if (registerUser.Type == "company")
            {
                if (registerUser.Document.Length != CnpjValidacao.TamanhoCnpj)
                {
                    NotificarErro("Quantidade de caracteres inválida.");
                    return CustomResponse();
                }

                var cnpjValido = CnpjValidacao.Validar(registerUser.Document);

                if (cnpjValido == false)
                {
                    NotificarErro("CNPJ inválido.");
                    return CustomResponse();
                }
            }

            var idPagarme = await AddClientPagarme(registerUser);

            if (String.IsNullOrWhiteSpace(idPagarme))
            {
                NotificarErro("Não foi possível realizar seu cadastro. Verifique se algo foi preenchido incorretamente.");
                return CustomResponse();
            }

            var user = new ApplicationUser
            {
                UserName = registerUser.Email,
                Email = registerUser.Email,
                Document = registerUser.Document,
                Type = registerUser.Type,
                EmailConfirmed = false,
                Name = registerUser.Name,
                Code = "",
                Foto = registerUser.Foto,
                Codigo_Pagarme = idPagarme
            };

            var result = await _userManager.CreateAsync(user, registerUser.Password);

            if (result.Succeeded)
            {
                // GERA TOKEN
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // CODIFICA O TOKEN
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                // ENVIA POR EMAIL
                SendEmailUserAddedConfirmationMail(user.Name, user.Email, encodedToken);

                return CustomResponse("Verifique sua caixa de e-mail para validar sua conta.");
            }
            foreach (var error in result.Errors)
            {
                NotificarErro(error.Description);
            }

            return CustomResponse(registerUser);
        }

        [HttpPost("entrar")]
        public async Task<ActionResult> Entrar(LoginUserViewModel loginUser)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var result = await _signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

            if (result.Succeeded)
            {
                var userLogado = await _userManager.FindByNameAsync(loginUser.Email);
                if (userLogado.EmailConfirmed == false)
                {
                    NotificarErro("Verifique seu e-mail para confirmar sua conta.");
                    await _signInManager.SignOutAsync();
                    return CustomResponse();
                }

                _logger.LogInformation("Usuário " + loginUser.Email + " logado com sucesso.");
                return CustomResponse(await GerarJwt(loginUser.Email));
            }
            if (result.IsLockedOut)
            {
                NotificarErro("Usuário temporariamente bloqueado por excesso de tentativas. Tente em alguns minutos.");
                return CustomResponse(loginUser);
            }

            NotificarErro("Usuário e/ou Senha inválidos.");
            return CustomResponse(loginUser);
        }

        [HttpPut("atualizar-dados")]
        public async Task<ActionResult> UpdateUser(ApplicationUser user)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var userToBeUpdated = await _userManager.FindByIdAsync(_user.GetUserId().ToString());
            
            var userSameEmail = await _usuarioService.GetUserEmailAsync(user.Email);
            if (userSameEmail != null && userToBeUpdated.Id != userSameEmail.Id)
            {
                NotificarErro("Já existe um usuário com este e-mail");
                return CustomResponse();
            }

            var userSameDocument = await _usuarioService.GetUserDocumentAsync(user.Document);
            if (userSameDocument != null && userToBeUpdated.Id != userSameDocument.Id)
            {
                NotificarErro("Já existe um usuário com este documento");
                return CustomResponse();
            }

            userToBeUpdated.Name = user.Name;
            userToBeUpdated.Email = user.Email;
            userToBeUpdated.Code = user.Code;

            var resultPagarme = await EditClientPagarme(user);

            if (resultPagarme)
            {
                await _userManager.UpdateAsync(userToBeUpdated);
            }
            else
            {
                NotificarErro("Houve um erro ao atualizar. Revise os dados e tente novamente.");
                return CustomResponse();
            }

            return CustomResponse();
        }

        [HttpPost("confirm-email/{username}/{token}")]
        public async Task<ActionResult> ConfirmEmail(string username, string token)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var usuario = await _userManager.FindByNameAsync(username);
            if (usuario == null)
            {
                NotificarErro("User not found.");
                return CustomResponse();
            }

            if (string.IsNullOrEmpty(token))
            {
                NotificarErro("Invalid token");
                return CustomResponse();
            }

            var tokenDecoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

            var result = await _userManager.ConfirmEmailAsync(usuario, tokenDecoded);
            if (!result.Succeeded)
            {
                NotificarErro("Error reseting your password. Contact our team.");
                return CustomResponse();
            }

            return CustomResponse(result);
        }

        [HttpPut("atualizar-senha")]
        public async Task<ActionResult> UpdateUserPassword(UserPasswordViewModel user)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var userToBeUpdated = await _userManager.FindByIdAsync(_user.GetUserId().ToString());

            if (user.CurrentPassword == user.NewPassword)
            {
                NotificarErro("Senha atual é igual a nova senha");
                return CustomResponse();
            }

            var result = await _userManager.ChangePasswordAsync(userToBeUpdated, user.CurrentPassword, user.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    NotificarErro(error.Description);
                }
            }            

            return CustomResponse(result);
        }

        [HttpPost("reset-password-token")]
        public async Task<ActionResult> ResetPasswordToken(ResetPasswordUserViewModel user)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var usuario = await _userManager.FindByNameAsync(user.Username);
            if (usuario == null)
            {
                NotificarErro("User not found.");
                return CustomResponse();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);

            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // ENVIAR TOKEN POR EMAIL
            SendEmailResetPasswordToken(code, user.Username);

            return CustomResponse(token);
        }

        [HttpPost("reset-password-user")]
        public async Task<ActionResult> ResetPasswordUser(ResetPasswordViewModel user)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var usuario = await _userManager.FindByNameAsync(user.Username);
            if (usuario == null)
            {
                NotificarErro("User not found.");
                return CustomResponse();
            }

            if (string.Compare(user.NewPassword, user.ConfirmPassword) != 0)
            {
                NotificarErro("Password and confirmation don't match.");
                return CustomResponse();
            }

            if (string.IsNullOrEmpty(user.Token))
            {
                NotificarErro("Invalid token");
                return CustomResponse();
            }

            var tokenDecoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(user.Token));

            var result = await _userManager.ResetPasswordAsync(usuario, tokenDecoded, user.NewPassword);
            if (!result.Succeeded)
            {
                NotificarErro("Error reseting your password. Contact our team.");
                return CustomResponse();
            }

            return CustomResponse(result);
        }

        [HttpPost("confirm-email")]
        public async Task<ActionResult> ConfirmEmail(ConfirmEmailViewModel user)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var usuario = await _userManager.FindByNameAsync(user.Username);
            if (usuario == null)
            {
                NotificarErro("User not found.");
                return CustomResponse();
            }

            var result = await _userManager.ConfirmEmailAsync(usuario, user.Token) ;
            if (!result.Succeeded)
            {
                NotificarErro("Error confirming your e-mail. Contact our team.");
                return CustomResponse();
            }

            return CustomResponse(result);
        }

        [HttpGet("getAllUsers")]
        public async Task<List<UsuarioListViewModel>> ObterUsuariosAsync()
        {
            var dtoList = await _usuarioService.ObterListaUsuariosAsync();
            var viewModelList = _mapper.Map<List<UsuarioListViewModel>>(dtoList);
            return viewModelList;
        }

        [HttpGet("por-id/{id}")]
        public async Task<ActionResult<ApplicationUser>> GetUserByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null) return BadRequest();

            return Ok(user);
        }
        #endregion

        #region METHODS
        private async Task<LoginResponseViewModel> GerarJwt(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            var claims = await _userManager.GetClaimsAsync(user);
            var userRoles = await _userManager.GetRolesAsync(user);

            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id));
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Nbf, ToUnixEpochDate(DateTime.UtcNow).ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(DateTime.UtcNow).ToString(), ClaimValueTypes.Integer64));
            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim("role", userRole));
            }

            var identityClaims = new ClaimsIdentity();
            identityClaims.AddClaims(claims);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _appSettings.Emissor,
                Audience = _appSettings.ValidoEm,
                Subject = identityClaims,
                Expires = DateTime.UtcNow.AddHours(_appSettings.ExpiracaoHoras),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            });

            var encodedToken = tokenHandler.WriteToken(token);

            var response = new LoginResponseViewModel
            {
                AccessToken = encodedToken,
                ExpiresIn = TimeSpan.FromHours(_appSettings.ExpiracaoHoras).TotalSeconds,
                UserToken = new UserTokenViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    Nome = user.Name,
                    Claims = claims.Select(c => new ClaimViewModel { Type = c.Type, Value = c.Value })
                }
            };

            return response;
        }

        private static long ToUnixEpochDate(DateTime date)
            => (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);

        private async Task<string> AddClientPagarme(RegisterUserViewModel registerUser)
        {
            try
            {
                AddHeaderPagarme();

                var clienteToAdd = new PagarmeCliente()
                {
                    Name = registerUser.Name,
                    Document = registerUser.Document,
                    Type = registerUser.Type,
                    Code = "Cliente_" + registerUser.Name.Split(" ", 10, StringSplitOptions.None)[0],
                    Email = registerUser.Email
                };

                HttpResponseMessage response = await client.PostAsJsonAsync(urlPagarme + "customers", clienteToAdd);
                string responseBody = await response.Content.ReadAsStringAsync();

                var clienteRecebido = JsonConvert.DeserializeObject<PagarmeCliente>(responseBody);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return clienteRecebido.Id;
                } 
                else
                {
                    return "";
                }
            }
            catch (HttpRequestException e)
            {
                return "Erro: " + e;
            }
        }

        private async Task<bool> EditClientPagarme(ApplicationUser applicationUser)
        {
            try
            {
                AddHeaderPagarme();

                var customerId = GetUserAndPagarmeId();

                var clienteToEdit = new PagarmeCliente()
                {
                    Name = applicationUser.Name,
                    Code = "Cliente_" + applicationUser.Name.Split(" ", 10, StringSplitOptions.None)[0],
                    Email = applicationUser.Email
                };

                HttpResponseMessage response = await client.PutAsJsonAsync(urlPagarme + "customers/" + customerId, clienteToEdit);
                string responseBody = await response.Content.ReadAsStringAsync();

                var clienteRecebido = JsonConvert.DeserializeObject<PagarmeCliente>(responseBody);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (HttpRequestException e)
            {
                return false;
            }
        }

        private string GetUserAndPagarmeId()
        {
            var usuarioId = _user.GetUserId();
            return _identityRepository.GetCodigoPagarme(usuarioId.ToString());
        }

        private void SendEmailUsuarioAdicionado(string name, string email)
        {
            try
            {
                MailMessage message = new MailMessage("contato@vaquinhaanimal.com.br", email);
                message.Subject = "Usuário Adicionado - Vaquinha Animal";
                message.Body = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'><html xmlns='http://www.w3.org/1999/xhtml' xmlns:o='urn:schemas-microsoft-com:office:office' style='font-family:arial, 'helvetica neue', helvetica, sans-serif'><head><meta charset='UTF-8'><meta content='width=device-width, initial-scale=1' name='viewport'><meta name='x-apple-disable-message-reformatting'><meta http-equiv='X-UA-Compatible' content='IE=edge'><meta content='telephone=no' name='format-detection'><title>New Email</title><!--[if (mso 16)]><style type='text/css'> a {text-decoration: none;} </style><![endif]--><!--[if gte mso 9]><style>sup { font-size: 100% !important; }</style><![endif]--><!--[if gte mso 9]><xml> <o:OfficeDocumentSettings> <o:AllowPNG></o:AllowPNG> <o:PixelsPerInch>96</o:PixelsPerInch> </o:OfficeDocumentSettings> </xml><![endif]--><style type='text/css'>#outlook a { padding:0;}.es-button { mso-style-priority:100!important; text-decoration:none!important;}a[x-apple-data-detectors] { color:inherit!important; text-decoration:none!important; font-size:inherit!important; font-family:inherit!important; font-weight:inherit!important; line-height:inherit!important;}.es-desk-hidden { display:none; float:left; overflow:hidden; width:0; max-height:0; line-height:0; mso-hide:all;}[data-ogsb] .es-button.es-button-1 { padding:10px 35px!important;}@media only screen and (max-width:600px) {p, ul li, ol li, a { line-height:150%!important } h1, h2, h3, h1 a, h2 a, h3 a { line-height:120% } h1 { font-size:30px!important; text-align:center } h2 { font-size:24px!important; text-align:left } h3 { font-size:20px!important; text-align:left } .es-header-body h1 a, .es-content-body h1 a, .es-footer-body h1 a { font-size:30px!important; text-align:center } .es-header-body h2 a, .es-content-body h2 a, .es-footer-body h2 a { font-size:24px!important; text-align:left } .es-header-body h3 a, .es-content-body h3 a, .es-footer-body h3 a { font-size:20px!important; text-align:left } .es-menu td a { font-size:14px!important } .es-header-body p, .es-header-body ul li, .es-header-body ol li, .es-header-body a { font-size:14px!important } .es-content-body p, .es-content-body ul li, .es-content-body ol li, .es-content-body a { font-size:14px!important } .es-footer-body p, .es-footer-body ul li, .es-footer-body ol li, .es-footer-body a { font-size:14px!important } .es-infoblock p, .es-infoblock ul li, .es-infoblock ol li, .es-infoblock a { font-size:12px!important } *[class='gmail-fix'] { display:none!important } .es-m-txt-c, .es-m-txt-c h1, .es-m-txt-c h2, .es-m-txt-c h3 { text-align:center!important } .es-m-txt-r, .es-m-txt-r h1, .es-m-txt-r h2, .es-m-txt-r h3 { text-align:right!important } .es-m-txt-l, .es-m-txt-l h1, .es-m-txt-l h2, .es-m-txt-l h3 { text-align:left!important } .es-m-txt-r img, .es-m-txt-c img, .es-m-txt-l img { display:inline!important } .es-button-border { display:inline-block!important } a.es-button, button.es-button { font-size:18px!important; display:inline-block!important } .es-adaptive table, .es-left, .es-right { width:100%!important } .es-content table, .es-header table, .es-footer table, .es-content, .es-footer, .es-header { width:100%!important; max-width:600px!important } .es-adapt-td { display:block!important; width:100%!important } .adapt-img { width:100%!important; height:auto!important } .es-m-p0 { padding:0!important } .es-m-p0r { padding-right:0!important } .es-m-p0l { padding-left:0!important } .es-m-p0t { padding-top:0!important } .es-m-p0b { padding-bottom:0!important } .es-m-p20b { padding-bottom:20px!important } .es-mobile-hidden, .es-hidden { display:none!important } tr.es-desk-hidden, td.es-desk-hidden, table.es-desk-hidden { width:auto!important; overflow:visible!important; float:none!important; max-height:inherit!important; line-height:inherit!important } tr.es-desk-hidden { display:table-row!important } table.es-desk-hidden { display:table!important } td.es-desk-menu-hidden { display:table-cell!important } .es-menu td { width:1%!important } table.es-table-not-adapt, .esd-block-html table { width:auto!important } table.es-social { display:inline-block!important } table.es-social td { display:inline-block!important } .es-desk-hidden { display:table-row!important; width:auto!important; overflow:visible!important; max-height:inherit!important } .es-m-p5 { padding:5px!important } .es-m-p5t { padding-top:5px!important } .es-m-p5b { padding-bottom:5px!important } .es-m-p5r { padding-right:5px!important } .es-m-p5l { padding-left:5px!important } .es-m-p10 { padding:10px!important } .es-m-p10t { padding-top:10px!important } .es-m-p10b { padding-bottom:10px!important } .es-m-p10r { padding-right:10px!important } .es-m-p10l { padding-left:10px!important } .es-m-p15 { padding:15px!important } .es-m-p15t { padding-top:15px!important } .es-m-p15b { padding-bottom:15px!important } .es-m-p15r { padding-right:15px!important } .es-m-p15l { padding-left:15px!important } .es-m-p20 { padding:20px!important } .es-m-p20t { padding-top:20px!important } .es-m-p20r { padding-right:20px!important } .es-m-p20l { padding-left:20px!important } .es-m-p25 { padding:25px!important } .es-m-p25t { padding-top:25px!important } .es-m-p25b { padding-bottom:25px!important } .es-m-p25r { padding-right:25px!important } .es-m-p25l { padding-left:25px!important } .es-m-p30 { padding:30px!important } .es-m-p30t { padding-top:30px!important } .es-m-p30b { padding-bottom:30px!important } .es-m-p30r { padding-right:30px!important } .es-m-p30l { padding-left:30px!important } .es-m-p35 { padding:35px!important } .es-m-p35t { padding-top:35px!important } .es-m-p35b { padding-bottom:35px!important } .es-m-p35r { padding-right:35px!important } .es-m-p35l { padding-left:35px!important } .es-m-p40 { padding:40px!important } .es-m-p40t { padding-top:40px!important } .es-m-p40b { padding-bottom:40px!important } .es-m-p40r { padding-right:40px!important } .es-m-p40l { padding-left:40px!important } }</style></head><body data-new-gr-c-s-loaded='14.1082.0' style='width:100%;font-family:arial, 'helvetica neue', helvetica, sans-serif;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;padding:0;Margin:0'><div class='es-wrapper-color' style='background-color:#ECEFF4'><!--[if gte mso 9]><v:background xmlns:v='urn:schemas-microsoft-com:vml' fill='t'> <v:fill type='tile' color='#eceff4'></v:fill> </v:background><![endif]--><table class='es-wrapper' width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;padding:0;Margin:0;width:100%;height:100%;background-repeat:repeat;background-position:center top;background-color:#ECEFF4'><tr><td valign='top' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' class='es-header' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-header-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:337px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:337px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0;padding-top:10px;padding-bottom:10px;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:12px'><img src='https://www.vaquinhaanimal.com.br/assets/img/logo_fundo_claro.png' alt='Logo' style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' height='30' title='Logo'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:203px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:203px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:600px'></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#ffffff;width:600px' bgcolor='#ffffff'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:40px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px;text-align:justify'>Ola, " + name + "<br><br>Seja muito bem vindo a Vaquinha Animal.</p></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px;text-align:justify'>Ao redor do Brasil, diversas pessoas precisam de ajuda para tratar e manter os cuidados necessários dos seus pets e não custa ajudarmos a dar uma qualidade de vida pra eles.</p></td></tr><tr><td align='center' style='padding:15px;Margin:0'><!--[if mso]><a href='https://www.vaquinhaanimal.com.br' target='_blank' hidden> <v:roundrect xmlns:v='urn:schemas-microsoft-com:vml' xmlns:w='urn:schemas-microsoft-com:office:word' esdevVmlButton href='https://www.vaquinhaanimal.com.br' style='height:41px; v-text-anchor:middle; width:271px' arcsize='50%' stroke='f' fillcolor='#fecd1c'> <w:anchorlock></w:anchorlock> <center style='color:#2e3440; font-family:arial, 'helvetica neue', helvetica, sans-serif; font-size:15px; font-weight:700; line-height:15px; mso-text-raise:1px'>Acessar a plataforma</center> </v:roundrect></a><![endif]--><!--[if !mso]><!-- --><span class='msohide es-button-border' style='border-style:solid;border-color:#4C566A;background:#FECD1C;border-width:0px;display:inline-block;border-radius:30px;width:auto;mso-hide:all'><a href='https://www.vaquinhaanimal.com.br' class='es-button es-button-1' target='_blank' style='mso-style-priority:100 !important;text-decoration:none;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;color:#2E3440;font-size:18px;padding:10px 35px;display:inline-block;background:#FECD1C;border-radius:30px;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-weight:bold;font-style:normal;line-height:22px;width:auto;text-align:center;mso-padding-alt:0;mso-border-alt:10px solid #FECD1C'>Acessar a plataforma</a></span><!--<![endif]--></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Precisa de alguma ajuda?</h1></td></tr></table></td></tr></table></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 class='p_name' style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>Telefone</h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='tel:+(000)123-456-789' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>+55 21 12345-6789</a></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='Margin:0;padding-bottom:15px;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone_1.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>E-mail<br></h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='mailto:contato@vaquinhaanimal.com.br' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>contato@vaquinhaanimal.com.br</a><br></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table class='es-footer' cellspacing='0' cellpadding='0' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' cellspacing='0' cellpadding='0' align='center' bgcolor='#ffffff' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-top:20px;padding-left:20px;padding-right:20px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:560px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Vaquinha Animal</h1></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='Margin:0;padding-top:20px;padding-bottom:20px;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:18px;color:#2E3440;font-size:12px'>© 2023 - Direitos Reservados - CNPJ: 48.173.612/0001-02 - Grupo Doadores Especiais</p></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table></td></tr></table></td></tr></table></td></tr></table></div></body></html>";

                MailAddress copy = new MailAddress("contato@vaquinhaanimal.com.br");
                message.CC.Add(copy);
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.zoho.com";
                smtp.Port = 587;
                smtp.Credentials = new NetworkCredential("contato@vaquinhaanimal.com.br", "Vasco10@!@");
                smtp.EnableSsl = true;

                message.IsBodyHtml = true;
                message.Priority = MailPriority.Normal;
                smtp.Send(message);
            }

            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }

        }

        private void SendEmailUserAddedConfirmationMail(string name, string email, string tokenConfirmacao)
        {
            try
            {
                MailMessage message = new MailMessage("contato@vaquinhaanimal.com.br", email);
                message.Subject = "Confirme seu e-mail - Vaquinha Animal";
                message.Body = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'><html xmlns='http://www.w3.org/1999/xhtml' xmlns:o='urn:schemas-microsoft-com:office:office' style='font-family:arial, 'helvetica neue', helvetica, sans-serif'><head><meta charset='UTF-8'><meta content='width=device-width, initial-scale=1' name='viewport'><meta name='x-apple-disable-message-reformatting'><meta http-equiv='X-UA-Compatible' content='IE=edge'><meta content='telephone=no' name='format-detection'><title>New Email</title><!--[if (mso 16)]><style type='text/css'> a {text-decoration: none;} </style><![endif]--><!--[if gte mso 9]><style>sup { font-size: 100% !important; }</style><![endif]--><!--[if gte mso 9]><xml> <o:OfficeDocumentSettings> <o:AllowPNG></o:AllowPNG> <o:PixelsPerInch>96</o:PixelsPerInch> </o:OfficeDocumentSettings> </xml><![endif]--><style type='text/css'>#outlook a { padding:0;}.es-button { mso-style-priority:100!important; text-decoration:none!important;}a[x-apple-data-detectors] { color:inherit!important; text-decoration:none!important; font-size:inherit!important; font-family:inherit!important; font-weight:inherit!important; line-height:inherit!important;}.es-desk-hidden { display:none; float:left; overflow:hidden; width:0; max-height:0; line-height:0; mso-hide:all;}[data-ogsb] .es-button.es-button-1 { padding:10px 35px!important;}@media only screen and (max-width:600px) {p, ul li, ol li, a { line-height:150%!important } h1, h2, h3, h1 a, h2 a, h3 a { line-height:120% } h1 { font-size:30px!important; text-align:center } h2 { font-size:24px!important; text-align:left } h3 { font-size:20px!important; text-align:left } .es-header-body h1 a, .es-content-body h1 a, .es-footer-body h1 a { font-size:30px!important; text-align:center } .es-header-body h2 a, .es-content-body h2 a, .es-footer-body h2 a { font-size:24px!important; text-align:left } .es-header-body h3 a, .es-content-body h3 a, .es-footer-body h3 a { font-size:20px!important; text-align:left } .es-menu td a { font-size:14px!important } .es-header-body p, .es-header-body ul li, .es-header-body ol li, .es-header-body a { font-size:14px!important } .es-content-body p, .es-content-body ul li, .es-content-body ol li, .es-content-body a { font-size:14px!important } .es-footer-body p, .es-footer-body ul li, .es-footer-body ol li, .es-footer-body a { font-size:14px!important } .es-infoblock p, .es-infoblock ul li, .es-infoblock ol li, .es-infoblock a { font-size:12px!important } *[class='gmail-fix'] { display:none!important } .es-m-txt-c, .es-m-txt-c h1, .es-m-txt-c h2, .es-m-txt-c h3 { text-align:center!important } .es-m-txt-r, .es-m-txt-r h1, .es-m-txt-r h2, .es-m-txt-r h3 { text-align:right!important } .es-m-txt-l, .es-m-txt-l h1, .es-m-txt-l h2, .es-m-txt-l h3 { text-align:left!important } .es-m-txt-r img, .es-m-txt-c img, .es-m-txt-l img { display:inline!important } .es-button-border { display:inline-block!important } a.es-button, button.es-button { font-size:18px!important; display:inline-block!important } .es-adaptive table, .es-left, .es-right { width:100%!important } .es-content table, .es-header table, .es-footer table, .es-content, .es-footer, .es-header { width:100%!important; max-width:600px!important } .es-adapt-td { display:block!important; width:100%!important } .adapt-img { width:100%!important; height:auto!important } .es-m-p0 { padding:0!important } .es-m-p0r { padding-right:0!important } .es-m-p0l { padding-left:0!important } .es-m-p0t { padding-top:0!important } .es-m-p0b { padding-bottom:0!important } .es-m-p20b { padding-bottom:20px!important } .es-mobile-hidden, .es-hidden { display:none!important } tr.es-desk-hidden, td.es-desk-hidden, table.es-desk-hidden { width:auto!important; overflow:visible!important; float:none!important; max-height:inherit!important; line-height:inherit!important } tr.es-desk-hidden { display:table-row!important } table.es-desk-hidden { display:table!important } td.es-desk-menu-hidden { display:table-cell!important } .es-menu td { width:1%!important } table.es-table-not-adapt, .esd-block-html table { width:auto!important } table.es-social { display:inline-block!important } table.es-social td { display:inline-block!important } .es-desk-hidden { display:table-row!important; width:auto!important; overflow:visible!important; max-height:inherit!important } .es-m-p5 { padding:5px!important } .es-m-p5t { padding-top:5px!important } .es-m-p5b { padding-bottom:5px!important } .es-m-p5r { padding-right:5px!important } .es-m-p5l { padding-left:5px!important } .es-m-p10 { padding:10px!important } .es-m-p10t { padding-top:10px!important } .es-m-p10b { padding-bottom:10px!important } .es-m-p10r { padding-right:10px!important } .es-m-p10l { padding-left:10px!important } .es-m-p15 { padding:15px!important } .es-m-p15t { padding-top:15px!important } .es-m-p15b { padding-bottom:15px!important } .es-m-p15r { padding-right:15px!important } .es-m-p15l { padding-left:15px!important } .es-m-p20 { padding:20px!important } .es-m-p20t { padding-top:20px!important } .es-m-p20r { padding-right:20px!important } .es-m-p20l { padding-left:20px!important } .es-m-p25 { padding:25px!important } .es-m-p25t { padding-top:25px!important } .es-m-p25b { padding-bottom:25px!important } .es-m-p25r { padding-right:25px!important } .es-m-p25l { padding-left:25px!important } .es-m-p30 { padding:30px!important } .es-m-p30t { padding-top:30px!important } .es-m-p30b { padding-bottom:30px!important } .es-m-p30r { padding-right:30px!important } .es-m-p30l { padding-left:30px!important } .es-m-p35 { padding:35px!important } .es-m-p35t { padding-top:35px!important } .es-m-p35b { padding-bottom:35px!important } .es-m-p35r { padding-right:35px!important } .es-m-p35l { padding-left:35px!important } .es-m-p40 { padding:40px!important } .es-m-p40t { padding-top:40px!important } .es-m-p40b { padding-bottom:40px!important } .es-m-p40r { padding-right:40px!important } .es-m-p40l { padding-left:40px!important } }</style></head><body data-new-gr-c-s-loaded='14.1082.0' style='width:100%;font-family:arial, 'helvetica neue', helvetica, sans-serif;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;padding:0;Margin:0'><div class='es-wrapper-color' style='background-color:#ECEFF4'><!--[if gte mso 9]><v:background xmlns:v='urn:schemas-microsoft-com:vml' fill='t'> <v:fill type='tile' color='#eceff4'></v:fill> </v:background><![endif]--><table class='es-wrapper' width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;padding:0;Margin:0;width:100%;height:100%;background-repeat:repeat;background-position:center top;background-color:#ECEFF4'><tr><td valign='top' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' class='es-header' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-header-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:337px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:337px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0;padding-top:10px;padding-bottom:10px;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:12px'><img src='https://www.vaquinhaanimal.com.br/assets/img/logo_fundo_claro.png' alt='Logo' style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' height='30' title='Logo'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:203px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:203px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:600px'></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#ffffff;width:600px' bgcolor='#ffffff'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:40px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px;text-align:justify'>Ola, " + name + "<br><br>Seja muito bem vindo a Vaquinha Animal.</p></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px;text-align:justify'>Ao redor do Brasil, diversas pessoas precisam de ajuda para tratar e manter os cuidados necessários dos seus pets e não custa ajudarmos a dar uma qualidade de vida pra eles.</p></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px;text-align:justify'>Não se esqueça de confirmar seu e-mail. Clique no botão abaixo para ativar sua conta.</p></td></tr><tr><td align='center' style='padding:15px;Margin:0'><!--[if mso]><a href='https://www.vaquinhaanimal.com.br' target='_blank' hidden> <v:roundrect xmlns:v='urn:schemas-microsoft-com:vml' xmlns:w='urn:schemas-microsoft-com:office:word' esdevVmlButton href='https://www.vaquinhaanimal.com.br' style='height:41px; v-text-anchor:middle; width:271px' arcsize='50%' stroke='f' fillcolor='#fecd1c'> <w:anchorlock></w:anchorlock> <center style='color:#2e3440; font-family:arial, 'helvetica neue', helvetica, sans-serif; font-size:15px; font-weight:700; line-height:15px; mso-text-raise:1px'>Confirmar e-mail</center> </v:roundrect></a><![endif]--><!--[if !mso]><!-- --><span class='msohide es-button-border' style='border-style:solid;border-color:#4C566A;background:#FECD1C;border-width:0px;display:inline-block;border-radius:30px;width:auto;mso-hide:all'><a href='http://www.vaquinhaanimal.com.br/auth/email-confirmation/" + email+"/"+tokenConfirmacao + "' class='es-button es-button-1' target='_blank' style='mso-style-priority:100 !important;text-decoration:none;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;color:#2E3440;font-size:18px;padding:10px 35px;display:inline-block;background:#FECD1C;border-radius:30px;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-weight:bold;font-style:normal;line-height:22px;width:auto;text-align:center;mso-padding-alt:0;mso-border-alt:10px solid #FECD1C'>Confirmar e-mail</a></span><!--<![endif]--></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Precisa de alguma ajuda?</h1></td></tr></table></td></tr></table></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 class='p_name' style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>Telefone</h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='tel:+(000)123-456-789' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>+55 21 12345-6789</a></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='Margin:0;padding-bottom:15px;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone_1.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>E-mail<br></h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='mailto:contato@vaquinhaanimal.com.br' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>contato@vaquinhaanimal.com.br</a><br></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table class='es-footer' cellspacing='0' cellpadding='0' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' cellspacing='0' cellpadding='0' align='center' bgcolor='#ffffff' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-top:20px;padding-left:20px;padding-right:20px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:560px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Vaquinha Animal</h1></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='Margin:0;padding-top:20px;padding-bottom:20px;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:18px;color:#2E3440;font-size:12px'>© 2023 - Direitos Reservados - CNPJ: 48.173.612/0001-02 - Grupo Doadores Especiais</p></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table></td></tr></table></td></tr></table></td></tr></table></div></body></html>";

                MailAddress copy = new MailAddress("contato@vaquinhaanimal.com.br");
                message.CC.Add(copy);
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.zoho.com";
                smtp.Port = 587;
                smtp.Credentials = new NetworkCredential("contato@vaquinhaanimal.com.br", "Vasco10@!@");
                smtp.EnableSsl = true;

                message.IsBodyHtml = true;
                message.Priority = MailPriority.Normal;
                smtp.Send(message);
            }

            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }

        }

        private void SendEmailResetPasswordToken(string token, string userMail)
        {
            var url = "http://www.vaquinhaanimal.com.br/auth/reset-password-user/" + userMail + "/" + token;

            try
            {
                MailMessage message = new MailMessage("contato@vaquinhaanimal.com.br", userMail);
                message.Subject = "Recuperação de Senha - Vaquinha Animal";
                message.Body = "<div style='font-size: 12px; font-family: Verdana; background-color: #f8f8f8; margin-left: 20px;'>" +
                    "<img src='https://www.vaquinhaanimal.com.br/assets/img/logo_fundo_claro.png' style='width: 300px;'/><br/><br/>" +
                    "<h2>RECUPERAÇÃO DE SENHA</h2></br></br></h2><br/><br/> " +
                    "<p><a href="+url+">CLIQUE AQUI HTTPS</a></p>";

                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.zoho.com";
                smtp.Port = 587;
                smtp.Credentials = new NetworkCredential("contato@vaquinhaanimal.com.br", "Vasco10@!@");
                smtp.EnableSsl = true;

                message.IsBodyHtml = true;
                message.Priority = MailPriority.Normal;
                smtp.Send(message);
            }

            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }

        }

        #endregion
    }
}