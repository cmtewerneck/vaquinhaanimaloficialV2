using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Threading.Tasks;
using VaquinhaAnimal.Api.Controllers;
using VaquinhaAnimal.Api.ViewModels;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Entities.Base;
using VaquinhaAnimal.Domain.Entities.Pagarme;
using VaquinhaAnimal.Domain.Entities.Validations.Documents;
using VaquinhaAnimal.Domain.Interfaces;

namespace VaquinhaAnimal.App.V1.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/transacoes")]
    public class PagarmeController : MainController
    {
        #region VARIABLES
        private readonly IUser _user;
        private readonly IIdentityRepository _identityRepository;
        private readonly IAssinaturaService _assinaturaService;
        private readonly IDoacaoService _doacaoService;
        private readonly ICartaoService _cartaoService;
        private readonly IUsuarioService _usuarioService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAssinaturaRepository _assinaturaRepository;
        private readonly IConfiguration _configuration;
        private readonly ICartaoRepository _cartaoRepository;
        private readonly IDoacaoRepository _doacaoRepository;
        private readonly ICampanhaService _campanhaService;
        private readonly ISignalR _signalR;
        private readonly ICampanhaRepository _campanhaRepository;
        #endregion

        #region CONSTRUCTOR
        public PagarmeController(INotificador notificador,
                                 IUser user,
                                 IDoacaoService doacaoService,
                                 IAssinaturaService assinaturaService,
                                 UserManager<ApplicationUser> userManager,
                                 ICartaoService cartaoService,
                                 ICartaoRepository cartaoRepository,
                                 IDoacaoRepository doacaoRepository,
                                 ICampanhaRepository campanhaRepository,
                                 IAssinaturaRepository assinaturaRepository,
                                 IUsuarioService usuarioService,
                                 ICampanhaService campanhaService,
                                 ISignalR signalR,
                                 IConfiguration configuration,
                                 IIdentityRepository identityRepository)
                                 : base(notificador, user, configuration)
        {
            _user = user;
            _usuarioService = usuarioService;
            _assinaturaService = assinaturaService;
            _identityRepository = identityRepository;
            _assinaturaRepository = assinaturaRepository;
            _doacaoService = doacaoService;
            _cartaoService = cartaoService;
            _userManager = userManager;
            _configuration = configuration;
            _cartaoRepository = cartaoRepository;
            _doacaoRepository = doacaoRepository;
            _campanhaService = campanhaService;
            _campanhaRepository = campanhaRepository;
            _signalR = signalR;
        }
        #endregion

        #region PAGARME ---> CARTÕES
        [HttpPost("add-card")]
        public async Task<ActionResult> AddCardPagarme(PagarmeCard card)
        {
            try
            {
                AddHeaderPagarme();
                var idPagarme = GetUserAndPagarmeId();

                HttpResponseMessage response = await client.PostAsJsonAsync(urlPagarme + "customers/" + idPagarme + "/cards", card);
                string teste = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(responseBody);

                // ADICIONANDO O CARTÃO COM O RESPONSE RECEBIDO
                var cartaoToAdd = new Cartao
                {
                    Card_Id = (string)obj["id"],
                    Exp_Month = (int)obj["exp_month"],
                    Exp_Year = (int)obj["exp_year"],
                    First_Six_Digits = (string)obj["first_six_digits"],
                    Last_Four_Digits = (string)obj["last_four_digits"],
                    Customer_Id = GetUserAndPagarmeId(),
                    Status = (string)obj["status"]
                };

                var adicionandoCartao = await _cartaoService.Adicionar(cartaoToAdd);

                return Ok(obj);
            }
            catch (HttpRequestException e)
            {
                return BadRequest("Erro: " + e);
            }
        }

        [HttpGet("list-user-card")]
        public async Task<object> ListUserCards()
        {
            var customerId = GetUserAndPagarmeId();

            var result = await _cartaoRepository.GetAllCardsAsync(customerId);

            return result;
        }

        [HttpGet("list-card")]
        public async Task<object> ListCardPagarme()
        {
            AddHeaderPagarme();
            var idPagarme = GetUserAndPagarmeId();

            HttpResponseMessage response = await client.GetAsync(urlPagarme + "customers/" + idPagarme + "/cards");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            var cartoes = JsonConvert.DeserializeObject<PagarmeResponse<PagarmeCardResponse>>(responseBody);

            return cartoes;
        }

        [HttpDelete("delete-card/{cardId}")]
        public async Task<ActionResult> DeleteCardPagarme(string cardId)
        {
            try
            {
                AddHeaderPagarme();
                var idPagarme = GetUserAndPagarmeId();

                HttpResponseMessage response = await client.DeleteAsync(urlPagarme + "customers/" + idPagarme + "/cards/" + cardId);
                string responseBody = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();

                return Ok(responseBody);
            }
            catch (HttpRequestException e)
            {
                return BadRequest("Erro: " + e);
            }
        }
        #endregion

        #region PAGARME ---> PEDIDOS
        [HttpPost("add-order/{campanhaId}")]
        public async Task<ActionResult> AddOrderPagarme(PagarmePedido order, Guid campanhaId)
        {
            // TESTA O RECAPTCHA
            var resultTestGoogle = await TesteGoogleRecaptcha(order.recaptcha);

            if (!resultTestGoogle)
            {
                NotificarErro("Erro na validação do Recaptcha");
                return CustomResponse();
            }

            if (order == null)
            {
                NotificarErro("Ordem nula");
                return CustomResponse();
            }

            var userLogado = await _userManager.FindByIdAsync(_user.GetUserId().ToString());

            if (userLogado != null)
            {
                order.customer.name = userLogado.Name;
                order.customer.email = userLogado.Email;
                order.customer.document = userLogado.Document;
                order.customer.type = userLogado.Type;
            } 
            
            if (order.customer.type == "individual")
            {
                if (order.customer.document.Length != CpfValidacao.TamanhoCpf)
                {
                    NotificarErro("Quantidade de caracteres inválida.");
                    return CustomResponse();
                }

                var cpfValido = CpfValidacao.Validar(order.customer.document);

                if (cpfValido == false)
                {
                    NotificarErro("CPF inválido.");
                    return CustomResponse();
                }
            } 
            else if (order.customer.type == "company")
            {
                if (order.customer.document.Length != CnpjValidacao.TamanhoCnpj)
                {
                    NotificarErro("Quantidade de caracteres inválida.");
                    return CustomResponse();
                }

                var cnpjValido = CnpjValidacao.Validar(order.customer.document);

                if (cnpjValido == false)
                {
                    NotificarErro("CNPJ inválido.");
                    return CustomResponse();
                }
            }

            if (order.items[0].amount < 10)
            {
                NotificarErro("Valor mínimo de doação: R$ 10,00");
                return CustomResponse();
            }

            if (order.valorPlataforma < 0)
            {
                NotificarErro("Valor da plataforma não pode ser negativo");
                return CustomResponse();
            }

            try
            {
                //Fazer cadastro do usuário primeiro, pro caso de dar erro não prosseguir
                var resultRegister = await RegistrarDoador(order.customer.name, order.customer.email, order.customer.document, order.customer.type);

                if (!resultRegister)
                {
                    return CustomResponse();
                };

                //Pega o usuário doador
                var usuarioDoador = await _userManager.FindByNameAsync(order.customer.email);

                //Pega a campanha que vai receber a doação
                var campanhaRecebedora = await _campanhaRepository.GetByIdWithImagesAsync(campanhaId);

                if (campanhaRecebedora == null)
                {
                    NotificarErro("Campanha não encontrada");
                    return CustomResponse();
                }

                AddHeaderPagarme();

                double valorDoacao;
                double valorPlataforma;
                double valorBeneficiario;

                if (campanhaRecebedora.Premium)
                {
                    valorDoacao = order.items[0].amount + order.valorPlataforma;
                    valorPlataforma = Math.Round((0.12 * (valorDoacao - order.valorPlataforma)) + 0.99 + order.valorPlataforma - (order.valorPlataforma * 0.0336), 2);
                    valorBeneficiario = valorDoacao - valorPlataforma;
                } 
                else
                {
                    valorDoacao = order.items[0].amount + order.valorPlataforma;
                    valorPlataforma = Math.Round((0.07 * (valorDoacao - order.valorPlataforma)) + 0.99 + order.valorPlataforma - (order.valorPlataforma * 0.0336), 2);
                    valorBeneficiario = valorDoacao - valorPlataforma;
                }

                order.items[0].amount = valorDoacao * 100;

                // CALCULAR TAXAS DO SPLIT
                order.items[0].code = campanhaRecebedora.Titulo;

                //Coloca um telefone só pra passar na Pagarme
                order.customer.phones = new PagarmePedidoPhones()
                {
                    mobile_phone = new PagarmePedidoMobilePhone()
                    {
                        country_code = "55",
                        area_code = "21",
                        number = "987987987",
                    }
                };

                order.payments[0].split = new List<PagarmePedidoSplit>();

                // SPLIT RECEBEDOR PLATAFORMA --> 3% + R$ 0,99
                order.payments[0].split.Add(new PagarmePedidoSplit
                {
                    //recipient_id = "rp_V0YW6MvI3I84xj59", // TESTE
                    recipient_id = "rp_JW8g38RmcPfqpPxj", // PRODUÇÃO
                    amount = Convert.ToInt32(valorPlataforma * 100),
                    type = "flat",
                    options = new PagarmeSplitOptions
                    {
                        charge_processing_fee = false,
                        charge_remainder_fee = true,
                        liable = false
                    }
                });

                // SPLIT RECEBEDOR BENEFICIÁRIO --> Restante - taxas
                order.payments[0].split.Add(new PagarmePedidoSplit
                {
                    recipient_id = campanhaRecebedora.Beneficiario.RecebedorId,
                    amount = Convert.ToInt32(valorBeneficiario * 100),
                    type = "flat",
                    options = new PagarmeSplitOptions
                    {
                        charge_processing_fee = true,
                        charge_remainder_fee = false,
                        liable = true
                    }
                });

                if (order.payments[0].credit_card.card_id != "" && order.payments[0].credit_card.card_id != null)
                {
                    order.payments[0].credit_card.card.exp_month = null;
                    order.payments[0].credit_card.card.exp_year = null;
                    order.payments[0].credit_card.card.holder_document = null;
                    order.payments[0].credit_card.card.holder_name = null;
                    order.payments[0].credit_card.card.number = null;
                }

                HttpResponseMessage response = await client.PostAsJsonAsync(urlPagarme + "orders/", order);
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(responseBody);

                if ((string)obj["status"] == "failed")
                {
                    NotificarErro("Erro na transação. Tente novamente ou mude a forma de pagamento!");
                    return CustomResponse();
                }

                // Verifica se houve erro de validação e insere no mensageiro
                if (obj["errors"] != null)
                {
                    foreach (var erro in obj["errors"])
                    {
                        VerificarMensagemErro(erro.First[0].ToString());
                    }

                    return CustomResponse();
                }

                var valorAntecipacao = Math.Round((valorPlataforma - (valorPlataforma * 0.0336)) * 0.0275 * 15 / 30, 2);

                // ADICIONANDO A DOAÇÃO COM O RESPONSE RECEBIDO
                var doacaoToAdd = new Doacao
                {
                    Data = (DateTime)obj["created_at"],
                    FormaPagamento = (string)obj.SelectToken("charges[0].payment_method"),
                    Status = (string)obj["status"],
                    Campanha_Id = campanhaId,
                    Transacao_Id = (string)obj["id"],
                    Usuario_Id = usuarioDoador.Id.ToString(),
                    Valor = ((decimal)obj["amount"]) / 100,
                    ValorDestinadoPlataforma = (decimal)order.valorPlataforma,
                    ValorPlataforma = (decimal)valorPlataforma - (decimal)valorAntecipacao,
                    ValorBeneficiario = (decimal)(valorBeneficiario - Math.Round(valorDoacao * 0.0336, 2)),
                    ValorTaxa = (decimal)Math.Round(valorDoacao * 0.0336, 2) + (decimal)valorAntecipacao,
                    Customer_Id = GetPagarmeIdNoSigned(usuarioDoador.Id.ToString()),
                    Charge_Id = (string)obj.SelectToken("charges[0].id"),
                    Url_Download = (string)obj.SelectToken("charges[0].last_transaction.pdf") // CASO DE BOLETO
                };

                var adicionandoDoacao = await _doacaoService.Adicionar(doacaoToAdd);

                // SE FALHAR O INSERT DE DOAÇÃO, RETORNA ERRO E DELETA A COBRANÇA NA PAGARME
                if (adicionandoDoacao == false)
                {
                    await DeleteCharge(doacaoToAdd.Charge_Id); // Deleta a cobrança feita NA PAGARME
                    NotificarErro("Sua doação não pôde ser adicionada.");
                    return CustomResponse();
                }

                if ((string)obj["status"] == "paid")
                {
                    var campanhaPraAlterar = await _campanhaRepository.GetByIdAsync(doacaoToAdd.Campanha_Id);
                    campanhaPraAlterar.TotalArrecadado += (doacaoToAdd.Valor - (decimal)order.valorPlataforma);

                    var alterandoCampanha = await _campanhaService.Atualizar(campanhaPraAlterar);

                    // SE DER ERRO PARA ATUALIZAR O SALDO DA CAMPANHA
                    if (alterandoCampanha == false)
                    {
                        await DeleteCharge(doacaoToAdd.Charge_Id); // Deleta a cobrança feita NA PAGARME
                        var resulDeleteDoacao = await _doacaoService.Remover(doacaoToAdd.Id); // Deleta a cobrança feita NO BANCO DE DADOS

                        NotificarErro("Erro ao alterar o total arrecadado pela campanha.");
                        return CustomResponse();
                    }
                }

                var userDonoCampanha = await _userManager.FindByIdAsync(campanhaRecebedora.Usuario_Id.ToString());

                // Enviando email para o doador
                SendEmailDoacaoRealizadaCartaoDoador(doacaoToAdd, usuarioDoador.Name, usuarioDoador.Email);

                // Enviando email para o dono da campanha
                SendEmailDoacaoRealizadaCartaoDonoCampanha(doacaoToAdd, userDonoCampanha.Email);

                return CustomResponse((string)obj["status"]);
            }
            catch (HttpRequestException e)
            {
                return BadRequest("Erro: " + e);
            }
        }

        [HttpPost("add-order-boleto/{campanhaId}")]
        public async Task<ActionResult> AddOrderPagarmeBoleto(PagarmePedidoBoleto order, Guid campanhaId)
        {
            // TESTA O RECAPTCHA
            var resultTestGoogle = await TesteGoogleRecaptcha(order.recaptcha);

            if (!resultTestGoogle)
            {
                NotificarErro("Erro na validação do Recaptcha");
                return CustomResponse();
            }

            if (order == null)
            {
                NotificarErro("Ordem nula");
                return CustomResponse();
            }

            var userLogado = await _userManager.FindByIdAsync(_user.GetUserId().ToString());

            if (userLogado != null)
            {
                order.customer.name = userLogado.Name;
                order.customer.email = userLogado.Email;
                order.customer.document = userLogado.Document;
                order.customer.type = userLogado.Type;
            }

            if (order.customer.type == "individual")
            {
                if (order.customer.document.Length != CpfValidacao.TamanhoCpf)
                {
                    NotificarErro("Quantidade de caracteres inválida.");
                    return CustomResponse();
                }

                var cpfValido = CpfValidacao.Validar(order.customer.document);

                if (cpfValido == false)
                {
                    NotificarErro("CPF inválido.");
                    return CustomResponse();
                }
            }
            else if (order.customer.type == "company")
            {
                if (order.customer.document.Length != CnpjValidacao.TamanhoCnpj)
                {
                    NotificarErro("Quantidade de caracteres inválida.");
                    return CustomResponse();
                }

                var cnpjValido = CnpjValidacao.Validar(order.customer.document);

                if (cnpjValido == false)
                {
                    NotificarErro("CNPJ inválido.");
                    return CustomResponse();
                }
            }

            if (order.items[0].amount < 10)
            {
                NotificarErro("Valor mínimo de doação: R$ 10,00");
                return CustomResponse();
            }

            if (order.valorPlataforma < 0)
            {
                NotificarErro("Valor da plataforma não pode ser negativo");
                return CustomResponse();
            }

            try
            {
                //Fazer cadastro do usuário primeiro, pro caso de dar erro não prosseguir
                var resultRegister = await RegistrarDoador(order.customer.name, order.customer.email, order.customer.document, order.customer.type);

                if (!resultRegister)
                {
                    return CustomResponse();
                };

                //Pega o usuário doador
                var usuarioDoador = await _userManager.FindByNameAsync(order.customer.email);

                //Pega a campanha que vai receber a doação
                var campanhaRecebedora = await _campanhaRepository.GetByIdWithImagesAsync(campanhaId);

                if (campanhaRecebedora == null)
                {
                    NotificarErro("Campanha não encontrada");
                    return CustomResponse();
                }

                AddHeaderPagarme();

                double valorDoacao;
                double valorPlataforma;
                double valorBeneficiario;

                if (campanhaRecebedora.Premium)
                {
                    valorDoacao = order.items[0].amount + order.valorPlataforma;
                    valorPlataforma = Math.Round((0.12 * (valorDoacao - order.valorPlataforma)) + order.valorPlataforma, 2);
                    valorBeneficiario = valorDoacao - valorPlataforma;
                }
                else
                {
                    valorDoacao = order.items[0].amount + order.valorPlataforma;
                    valorPlataforma = Math.Round((0.07 * (valorDoacao - order.valorPlataforma)) + order.valorPlataforma, 2);
                    valorBeneficiario = valorDoacao - valorPlataforma;
                }

                order.items[0].amount = valorDoacao * 100;

                // CALCULAR TAXAS DO SPLIT
                order.items[0].code = campanhaRecebedora.Titulo;

                //Coloca um telefone só pra passar na Pagarme
                order.customer.phones = new PagarmePedidoBoletoPhones()
                {
                    mobile_phone = new PagarmePedidoBoletoMobilePhone()
                    {
                        country_code = "55",
                        area_code = "21",
                        number = "987987987",
                    }
                };

                order.payments[0].split = new List<PagarmePedidoBoletoSplit>();

                // SPLIT RECEBEDOR PLATAFORMA --> 3%
                order.payments[0].split.Add(new PagarmePedidoBoletoSplit
                {
                    //recipient_id = "rp_V0YW6MvI3I84xj59", // TESTE
                    recipient_id = "rp_JW8g38RmcPfqpPxj", // PRODUÇÃO
                    amount = Convert.ToInt32(valorPlataforma * 100),
                    type = "flat",
                    options = new PagarmeSplitBoletoOptions
                    {
                        charge_processing_fee = false,
                        charge_remainder_fee = true,
                        liable = false
                    }
                });

                // SPLIT RECEBEDOR BENEFICIÁRIO --> Restante - taxa de 3,49
                order.payments[0].split.Add(new PagarmePedidoBoletoSplit
                {
                    recipient_id = campanhaRecebedora.Beneficiario.RecebedorId,
                    amount = Convert.ToInt32(valorBeneficiario * 100),
                    type = "flat",
                    options = new PagarmeSplitBoletoOptions
                    {
                        charge_processing_fee = true,
                        charge_remainder_fee = false,
                        liable = true
                    }
                });

                HttpResponseMessage response = await client.PostAsJsonAsync(urlPagarme + "orders/", order);
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(responseBody);

                if ((string)obj["status"] == "failed")
                {
                    NotificarErro("Erro na transação. Tente novamente ou mude a forma de pagamento!");
                    return CustomResponse();
                }

                // Verifica se houve erro de validação e insere no mensageiro
                if (obj["errors"] != null)
                {
                    foreach (var erro in obj["errors"])
                    {
                        VerificarMensagemErro(erro.First[0].ToString());
                    }

                    return CustomResponse();
                }

                // ADICIONANDO A DOAÇÃO COM O RESPONSE RECEBIDO
                var doacaoToAdd = new Doacao
                {
                    Data = (DateTime)obj["created_at"],
                    FormaPagamento = (string)obj.SelectToken("charges[0].payment_method"),
                    Status = (string)obj["status"],
                    Campanha_Id = campanhaId,
                    Transacao_Id = (string)obj["id"],
                    Usuario_Id = usuarioDoador.Id.ToString(),
                    Valor = ((decimal)obj["amount"]) / 100,
                    ValorDestinadoPlataforma = (decimal)order.valorPlataforma,
                    ValorPlataforma = (decimal)valorPlataforma,
                    ValorBeneficiario = (decimal)(valorBeneficiario - 3.49),
                    ValorTaxa = 3.49m,
                    Customer_Id = GetPagarmeIdNoSigned(usuarioDoador.Id.ToString()),
                    Url_Download = (string)obj.SelectToken("charges[0].last_transaction.pdf"),
                    Charge_Id = (string)obj.SelectToken("charges[0].id")
                };

                var adicionandoDoacao = await _doacaoService.Adicionar(doacaoToAdd);

                // SE FALHAR O INSERT DE DOAÇÃO, RETORNA ERRO E DELETA A COBRANÇA NA PAGARME
                if (adicionandoDoacao == false)
                {
                    await DeleteCharge(doacaoToAdd.Charge_Id); // Deleta a cobrança feita NA PAGARME
                    NotificarErro("Sua doação não pôde ser adicionada.");
                    return CustomResponse();
                }

                var userDonoCampanha = await _userManager.FindByIdAsync(campanhaRecebedora.Usuario_Id.ToString());

                // Enviando email para o doador
                SendEmailDoacaoRealizadaBoletoDoador(doacaoToAdd, usuarioDoador.Name, usuarioDoador.Email);

                // Enviando email para o dono da campanha
                SendEmailDoacaoRealizadaBoletoDonoCampanha(doacaoToAdd, userDonoCampanha.Email);

                return CustomResponse(obj);
            }
            catch (HttpRequestException e)
            {
                return BadRequest("Erro: " + e);
            }
        }

        [HttpPost("add-order-pix/{campanhaId}")]
        public async Task<ActionResult> AddOrderPagarmePix(PagarmePedidoPix order, Guid campanhaId)
        {
            // TESTA O RECAPTCHA
            var resultTestGoogle = await TesteGoogleRecaptcha(order.recaptcha);

            if (!resultTestGoogle)
            {
                NotificarErro("Erro na validação do Recaptcha");
                return CustomResponse();
            }

            if (order == null)
            {
                NotificarErro("Usuário nulo");
                return CustomResponse();
            }

            var userLogado = await _userManager.FindByIdAsync(_user.GetUserId().ToString());

            if (userLogado != null)
            {
                order.customer.name = userLogado.Name;
                order.customer.email = userLogado.Email;
                order.customer.document = userLogado.Document;
                order.customer.type = userLogado.Type;
            }

            if (order.customer.type == "individual")
            {
                if (order.customer.document.Length != CpfValidacao.TamanhoCpf)
                {
                    NotificarErro("Quantidade de caracteres inválida.");
                    return CustomResponse();
                }

                var cpfValido = CpfValidacao.Validar(order.customer.document);

                if (cpfValido == false)
                {
                    NotificarErro("CPF inválido.");
                    return CustomResponse();
                }
            }
            else if (order.customer.type == "company")
            {
                if (order.customer.document.Length != CnpjValidacao.TamanhoCnpj)
                {
                    NotificarErro("Quantidade de caracteres inválida.");
                    return CustomResponse();
                }

                var cnpjValido = CnpjValidacao.Validar(order.customer.document);

                if (cnpjValido == false)
                {
                    NotificarErro("CNPJ inválido.");
                    return CustomResponse();
                }
            }

            if (order.items[0].amount < 10)
            {
                NotificarErro("Valor mínimo de doação: R$ 10,00");
                return CustomResponse();
            }

            if (order.valorPlataforma < 0)
            {
                NotificarErro("Valor da plataforma não pode ser negativo");
                return CustomResponse();
            }

            try
            {
                //Fazer cadastro do usuário primeiro, pro caso de dar erro não prosseguir
                var resultRegister = await RegistrarDoador(order.customer.name, order.customer.email, order.customer.document, order.customer.type);

                if (!resultRegister)
                {
                    return CustomResponse();
                };

                //Pega o usuário doador
                var usuarioDoador = await _userManager.FindByNameAsync(order.customer.email);

                //Pega a campanha que vai receber a doação
                var campanhaRecebedora = await _campanhaRepository.GetByIdWithImagesAsync(campanhaId);

                if (campanhaRecebedora == null)
                {
                    NotificarErro("Campanha não encontrada");
                    return CustomResponse();
                }

                AddHeaderPagarme();

                double valorDoacao;
                double valorPlataforma;
                double valorBeneficiario;

                if (campanhaRecebedora.Premium)
                {
                    valorDoacao = order.items[0].amount + order.valorPlataforma;
                    valorPlataforma = Math.Round((0.12 * (valorDoacao - order.valorPlataforma)) + order.valorPlataforma - (order.valorPlataforma * 0.0119), 2);
                    valorBeneficiario = valorDoacao - valorPlataforma;
                }
                else
                {
                    valorDoacao = order.items[0].amount + order.valorPlataforma;
                    valorPlataforma = Math.Round((0.07 * (valorDoacao - order.valorPlataforma)) + order.valorPlataforma - (order.valorPlataforma * 0.0119), 2);
                    valorBeneficiario = valorDoacao - valorPlataforma;
                }

                order.items[0].amount = valorDoacao * 100;

                // CALCULAR TAXAS DO SPLIT
                order.items[0].code = campanhaRecebedora.Titulo;

                //Coloca um telefone só pra passar na Pagarme
                order.customer.phones = new PagarmePedidoPixPhones()
                {
                    mobile_phone = new PagarmePedidoPixMobilePhone()
                    {
                        country_code = "55",
                        area_code = "21",
                        number = "987987987",
                    }
                };

                order.payments[0].split = new List<PagarmePedidoPixSplit>();

                // SPLIT RECEBEDOR PLATAFORMA --> 3%
                order.payments[0].split.Add(new PagarmePedidoPixSplit
                {
                    //recipient_id = "rp_V0YW6MvI3I84xj59", // TESTE
                    recipient_id = "rp_JW8g38RmcPfqpPxj", // PRODUÇÃO
                    amount = Convert.ToInt32(valorPlataforma * 100),
                    type = "flat",
                    options = new PagarmeSplitPixOptions
                    {
                        charge_processing_fee = false,
                        charge_remainder_fee = true,
                        liable = false
                    }
                });

                // SPLIT RECEBEDOR BENEFICIÁRIO --> Restante - taxa de 1,19%
                order.payments[0].split.Add(new PagarmePedidoPixSplit
                {
                    //recipient_id = "re_cljqcedqg00sl019toqzo7fp2", // Qualquer pra teste
                    recipient_id = campanhaRecebedora.Beneficiario.RecebedorId,
                    amount = Convert.ToInt32(valorBeneficiario * 100),
                    type = "flat",
                    options = new PagarmeSplitPixOptions
                    {
                        charge_processing_fee = true,
                        charge_remainder_fee = false,
                        liable = true
                    }
                });

                HttpResponseMessage response = await client.PostAsJsonAsync(urlPagarme + "orders/", order);
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(responseBody);

                if ((string)obj["status"] == "failed")
                {
                    NotificarErro("Erro na transação. Tente novamente ou mude a forma de pagamento!");
                    return CustomResponse();
                }

                // Verifica se houve erro de validação e insere no mensageiro
                if (obj["errors"] != null)
                {
                    foreach (var erro in obj["errors"])
                    {
                        VerificarMensagemErro(erro.First[0].ToString());
                    }

                    return CustomResponse();
                }

                // ADICIONANDO A DOAÇÃO COM O RESPONSE RECEBIDO
                var doacaoToAdd = new Doacao
                {
                    Data = (DateTime)obj["created_at"],
                    FormaPagamento = (string)obj.SelectToken("charges[0].payment_method"),
                    Status = (string)obj["status"],
                    Campanha_Id = campanhaId,
                    Transacao_Id = (string)obj["id"],
                    Usuario_Id = usuarioDoador.Id.ToString(),
                    Valor = ((decimal)obj["amount"]) / 100,
                    ValorDestinadoPlataforma = (decimal)order.valorPlataforma,
                    ValorPlataforma = (decimal)valorPlataforma,
                    ValorBeneficiario = (decimal)(valorBeneficiario - Math.Round(valorDoacao * 0.0119, 2)),
                    ValorTaxa = (decimal)Math.Round(valorDoacao * 0.0119, 2),
                    Customer_Id = GetPagarmeIdNoSigned(usuarioDoador.Id.ToString()),
                    Url_Download = (string)obj.SelectToken("charges[0].last_transaction.pdf"),
                    Charge_Id = (string)obj.SelectToken("charges[0].id")
                };

                var adicionandoDoacao = await _doacaoService.Adicionar(doacaoToAdd);

                // SE FALHAR O INSERT DE DOAÇÃO, RETORNA ERRO E DELETA A COBRANÇA NA PAGARME
                if (adicionandoDoacao == false)
                {
                    await DeleteCharge(doacaoToAdd.Charge_Id); // Deleta a cobrança feita NA PAGARME
                    NotificarErro("Sua doação não pôde ser adicionada.");
                    return CustomResponse();
                }

                if ((string)obj["status"] == "paid")
                {
                    var campanhaPraAlterar = await _campanhaRepository.GetByIdAsync(doacaoToAdd.Campanha_Id);
                    campanhaPraAlterar.TotalArrecadado += (doacaoToAdd.Valor - (decimal)order.valorPlataforma);

                    var alterandoCampanha = await _campanhaService.Atualizar(campanhaPraAlterar);

                    if (alterandoCampanha == false)
                    {
                        await DeleteCharge(doacaoToAdd.Charge_Id); // Deleta a cobrança feita NA PAGARME
                        var resulDeleteDoacao = await _doacaoService.Remover(doacaoToAdd.Id); // Deleta a cobrança feita NO BANCO DE DADOS

                        NotificarErro("Erro ao alterar o total arrecadado pela campanha.");
                        return CustomResponse();
                    }
                }

                return CustomResponse(new { url = (string)obj.SelectToken("charges[0].last_transaction.qr_code_url"), copiaCola = (string)obj.SelectToken("charges[0].last_transaction.qr_code") });
            }
            catch (HttpRequestException e)
            {
                return BadRequest("Erro: " + e);
            }
        }

        [HttpPost("add-order-pix-rapido/{campanhaId}")]
        public async Task<ActionResult> AddOrderPagarmePixRapido(PagarmePedidoPixRapido order, Guid campanhaId)
        {
            // TESTA O RECAPTCHA
            var resultTestGoogle = await TesteGoogleRecaptcha(order.recaptcha);

            if (!resultTestGoogle)
            {
                NotificarErro("Erro na validação do Recaptcha");
                return CustomResponse();
            }

            if (order == null)
            {
                NotificarErro("Usuário nulo");
                return CustomResponse();
            }

            var userLogado = await _userManager.FindByIdAsync(_user.GetUserId().ToString());

            if (userLogado != null)
            {
                order.customer.name = userLogado.Name;
                order.customer.email = userLogado.Email;
                order.customer.document = userLogado.Document;
                order.customer.type = userLogado.Type;
            }

            if (order.customer.type == "individual")
            {
                if (order.customer.document.Length != CpfValidacao.TamanhoCpf)
                {
                    NotificarErro("Quantidade de caracteres inválida.");
                    return CustomResponse();
                }

                var cpfValido = CpfValidacao.Validar(order.customer.document);

                if (cpfValido == false)
                {
                    NotificarErro("CPF inválido.");
                    return CustomResponse();
                }
            }
            else if (order.customer.type == "company")
            {
                if (order.customer.document.Length != CnpjValidacao.TamanhoCnpj)
                {
                    NotificarErro("Quantidade de caracteres inválida.");
                    return CustomResponse();
                }

                var cnpjValido = CnpjValidacao.Validar(order.customer.document);

                if (cnpjValido == false)
                {
                    NotificarErro("CNPJ inválido.");
                    return CustomResponse();
                }
            }

            if (order.items[0].amount < 10)
            {
                NotificarErro("Valor mínimo de doação: R$ 10,00");
                return CustomResponse();
            }

            if (order.valorPlataforma < 0)
            {
                NotificarErro("Valor da plataforma não pode ser negativo");
                return CustomResponse();
            }

            try
            {
                //Fazer cadastro do usuário primeiro, pro caso de dar erro não prosseguir
                var resultRegister = await RegistrarDoador(order.customer.name, order.customer.email, order.customer.document, order.customer.type);

                if (!resultRegister)
                {
                    return CustomResponse();
                };

                //Pega o usuário doador
                var usuarioDoador = await _userManager.FindByNameAsync(order.customer.email);

                //Pega a campanha que vai receber a doação
                var campanhaRecebedora = await _campanhaRepository.GetByIdWithImagesAsync(campanhaId);

                if (campanhaRecebedora == null)
                {
                    NotificarErro("Campanha não encontrada");
                    return CustomResponse();
                }

                //Adiciona header da pagarme
                AddHeaderPagarme();

                double valorDoacao;
                double valorPlataforma;
                double valorBeneficiario;

                if (campanhaRecebedora.Premium)
                {
                    valorDoacao = order.items[0].amount + order.valorPlataforma;
                    valorPlataforma = Math.Round((0.12 * (valorDoacao - order.valorPlataforma)) + order.valorPlataforma - (order.valorPlataforma * 0.0119), 2);
                    valorBeneficiario = valorDoacao - valorPlataforma;
                }
                else
                {
                    valorDoacao = order.items[0].amount + order.valorPlataforma;
                    valorPlataforma = Math.Round((0.07 * (valorDoacao - order.valorPlataforma)) + order.valorPlataforma - (order.valorPlataforma * 0.0119), 2);
                    valorBeneficiario = valorDoacao - valorPlataforma;
                }

                //Calcula os valores da doacao, da plataforma e do beneficiario
                //var valorDoacao = order.items[0].amount + order.valorPlataforma;
                //var valorPlataforma = Math.Round((0.07 * (valorDoacao - order.valorPlataforma)) + order.valorPlataforma - (order.valorPlataforma * 0.0119), 2);
                //var valorBeneficiario = valorDoacao - valorPlataforma;

                //Multiplica por 100 o valor da model para enviar em centavos para a pagarme
                order.items[0].amount = valorDoacao * 100;

                //Coloca o titulo da campanha como código do item
                order.items[0].code = campanhaRecebedora.Titulo;

                //Coloca um telefone só pra passar na Pagarme
                order.customer.phones = new PagarmePedidoPixRapidoPhones()
                {
                    mobile_phone = new PagarmePedidoPixRapidoMobilePhone()
                    {
                        country_code = "55",
                        area_code = "21",
                        number = "987987987",
                    }
                };

                // CALCULAR TAXAS DO SPLIT
                order.payments[0].split = new List<PagarmePedidoPixRapidoSplit>();

                // SPLIT RECEBEDOR PLATAFORMA --> 3% + valor doado por fora
                order.payments[0].split.Add(new PagarmePedidoPixRapidoSplit
                {
                    //recipient_id = "rp_V0YW6MvI3I84xj59", // TESTE
                    recipient_id = "rp_JW8g38RmcPfqpPxj", // PRODUÇÃO
                    amount = Convert.ToInt32(valorPlataforma * 100),
                    type = "flat",
                    options = new PagarmeSplitPixRapidoOptions
                    {
                        charge_processing_fee = false,
                        charge_remainder_fee = true,
                        liable = false
                    }
                });

                // SPLIT RECEBEDOR BENEFICIÁRIO --> Restante - taxa de 1,19%
                order.payments[0].split.Add(new PagarmePedidoPixRapidoSplit
                {
                    //recipient_id = "re_cljqcedqg00sl019toqzo7fp2", // Qualquer pra teste
                    recipient_id = campanhaRecebedora.Beneficiario.RecebedorId,
                    amount = Convert.ToInt32(valorBeneficiario * 100),
                    type = "flat",
                    options = new PagarmeSplitPixRapidoOptions
                    {
                        charge_processing_fee = true,
                        charge_remainder_fee = false,
                        liable = true
                    }
                });

                //Realiza doação!!!
                HttpResponseMessage response = await client.PostAsJsonAsync(urlPagarme + "orders/", order);
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(responseBody);

                // Verifica se o pagamento consta como FAILED
                if ((string)obj["status"] == "failed")
                {
                    NotificarErro("Erro na transação. Tente novamente ou mude a forma de pagamento!");
                    return CustomResponse();
                }

                // Verifica se houve erro de validação e insere no mensageiro
                if (obj["errors"] != null)
                {
                    foreach (var erro in obj["errors"])
                    {
                        VerificarMensagemErro(erro.First[0].ToString());
                    }

                    return CustomResponse();
                }

                // ADICIONANDO A DOAÇÃO COM O RESPONSE RECEBIDO
                var doacaoToAdd = new Doacao
                {
                    Data = (DateTime)obj["created_at"],
                    FormaPagamento = (string)obj.SelectToken("charges[0].payment_method"),
                    Status = (string)obj["status"],
                    Campanha_Id = campanhaId,
                    Transacao_Id = (string)obj["id"],
                    Usuario_Id = usuarioDoador.Id.ToString(),
                    Valor = ((decimal)obj["amount"]) / 100,
                    ValorDestinadoPlataforma = (decimal)order.valorPlataforma,
                    ValorPlataforma = (decimal)valorPlataforma,
                    ValorBeneficiario = (decimal)(valorBeneficiario - Math.Round(valorDoacao * 0.0119, 2)),
                    ValorTaxa = (decimal)Math.Round(valorDoacao * 0.0119, 2),
                    Customer_Id = GetPagarmeIdNoSigned(usuarioDoador.Id.ToString()),
                    Url_Download = (string)obj.SelectToken("charges[0].last_transaction.pdf"),
                    Charge_Id = (string)obj.SelectToken("charges[0].id")
                };

                var adicionandoDoacao = await _doacaoService.Adicionar(doacaoToAdd);

                // SE FALHAR O INSERT DE DOAÇÃO, RETORNA ERRO E DELETA A COBRANÇA NA PAGARME
                if (adicionandoDoacao == false)
                {
                    await DeleteCharge(doacaoToAdd.Charge_Id); // Deleta a cobrança feita NA PAGARME
                    NotificarErro("Sua doação não pôde ser adicionada.");
                    return CustomResponse();
                }

                // Se o status da doação vier PAID, sucesso!!
                if ((string)obj["status"] == "paid")
                {
                    var campanhaPraAlterar = await _campanhaRepository.GetByIdAsync(doacaoToAdd.Campanha_Id);
                    campanhaPraAlterar.TotalArrecadado += (doacaoToAdd.Valor - (decimal)order.valorPlataforma);

                    var alterandoCampanha = await _campanhaService.Atualizar(campanhaPraAlterar);

                    if (alterandoCampanha == false)
                    {
                        await DeleteCharge(doacaoToAdd.Charge_Id); // Deleta a cobrança feita NA PAGARME
                        var resulDeleteDoacao = await _doacaoService.Remover(doacaoToAdd.Id); // Deleta a cobrança feita NO BANCO DE DADOS

                        NotificarErro("Erro ao alterar o total arrecadado pela campanha.");
                        return CustomResponse();
                    }
                }

                return CustomResponse(new { url = (string)obj.SelectToken("charges[0].last_transaction.qr_code_url"), copiaCola = (string)obj.SelectToken("charges[0].last_transaction.qr_code") });
            }
            catch (HttpRequestException e)
            {
                return BadRequest("Erro: " + e);
            }
        }

        [HttpPost("add-assinatura/{campanhaId}")]
        public async Task<ActionResult> AddAssinatura(AssinaturaCreateViewModel order, Guid campanhaId)
        {
            // TESTA O RECAPTCHA
            var resultTestGoogle = await TesteGoogleRecaptcha(order.recaptcha);

            if (!resultTestGoogle)
            {
                NotificarErro("Erro na validação do Recaptcha");
                return CustomResponse();
            }

            var assinaturaToCreate = new PagarmePedidoRecorrencia();

            if (order == null)
            {
                NotificarErro("Ordem nula");
                return CustomResponse();
            }

            var userLogado = await _userManager.FindByIdAsync(_user.GetUserId().ToString());

            // MONTANDO OBJETO
            assinaturaToCreate.payment_method = "credit_card";
            assinaturaToCreate.currency = "BRL";
            assinaturaToCreate.interval = "month";
            assinaturaToCreate.interval_count = 1;
            assinaturaToCreate.installments = 1;
            assinaturaToCreate.statement_descriptor = "Vaquinha";
            assinaturaToCreate.billing_type = "prepaid";
            assinaturaToCreate.customer = new PagarmeAssinaturaCustomer()
            {
                name = userLogado.Name,
                email = userLogado.Email,
                document = userLogado.Document,
                type = userLogado.Type
            };
            assinaturaToCreate.items = new List<RecorrenciaItem>();
            assinaturaToCreate.items.Add(new RecorrenciaItem()
            {
                description = "Vaquinha",
                quantity = 1,
                pricing_scheme = new RecorrenciaScheme()
                {
                    price = order.items[0].pricing_scheme.price * 100,
                    scheme_type = "unit"
                }
            });
            assinaturaToCreate.customer.phones = new PagarmeAssinaturaPhones()
            {
                mobile_phone = new PagarmeAssinaturaMobilePhone()
                {
                    country_code = "55",
                    area_code = "21",
                    number = "987987987"
                }
            };
            assinaturaToCreate.split = new PagarmeAssinaturaSplit() {enabled = true};
            assinaturaToCreate.split.rules = new List<PagarmeAssinaturaRulesSplit>();
            assinaturaToCreate.card = new PagarmeAssianturaCard()
            {
                cvv = order.card.cvv,
                exp_month = order.card.exp_month,
                exp_year = order.card.exp_year,
                holder_document = order.card.holder_document,
                holder_name = order.card.holder_name,
                number = order.card.number,
                billing_address = new BillingAddressAssiantura()
                {
                    city = order.card.billing_address.city,
                    country = order.card.billing_address.country,
                    line_1 = order.card.billing_address.line_1,
                    state = order.card.billing_address.state,
                    zip_code = order.card.billing_address.zip_code,
                }
            };
            if (assinaturaToCreate.card_id != "" && assinaturaToCreate.card_id != null)
            {
                assinaturaToCreate.card.exp_month = null;
                assinaturaToCreate.card.exp_year = null;
                assinaturaToCreate.card.holder_document = null;
                assinaturaToCreate.card.holder_name = null;
                assinaturaToCreate.card.number = null;
            }

            if (assinaturaToCreate.customer.type == "individual")
            {
                if (assinaturaToCreate.customer.document.Length != CpfValidacao.TamanhoCpf)
                {
                    NotificarErro("Quantidade de caracteres inválida.");
                    return CustomResponse();
                }

                var cpfValido = CpfValidacao.Validar(assinaturaToCreate.customer.document);

                if (cpfValido == false)
                {
                    NotificarErro("CPF inválido.");
                    return CustomResponse();
                }
            }
            else if (assinaturaToCreate.customer.type == "company")
            {
                if (assinaturaToCreate.customer.document.Length != CnpjValidacao.TamanhoCnpj)
                {
                    NotificarErro("Quantidade de caracteres inválida.");
                    return CustomResponse();
                }

                var cnpjValido = CnpjValidacao.Validar(assinaturaToCreate.customer.document);

                if (cnpjValido == false)
                {
                    NotificarErro("CNPJ inválido.");
                    return CustomResponse();
                }
            }

            if (order.items[0].pricing_scheme.price < 10)
            {
                NotificarErro("Valor mínimo de assinatura: R$ 10,00");
                return CustomResponse();
            }

            try
            {
                //Pega o usuário doador
                var usuarioDoador = await _userManager.FindByNameAsync(assinaturaToCreate.customer.email);

                //Pega a campanha que vai receber a doação
                var campanhaRecebedora = await _campanhaRepository.GetByIdWithImagesAsync(campanhaId);

                if (campanhaRecebedora == null)
                {
                    NotificarErro("Campanha não encontrada");
                    return CustomResponse();
                }

                AddHeaderPagarme();

                var valorDoacao = order.items[0].pricing_scheme.price;
                var valorPlataforma = Math.Round(0.07 * valorDoacao + 0.99, 2);
                var percentualPlataforma = Math.Round((valorPlataforma / valorDoacao) * 100, 0);
                var valorBeneficiario = valorDoacao - valorPlataforma;
                var percentualBeneficiario = 100 - percentualPlataforma;

                // SPLIT RECEBEDOR PLATAFORMA --> 3% + R$ 0,99
                assinaturaToCreate.split.rules.Add(new PagarmeAssinaturaRulesSplit
                {
                    //recipient_id = "rp_V0YW6MvI3I84xj59", // TESTE
                    recipient_id = "rp_JW8g38RmcPfqpPxj", // PRODUÇÃO
                    amount = (int)percentualPlataforma,
                    type = "percentage",
                    options = new PagarmeAssinaturaSplitOptions
                    {
                        charge_processing_fee = false,
                        charge_remainder_fee = true,
                        liable = false
                    }
                });

                // SPLIT RECEBEDOR BENEFICIÁRIO --> Restante - taxas
                assinaturaToCreate.split.rules.Add(new PagarmeAssinaturaRulesSplit
                {
                    recipient_id = campanhaRecebedora.Beneficiario.RecebedorId,
                    amount = (int)percentualBeneficiario,
                    type = "percentage",
                    options = new PagarmeAssinaturaSplitOptions
                    {
                        charge_processing_fee = true,
                        charge_remainder_fee = false,
                        liable = true
                    }
                });

                HttpResponseMessage response = await client.PostAsJsonAsync(urlPagarme + "subscriptions/", assinaturaToCreate);
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(responseBody);

                if ((string)obj["status"] == "failed")
                {
                    NotificarErro("Erro na transação. Tente novamente ou mude a forma de pagamento!");
                    return CustomResponse();
                }

                // Verifica se houve erro de validação e insere no mensageiro
                if (obj["errors"] != null)
                {
                    foreach (var erro in obj["errors"])
                    {
                        VerificarMensagemErro(erro.First[0].ToString());
                    }

                    return CustomResponse();
                }

                var assinaturaToAdd = new Assinatura {
                    CampanhaId = campanhaId,
                    SubscriptionId = (string)obj["id"]
                };

                var adicionandoAssinatura = await _assinaturaService.Adicionar(assinaturaToAdd);

                // SE FALHAR O INSERT DA ASSINATURA
                if (adicionandoAssinatura == false)
                {
                    NotificarErro("Sua assinatura não pôde ser adicionada.");
                    return CustomResponse();
                }

                var userDonoCampanha = await _userManager.FindByIdAsync(campanhaRecebedora.Usuario_Id.ToString());

                // Enviando email para o doador
                //SendEmailDoacaoRealizadaAssinaturaDoador(doacaoToAdd, usuarioDoador.Name, usuarioDoador.Email);

                // Enviando email para o dono da campanha
                //SendEmailDoacaoRealizadaAssinaturaDonoCampanha(doacaoToAdd, userDonoCampanha.Name);

                return CustomResponse((string)obj["status"]);
            }
            catch (HttpRequestException e)
            {
                return BadRequest("Erro: " + e);
            }
        }

        [HttpPost("add-recorrencia")]
        public async Task<ActionResult> AddRecorrenciaPagarme(PagarmePedidoRecorrencia recorrencia)
        {
            // TESTA O RECAPTCHA
            var resultTestGoogle = await TesteGoogleRecaptcha(recorrencia.recaptcha);

            if (!resultTestGoogle)
            {
                NotificarErro("Erro na validação do Recaptcha");
                return CustomResponse();
            }

            if (recorrencia == null)
            {
                NotificarErro("Objeto nulo");
                return CustomResponse();
            }

            var userLogado = await _userManager.FindByIdAsync(_user.GetUserId().ToString());

            if (userLogado != null)
            {
                recorrencia.customer.name = userLogado.Name;
                recorrencia.customer.email = userLogado.Email;
                recorrencia.customer.document = userLogado.Document;
                recorrencia.customer.type = userLogado.Type;
            }

            if (recorrencia.customer.type == "individual")
            {
                if (recorrencia.customer.document.Length != CpfValidacao.TamanhoCpf)
                {
                    NotificarErro("Quantidade de caracteres inválida.");
                    return CustomResponse();
                }

                var cpfValido = CpfValidacao.Validar(recorrencia.customer.document);

                if (cpfValido == false)
                {
                    NotificarErro("CPF inválido.");
                    return CustomResponse();
                }
            }
            else if (recorrencia.customer.type == "company")
            {
                if (recorrencia.customer.document.Length != CnpjValidacao.TamanhoCnpj)
                {
                    NotificarErro("Quantidade de caracteres inválida.");
                    return CustomResponse();
                }

                var cnpjValido = CnpjValidacao.Validar(recorrencia.customer.document);

                if (cnpjValido == false)
                {
                    NotificarErro("CNPJ inválido.");
                    return CustomResponse();
                }
            }

            if (recorrencia.items[0].pricing_scheme.price < 10)
            {
                NotificarErro("Valor mínimo de assinatura: R$ 10,00");
                return CustomResponse();
            }

            try
            {
                AddHeaderPagarme();

                // Criando JSON
                var recorrenciaToSend = new PagarmePedidoRecorrencia()
                {
                    payment_method = "credit_card",
                    currency = "BRL",
                    interval = "month",
                    interval_count = 1,
                    billing_type = "exact_day",
                    installments = 1,
                    card_id = recorrencia.card_id,
                    //customer_id = GetUserAndPagarmeId(),
                    items = new List<RecorrenciaItem>()
                    {
                        new RecorrenciaItem()
                        {
                            description = "Vaquinha Animal",
                            quantity = 1,
                            pricing_scheme = new RecorrenciaScheme()
                            {
                                //price = (recorrencia.value * 100)
                            }
                        }
                    }
                };

                HttpResponseMessage response = await client.PostAsJsonAsync(urlPagarme + "subscriptions/", recorrenciaToSend);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(responseBody);

                if ((string)obj["status"] == "failed")
                {
                    return BadRequest("Erro na transação. Tente novamente ou mude o cartão!");
                }

                if ((string)obj["status"] == "paid")
                {
                    return BadRequest("Sua doação não pôde ser adicionada.");
                }

                return Ok(obj);
            }
            catch (HttpRequestException e)
            {
                return BadRequest("Erro: " + e);
            }
        }

        [HttpDelete("delete-recorrencia/{assinaturaId}")]
        public async Task<ActionResult> DeleteRecorrenciaPagarme(string assinaturaId)
        {
            try
            {
                AddHeaderPagarme();

                HttpResponseMessage response = await client.DeleteAsync(urlPagarme + "subscriptions/" + assinaturaId);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(responseBody);

                return Ok(obj);
            }
            catch (HttpRequestException e)
            {
                return BadRequest("Erro: " + e);
            }
        }

        [HttpGet("list-recurrencies/{PageSize:int}/{PageNumber:int}")]
        public async Task<object> ListAssinaturasPagarme(int PageSize, int PageNumber)
        {
            AddHeaderPagarme();
            var idPagarme = GetUserAndPagarmeId();

            var query = new Dictionary<string, string>
            {
                ["customer_id"] = idPagarme,
                ["page"] = PageNumber.ToString(),
                ["size"] = PageSize.ToString()
            };

            HttpResponseMessage response = await client.GetAsync(QueryHelpers.AddQueryString(urlPagarme + "subscriptions", query));
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            var assianturas = JsonConvert.DeserializeObject<PagarmeResponse<PagarmeRecurrenciesResponse>>(responseBody);

            return assianturas;
        }
        #endregion

        #region PAGARME ---> WEBHOOKS
        [AllowAnonymous]
        [HttpPost("pedidos-webhook")]
        public async Task<ActionResult> PedidosWebhook([FromBody] WebhookPedidos request)
        {
            // Obter a doação
            var doacao = await _doacaoRepository.ObterDoacaoPelaCobranca(request.data.charges[0].id);

            if (doacao != null)
            {
                // Obter a campanha
                var campanha = await _campanhaRepository.GetByIdAsync(doacao.Campanha_Id);

                // Obter o dono da campanha
                var donoCampanha = await _userManager.FindByIdAsync(campanha.Usuario_Id.ToString());

                // Obter o doador
                var doador = await _userManager.FindByIdAsync(doacao.Usuario_Id.ToString());

                // Caso o request venha PAID
                if (request.data.status == "paid")
                {
                    // PENDING to PAID
                    if (doacao.Status == "pending")
                    {
                        // Atualizo o status da doação para PAID
                        doacao.Status = "paid";
                        await _doacaoService.Atualizar(doacao);

                        // Atualizo o valor da campanha, ADICIONO o valor
                        campanha.TotalArrecadado += (((request.data.amount) / 100) - doacao.ValorDestinadoPlataforma);
                        await _campanhaService.Atualizar(campanha);

                        if (request.data.charges[0].payment_method == "pix")
                        {
                            await _signalR.PixIsPaid(true);

                            // Enviando email para o doador
                            SendEmailDoacaoRealizadaPixDoador(doacao, doador.Name, doador.Email);

                            // Enviando email para o dono da campanha
                            SendEmailDoacaoRealizadaPixDonoCampanha(doacao, donoCampanha.Name);
                        }

                        return Ok();
                    }

                    // PROCESSING to PAID
                    if (doacao.Status == "processing")
                    {
                        // Atualizo o status da doação para PAID
                        doacao.Status = "paid";
                        await _doacaoService.Atualizar(doacao);

                        // Atualizo o valor da campanha, ADICIONO o valor e subtraio o valor que é destinado a plataforma
                        campanha.TotalArrecadado += (((request.data.amount) / 100) - doacao.ValorDestinadoPlataforma);
                        await _campanhaService.Atualizar(campanha);

                        if (request.data.charges[0].payment_method == "pix")
                        {
                            await _signalR.PixIsPaid(true);

                            // Enviando email para o doador
                            SendEmailDoacaoRealizadaPixDoador(doacao, doador.Name, doador.Email);

                            // Enviando email para o dono da campanha
                            SendEmailDoacaoRealizadaPixDonoCampanha(doacao, donoCampanha.Name);
                        }

                        return Ok();
                    }
                }

                // Caso o request venha FAILED
                if (request.data.status == "failed")
                {
                    // PENDING to FAILED
                    if (doacao.Status == "pending")
                    {
                        // Atualizo o status da doação para FAILED
                        doacao.Status = "failed";
                        await _doacaoService.Atualizar(doacao);

                        if (request.data.charges[0].payment_method == "pix")
                        {
                            await _signalR.PixIsPaid(false);
                        }

                        return Ok();
                    }

                    // PROCESSING to FAILED
                    if (doacao.Status == "processing")
                    {
                        // Atualizo o status da doação para FAILED
                        doacao.Status = "failed";
                        await _doacaoService.Atualizar(doacao);

                        if (request.data.charges[0].payment_method == "pix")
                        {
                            await _signalR.PixIsPaid(false);
                        }

                        return Ok();
                    }

                    // PAID to FAILED
                    if (doacao.Status == "paid")
                    {
                        // Atualizo o status da doação para PAID
                        doacao.Status = "failed";
                        await _doacaoService.Atualizar(doacao);

                        // Atualizo o valor da campanha, SUBTRAIO o valor
                        campanha.TotalArrecadado -= (((request.data.amount) / 100) + doacao.ValorDestinadoPlataforma);

                        await _campanhaService.Atualizar(campanha);

                        return Ok();
                    }
                }

                // Caso o request venha CANCELED
                if (request.data.status == "canceled")
                {
                    // PENDING to CANCELED
                    if (doacao.Status == "pending")
                    {
                        // Atualizo o status da doação para FAILED
                        doacao.Status = "canceled";
                        await _doacaoService.Atualizar(doacao);

                        return Ok();
                    }

                    // PROCESSING to CANCELED
                    if (doacao.Status == "processing")
                    {
                        // Atualizo o status da doação para FAILED
                        doacao.Status = "canceled";
                        await _doacaoService.Atualizar(doacao);

                        return Ok();
                    }

                    // PAID to CANCELED
                    if (doacao.Status == "paid")
                    {
                        // Atualizo o status da doação para PAID
                        doacao.Status = "canceled";
                        await _doacaoService.Atualizar(doacao);

                        // Atualizo o valor da campanha, SUBTRAIO o valor
                        campanha.TotalArrecadado -= (((request.data.amount) / 100) + doacao.ValorDestinadoPlataforma);
                        await _campanhaService.Atualizar(campanha);

                        return Ok();
                    }
                }
            }

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("assinaturas-webhook")]
        public async Task<ActionResult> AssinaturasWebhook(PagarmeResponseAssinatura request)
        {
            decimal valorDoacao = request.data.paid_amount / 100;
            decimal percentualBeneficiario = (decimal)request.data.last_transaction.split[0].amount / 100;
            decimal percentualPlataforma = (decimal)request.data.last_transaction.split[1].amount / 100;

            var valorBeneficiario = Math.Round(valorDoacao * percentualBeneficiario - (valorDoacao * 0.0449m), 2);
            var valorPlataforma = Math.Round(valorDoacao * percentualPlataforma, 2);
            var valorTaxa = Math.Round(valorDoacao * 0.0449m, 2);

            var userId = await _usuarioService.ObterUserPeloCustomerIdAsync(request.data.customer.id);

            if (request.data.invoice != null)
            {
                // Pega a assinatura e a campanha pelo subscriptionId
                var assinatura = await _assinaturaRepository.GetBySubscriptionAsync(request.data.invoice.subscriptionId);
                var campanha = await _campanhaRepository.GetByIdAsync(assinatura.CampanhaId);

                // ADICIONANDO A DOAÇÃO COM O RESPONSE RECEBIDO
                var doacaoToAdd = new Doacao
                {
                    Data = request.created_at,
                    FormaPagamento = request.data.payment_method,
                    Status = request.data.status,
                    Campanha_Id = campanha.Id,
                    Transacao_Id = request.data.last_transaction.id,
                    Usuario_Id = userId.Id,
                    Valor = valorDoacao,
                    ValorPlataforma = valorPlataforma,
                    ValorBeneficiario = valorBeneficiario,
                    ValorTaxa = valorTaxa,
                    Customer_Id = request.data.customer.id,
                    Charge_Id = request.data.id
                };

                var adicionandoDoacao = await _doacaoService.Adicionar(doacaoToAdd);

                // SE FALHAR O INSERT DE DOAÇÃO, RETORNA ERRO E DELETA A COBRANÇA NA PAGARME
                if (adicionandoDoacao == false)
                {
                    await DeleteCharge(doacaoToAdd.Charge_Id); // Deleta a cobrança feita NA PAGARME
                    NotificarErro("Sua doação não pôde ser adicionada.");
                    return CustomResponse();
                }

                // ALTERANDO VALOR DA CAMPANHA
                campanha.TotalArrecadado += doacaoToAdd.Valor;
                var alterandoCampanha = await _campanhaService.Atualizar(campanha);
                if (alterandoCampanha == false)
                {
                    await DeleteCharge(doacaoToAdd.Charge_Id); // Deleta a cobrança feita NA PAGARME
                    var resulDeleteDoacao = await _doacaoService.Remover(doacaoToAdd.Id); // Deleta a cobrança feita NO BANCO DE DADOS

                    NotificarErro("Erro ao alterar o total arrecadado pela campanha.");
                    return CustomResponse();
                }

                return Ok();
            }

            return Ok();
        }
        #endregion

        #region PAGARME ---> INTERNAL METHODS
        private string GetUserAndPagarmeId()
        {
            var usuarioId = _user.GetUserId();
            return _identityRepository.GetCodigoPagarme(usuarioId.ToString());
        }

        private async Task<bool> TesteGoogleRecaptcha(string codigo)
        {
            // TESTA O RECAPTCHA
            var url = "https://www.google.com/recaptcha/api/siteverify";

            var formValues = new Dictionary<string, string>
            {
                ["secret"] = _configuration["GoogleRecaptcha:Key"],
                ["response"] = codigo,
                ["remoteip"] = ""
            };

            var formData = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(formValues)
            };

            var resultGoogle = await client.SendAsync(formData);
            string responseBodyGoogle = await resultGoogle.Content.ReadAsStringAsync();
            JObject objGoogle = JObject.Parse(responseBodyGoogle);
            var sucesso = (bool)objGoogle["success"];

            if (!sucesso)
            {
                return false;
            }

            return true;
        }

        private string GetPagarmeIdNoSigned(string userId)
        {
            var codigoPagarme = _identityRepository.GetCodigoPagarme(userId);
            return codigoPagarme;
        }

        private async Task<ActionResult> DeleteCharge(string chargeId)
        {
            AddHeaderPagarme();

            HttpResponseMessage response = await client.DeleteAsync(urlPagarme + "charges/" + chargeId);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            JObject obj = JObject.Parse(responseBody);

            return Ok();
        }

        private async Task<ActionResult<string>> CallApi(string method, string urlCompleta, string objetoString = "")
        {
            object objJson = null;
            HttpResponseMessage response = null;

            if (objetoString != "" || objetoString != null)
            {
                objJson = JsonConvert.DeserializeObject(objetoString);
            }

            if (method == "post")
            {
                response = await client.PostAsJsonAsync(urlCompleta, objJson);
            }

            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            JObject obj = JObject.Parse(responseBody);
            var objToReturn = JsonConvert.SerializeObject(obj);

            return Ok(objToReturn);
        }
        #endregion

        #region ADICIONANDO USUARIO SEM CONTA
        private async Task<bool> RegistrarDoador(string nome, string email, string documentNumber, string type)
        {
            if (!ModelState.IsValid)
            {
                NotificarErro("Erro na model");
                return false;
            }

            var user = await _userManager.FindByNameAsync(email);
            if (user != null)
            {
                if (user.Document != documentNumber)
                {
                    NotificarErro("E-mail já cadastrado com documento diferente.");
                    return false;
                }

                return true;
            }

            RegisterUserViewModel clienteToAdd = new RegisterUserViewModel()
            {
                Name = nome,
                Email = email,
                Document = documentNumber,
                Type = type
            };

            var idPagarme = await AddClientPagarme(clienteToAdd);

            if (String.IsNullOrWhiteSpace(idPagarme))
            {
                NotificarErro("Erro ao adicionar cliente na operadora financeira. Entre em contato com o suporte.");
                return false;
            }

            var userToAdd = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Document = documentNumber,
                Type = type,
                EmailConfirmed = false,
                Name = nome,
                Code = "",
                Foto = "",
                Codigo_Pagarme = idPagarme
            };

            //Gerar password aleatório 
            var passwordGenerated = GerarSenhaAleatoria();
            var result = await _userManager.CreateAsync(userToAdd, passwordGenerated);

            //Criar método pra enviar email e inserir a senha do usuário 
            var resultEmailEnviado = SendEmailPagamentoPixRapidoRealizado(email, passwordGenerated);

            foreach (var error in result.Errors)
            {
                NotificarErro(error.Description);
                return false;
            }

            return true;
        }

        private void VerificarMensagemErro(string erro)
        {
            if (erro == "The area_code field is required.")
            {
                NotificarErro("Código de área do telefone necessário.");
            }
            else if (erro == "You may not use numbers or special characters.")
            {
                NotificarErro("Nome inválido.");
            }
            else if (erro == "The number field is not a valid card number")
            {
                NotificarErro("Número de cartão inválido.");
            }
            else if (erro == "Card expired.")
            {
                NotificarErro("Cartão expirado.");
            }
            else if (erro == "The field cvv must be a string with a minimum length of 3 and a maximum length of 4.")
            {
                NotificarErro("CVV precisa ter entre 3 e 4 caracteres.");
            }
            else if (erro == "The country_code field is required.")
            {
                NotificarErro("Código do país do telefone necessário.");
            }
            else if (erro == "The number field is required.")
            {
                NotificarErro("Número do telefone necessário.");
            }
            else if (erro == "The field country_code must be a string with a maximum length of 4.")
            {
                NotificarErro("Código do país deve conter no máximo 4 caracteres.");
            }
            else if (erro == "The name field is required.")
            {
                NotificarErro("Nome é um campo obrigatório.");
            }
            else if (erro == "The type field is required.")
            {
                NotificarErro("Tipo do documento é um campo obrigatório.");
            }
            else if (erro == "The email field is not a valid e-mail address.")
            {
                NotificarErro("O campo e-mail está com formato inválido.");
            }
            else if (erro == "The document field is not a valid number.")
            {
                NotificarErro("O campo documento está com formato inválido.");
            }
            else if (erro == "The field amount must be greater than or equal to 1")
            {
                NotificarErro("O campo valor precisa ser maior que zero.");
            }
            else
            {
                NotificarErro("Houve um erro no pagamento!");
            }
        }

        private async Task<string> AddClientPagarme(RegisterUserViewModel registerUser)
        {
            try
            {
                AddHeaderPagarme();

                var clienteToAdd = new PagarmeCliente()
                {
                    Name = registerUser.Name,
                    Type = registerUser.Type,
                    Document = registerUser.Document,
                    Code = "Cliente_" + registerUser.Name.Split(" ", 10, StringSplitOptions.None)[0],
                    Email = registerUser.Email
                };

                HttpResponseMessage response = await client.PostAsJsonAsync(urlPagarme + "customers", clienteToAdd);
                string responseBody = await response.Content.ReadAsStringAsync();

                var customer = JsonConvert.DeserializeObject<PagarmeCliente>(responseBody);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return customer.Id;
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

        private string GerarSenhaAleatoria()
        {
            Random numAleatorio = new Random();

            var char1 = "V";
            var char2 = "a";
            var char3 = numAleatorio.Next(9).ToString();
            var char4 = numAleatorio.Next(9).ToString();
            var char5 = numAleatorio.Next(9).ToString();
            var char6 = numAleatorio.Next(9).ToString();
            var char7 = numAleatorio.Next(9).ToString();
            var char8 = "@";

            return char1 + char2 + char3 + char4 + char5 + char6 + char7 + char8;
        }
        #endregion

        #region EMAILS
        private bool SendEmailPagamentoPixRapidoRealizado(string email, string password)
        {
            try
            {
                MailMessage message = new MailMessage("contato@vaquinhaanimal.com.br", email);
                message.Subject = "Conta Criada - Vaquinha Animal";
                message.Body = "<div style='font-size: 12px; font-family: Verdana; background-color: #f8f8f8; margin-left: 20px;'>" +
                    "<img src='https://www.doadoresespeciais.com.br/assets/img/logo.png' style='width: 300px;'/><br/><br/>" +
                    "<h2>Obrigado por fazer parte do time da Vaquinha Animal</h2></br></br> <h3><u>Confira os detalhes da sua conta abaixo: </u></h2><br/><br/> " +
                    "<h4>Criamos uma conta para você poder realizar outras doações e acompanhar o status das doações já realizadas. Para acessar é muito simples.</h4>" +
                    "<h4>Acesse a plataforma: <a href='www.vaquinhaanimal.com.br'><strong>CLICANDO AQUI</strong></a> e utilize seu e-mail e a senha " + password + "</h4></div>";

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
                return true;
            }

            catch (Exception ex)
            {
                return false;
            }

        }

        private void SendEmailDoacaoRealizadaBoletoDoador(Doacao doacao, string name, string doadorEmail)
        {
            try
            {
                MailMessage message = new MailMessage("contato@vaquinhaanimal.com.br", doadorEmail);
                message.Subject = "Doação Realizada - Vaquinha Animal";
                message.Body = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'><html xmlns='http://www.w3.org/1999/xhtml' xmlns:o='urn:schemas-microsoft-com:office:office' style='font-family:arial, 'helvetica neue', helvetica, sans-serif'><head><meta charset='UTF-8'><meta content='width=device-width, initial-scale=1' name='viewport'><meta name='x-apple-disable-message-reformatting'><meta http-equiv='X-UA-Compatible' content='IE=edge'><meta content='telephone=no' name='format-detection'><title>New Email</title><!--[if (mso 16)]><style type='text/css'> a {text-decoration: none;} </style><![endif]--><!--[if gte mso 9]><style>sup { font-size: 100% !important; }</style><![endif]--><!--[if gte mso 9]><xml> <o:OfficeDocumentSettings> <o:AllowPNG></o:AllowPNG> <o:PixelsPerInch>96</o:PixelsPerInch> </o:OfficeDocumentSettings> </xml><![endif]--><style type='text/css'>#outlook a { padding:0;}.es-button { mso-style-priority:100!important; text-decoration:none!important;}a[x-apple-data-detectors] { color:inherit!important; text-decoration:none!important; font-size:inherit!important; font-family:inherit!important; font-weight:inherit!important; line-height:inherit!important;}.es-desk-hidden { display:none; float:left; overflow:hidden; width:0; max-height:0; line-height:0; mso-hide:all;}[data-ogsb] .es-button.es-button-1 { padding:10px 35px!important;}@media only screen and (max-width:600px) {p, ul li, ol li, a { line-height:150%!important } h1, h2, h3, h1 a, h2 a, h3 a { line-height:120% } h1 { font-size:30px!important; text-align:center } h2 { font-size:24px!important; text-align:left } h3 { font-size:20px!important; text-align:left } .es-header-body h1 a, .es-content-body h1 a, .es-footer-body h1 a { font-size:30px!important; text-align:center } .es-header-body h2 a, .es-content-body h2 a, .es-footer-body h2 a { font-size:24px!important; text-align:left } .es-header-body h3 a, .es-content-body h3 a, .es-footer-body h3 a { font-size:20px!important; text-align:left } .es-menu td a { font-size:14px!important } .es-header-body p, .es-header-body ul li, .es-header-body ol li, .es-header-body a { font-size:14px!important } .es-content-body p, .es-content-body ul li, .es-content-body ol li, .es-content-body a { font-size:14px!important } .es-footer-body p, .es-footer-body ul li, .es-footer-body ol li, .es-footer-body a { font-size:14px!important } .es-infoblock p, .es-infoblock ul li, .es-infoblock ol li, .es-infoblock a { font-size:12px!important } *[class='gmail-fix'] { display:none!important } .es-m-txt-c, .es-m-txt-c h1, .es-m-txt-c h2, .es-m-txt-c h3 { text-align:center!important } .es-m-txt-r, .es-m-txt-r h1, .es-m-txt-r h2, .es-m-txt-r h3 { text-align:right!important } .es-m-txt-l, .es-m-txt-l h1, .es-m-txt-l h2, .es-m-txt-l h3 { text-align:left!important } .es-m-txt-r img, .es-m-txt-c img, .es-m-txt-l img { display:inline!important } .es-button-border { display:inline-block!important } a.es-button, button.es-button { font-size:18px!important; display:inline-block!important } .es-adaptive table, .es-left, .es-right { width:100%!important } .es-content table, .es-header table, .es-footer table, .es-content, .es-footer, .es-header { width:100%!important; max-width:600px!important } .es-adapt-td { display:block!important; width:100%!important } .adapt-img { width:100%!important; height:auto!important } .es-m-p0 { padding:0!important } .es-m-p0r { padding-right:0!important } .es-m-p0l { padding-left:0!important } .es-m-p0t { padding-top:0!important } .es-m-p0b { padding-bottom:0!important } .es-m-p20b { padding-bottom:20px!important } .es-mobile-hidden, .es-hidden { display:none!important } tr.es-desk-hidden, td.es-desk-hidden, table.es-desk-hidden { width:auto!important; overflow:visible!important; float:none!important; max-height:inherit!important; line-height:inherit!important } tr.es-desk-hidden { display:table-row!important } table.es-desk-hidden { display:table!important } td.es-desk-menu-hidden { display:table-cell!important } .es-menu td { width:1%!important } table.es-table-not-adapt, .esd-block-html table { width:auto!important } table.es-social { display:inline-block!important } table.es-social td { display:inline-block!important } .es-desk-hidden { display:table-row!important; width:auto!important; overflow:visible!important; max-height:inherit!important } .es-m-p5 { padding:5px!important } .es-m-p5t { padding-top:5px!important } .es-m-p5b { padding-bottom:5px!important } .es-m-p5r { padding-right:5px!important } .es-m-p5l { padding-left:5px!important } .es-m-p10 { padding:10px!important } .es-m-p10t { padding-top:10px!important } .es-m-p10b { padding-bottom:10px!important } .es-m-p10r { padding-right:10px!important } .es-m-p10l { padding-left:10px!important } .es-m-p15 { padding:15px!important } .es-m-p15t { padding-top:15px!important } .es-m-p15b { padding-bottom:15px!important } .es-m-p15r { padding-right:15px!important } .es-m-p15l { padding-left:15px!important } .es-m-p20 { padding:20px!important } .es-m-p20t { padding-top:20px!important } .es-m-p20r { padding-right:20px!important } .es-m-p20l { padding-left:20px!important } .es-m-p25 { padding:25px!important } .es-m-p25t { padding-top:25px!important } .es-m-p25b { padding-bottom:25px!important } .es-m-p25r { padding-right:25px!important } .es-m-p25l { padding-left:25px!important } .es-m-p30 { padding:30px!important } .es-m-p30t { padding-top:30px!important } .es-m-p30b { padding-bottom:30px!important } .es-m-p30r { padding-right:30px!important } .es-m-p30l { padding-left:30px!important } .es-m-p35 { padding:35px!important } .es-m-p35t { padding-top:35px!important } .es-m-p35b { padding-bottom:35px!important } .es-m-p35r { padding-right:35px!important } .es-m-p35l { padding-left:35px!important } .es-m-p40 { padding:40px!important } .es-m-p40t { padding-top:40px!important } .es-m-p40b { padding-bottom:40px!important } .es-m-p40r { padding-right:40px!important } .es-m-p40l { padding-left:40px!important } }</style></head><body data-new-gr-c-s-loaded='14.1082.0' style='width:100%;font-family:arial, 'helvetica neue', helvetica, sans-serif;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;padding:0;Margin:0'><div class='es-wrapper-color' style='background-color:#ECEFF4'><!--[if gte mso 9]><v:background xmlns:v='urn:schemas-microsoft-com:vml' fill='t'> <v:fill type='tile' color='#eceff4'></v:fill> </v:background><![endif]--><table class='es-wrapper' width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;padding:0;Margin:0;width:100%;height:100%;background-repeat:repeat;background-position:center top;background-color:#ECEFF4'><tr><td valign='top' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' class='es-header' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-header-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:337px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:337px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0;padding-top:10px;padding-bottom:10px;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:12px'><img src='https://www.vaquinhaanimal.com.br/assets/img/logo_fundo_claro.png' alt='Logo' style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' height='30' title='Logo'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:203px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:203px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:600px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#ffffff;width:600px' bgcolor='#ffffff'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:40px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px;text-align:justify'>Olá, " + name + "<br><br>Muito obrigado por ter doado para a Vaquinha Animal. Seu pagamento só será compensado após o pagamento do boleto.</p></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px;text-align:justify'>Para pagar é fácil. Você pode clicar <a href="+doacao.Url_Download+">AQUI</a> para realizar o download do seu boleto ou acessar a plataforma, no menu MINHAS DOAÇÕES, clicar para realizar o download.</p></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'>Confira os detalhes da sua doação:</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><br></p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Data:</strong> " + doacao.Data + "</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Valor:</strong> R$ " + doacao.Valor + "</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Forma de Pagamento: </strong> Boleto</p></td></tr><tr><td align='center' style='padding:15px;Margin:0'><!--[if mso]><a href='https://www.vaquinhaanimal.com.br' target='_blank' hidden> <v:roundrect xmlns:v='urn:schemas-microsoft-com:vml' xmlns:w='urn:schemas-microsoft-com:office:word' esdevVmlButton href='https://www.vaquinhaanimal.com.br' style='height:41px; v-text-anchor:middle; width:271px' arcsize='50%' stroke='f' fillcolor='#fecd1c'> <w:anchorlock></w:anchorlock> <center style='color:#2e3440; font-family:arial, 'helvetica neue', helvetica, sans-serif; font-size:15px; font-weight:700; line-height:15px; mso-text-raise:1px'>Acessar a plataforma</center> </v:roundrect></a><![endif]--><!--[if !mso]><!-- --><span class='msohide es-button-border' style='border-style:solid;border-color:#4C566A;background:#FECD1C;border-width:0px;display:inline-block;border-radius:30px;width:auto;mso-hide:all'><a href='https://www.vaquinhaanimal.com.br' class='es-button es-button-1' target='_blank' style='mso-style-priority:100 !important;text-decoration:none;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;color:#2E3440;font-size:18px;padding:10px 35px;display:inline-block;background:#FECD1C;border-radius:30px;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-weight:bold;font-style:normal;line-height:22px;width:auto;text-align:center;mso-padding-alt:0;mso-border-alt:10px solid #FECD1C'>Acessar a plataforma</a></span><!--<![endif]--></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Precisa de alguma ajuda?</h1></td></tr></table></td></tr></table></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 class='p_name' style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>Telefone</h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='tel:+(000)123-456-789' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>+55 21 12345-6789</a></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='Margin:0;padding-bottom:15px;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone_1.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>E-mail<br></h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='mailto:contato@vaquinhaanimal.com.br' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>contato@vaquinhaanimal.com.br</a><br></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table class='es-footer' cellspacing='0' cellpadding='0' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' cellspacing='0' cellpadding='0' align='center' bgcolor='#ffffff' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-top:20px;padding-left:20px;padding-right:20px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:560px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Vaquinha Animal</h1></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='Margin:0;padding-top:20px;padding-bottom:20px;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:18px;color:#2E3440;font-size:12px'>© 2023 - Direitos Reservados - CNPJ: 48.173.612/0001-02 - Grupo Doadores Especiais</p></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table></td></tr></table></td></tr></table></td></tr></table></div></body></html>";

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

        private void SendEmailDoacaoRealizadaBoletoDonoCampanha(Doacao doacao, string donoCampanhaMail)
        {
            try
            {
                MailMessage message = new MailMessage("contato@vaquinhaanimal.com.br", donoCampanhaMail);
                message.Subject = "Doação Recebida - Vaquinha Animal";
                message.Body = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'><html xmlns='http://www.w3.org/1999/xhtml' xmlns:o='urn:schemas-microsoft-com:office:office' style='font-family:arial, 'helvetica neue', helvetica, sans-serif'><head><meta charset='UTF-8'><meta content='width=device-width, initial-scale=1' name='viewport'><meta name='x-apple-disable-message-reformatting'><meta http-equiv='X-UA-Compatible' content='IE=edge'><meta content='telephone=no' name='format-detection'><title>New Email</title><!--[if (mso 16)]><style type='text/css'> a {text-decoration: none;} </style><![endif]--><!--[if gte mso 9]><style>sup { font-size: 100% !important; }</style><![endif]--><!--[if gte mso 9]><xml> <o:OfficeDocumentSettings> <o:AllowPNG></o:AllowPNG> <o:PixelsPerInch>96</o:PixelsPerInch> </o:OfficeDocumentSettings> </xml><![endif]--><style type='text/css'>#outlook a { padding:0;}.es-button { mso-style-priority:100!important; text-decoration:none!important;}a[x-apple-data-detectors] { color:inherit!important; text-decoration:none!important; font-size:inherit!important; font-family:inherit!important; font-weight:inherit!important; line-height:inherit!important;}.es-desk-hidden { display:none; float:left; overflow:hidden; width:0; max-height:0; line-height:0; mso-hide:all;}[data-ogsb] .es-button.es-button-1 { padding:10px 35px!important;}@media only screen and (max-width:600px) {p, ul li, ol li, a { line-height:150%!important } h1, h2, h3, h1 a, h2 a, h3 a { line-height:120% } h1 { font-size:30px!important; text-align:center } h2 { font-size:24px!important; text-align:left } h3 { font-size:20px!important; text-align:left } .es-header-body h1 a, .es-content-body h1 a, .es-footer-body h1 a { font-size:30px!important; text-align:center } .es-header-body h2 a, .es-content-body h2 a, .es-footer-body h2 a { font-size:24px!important; text-align:left } .es-header-body h3 a, .es-content-body h3 a, .es-footer-body h3 a { font-size:20px!important; text-align:left } .es-menu td a { font-size:14px!important } .es-header-body p, .es-header-body ul li, .es-header-body ol li, .es-header-body a { font-size:14px!important } .es-content-body p, .es-content-body ul li, .es-content-body ol li, .es-content-body a { font-size:14px!important } .es-footer-body p, .es-footer-body ul li, .es-footer-body ol li, .es-footer-body a { font-size:14px!important } .es-infoblock p, .es-infoblock ul li, .es-infoblock ol li, .es-infoblock a { font-size:12px!important } *[class='gmail-fix'] { display:none!important } .es-m-txt-c, .es-m-txt-c h1, .es-m-txt-c h2, .es-m-txt-c h3 { text-align:center!important } .es-m-txt-r, .es-m-txt-r h1, .es-m-txt-r h2, .es-m-txt-r h3 { text-align:right!important } .es-m-txt-l, .es-m-txt-l h1, .es-m-txt-l h2, .es-m-txt-l h3 { text-align:left!important } .es-m-txt-r img, .es-m-txt-c img, .es-m-txt-l img { display:inline!important } .es-button-border { display:inline-block!important } a.es-button, button.es-button { font-size:18px!important; display:inline-block!important } .es-adaptive table, .es-left, .es-right { width:100%!important } .es-content table, .es-header table, .es-footer table, .es-content, .es-footer, .es-header { width:100%!important; max-width:600px!important } .es-adapt-td { display:block!important; width:100%!important } .adapt-img { width:100%!important; height:auto!important } .es-m-p0 { padding:0!important } .es-m-p0r { padding-right:0!important } .es-m-p0l { padding-left:0!important } .es-m-p0t { padding-top:0!important } .es-m-p0b { padding-bottom:0!important } .es-m-p20b { padding-bottom:20px!important } .es-mobile-hidden, .es-hidden { display:none!important } tr.es-desk-hidden, td.es-desk-hidden, table.es-desk-hidden { width:auto!important; overflow:visible!important; float:none!important; max-height:inherit!important; line-height:inherit!important } tr.es-desk-hidden { display:table-row!important } table.es-desk-hidden { display:table!important } td.es-desk-menu-hidden { display:table-cell!important } .es-menu td { width:1%!important } table.es-table-not-adapt, .esd-block-html table { width:auto!important } table.es-social { display:inline-block!important } table.es-social td { display:inline-block!important } .es-desk-hidden { display:table-row!important; width:auto!important; overflow:visible!important; max-height:inherit!important } .es-m-p5 { padding:5px!important } .es-m-p5t { padding-top:5px!important } .es-m-p5b { padding-bottom:5px!important } .es-m-p5r { padding-right:5px!important } .es-m-p5l { padding-left:5px!important } .es-m-p10 { padding:10px!important } .es-m-p10t { padding-top:10px!important } .es-m-p10b { padding-bottom:10px!important } .es-m-p10r { padding-right:10px!important } .es-m-p10l { padding-left:10px!important } .es-m-p15 { padding:15px!important } .es-m-p15t { padding-top:15px!important } .es-m-p15b { padding-bottom:15px!important } .es-m-p15r { padding-right:15px!important } .es-m-p15l { padding-left:15px!important } .es-m-p20 { padding:20px!important } .es-m-p20t { padding-top:20px!important } .es-m-p20r { padding-right:20px!important } .es-m-p20l { padding-left:20px!important } .es-m-p25 { padding:25px!important } .es-m-p25t { padding-top:25px!important } .es-m-p25b { padding-bottom:25px!important } .es-m-p25r { padding-right:25px!important } .es-m-p25l { padding-left:25px!important } .es-m-p30 { padding:30px!important } .es-m-p30t { padding-top:30px!important } .es-m-p30b { padding-bottom:30px!important } .es-m-p30r { padding-right:30px!important } .es-m-p30l { padding-left:30px!important } .es-m-p35 { padding:35px!important } .es-m-p35t { padding-top:35px!important } .es-m-p35b { padding-bottom:35px!important } .es-m-p35r { padding-right:35px!important } .es-m-p35l { padding-left:35px!important } .es-m-p40 { padding:40px!important } .es-m-p40t { padding-top:40px!important } .es-m-p40b { padding-bottom:40px!important } .es-m-p40r { padding-right:40px!important } .es-m-p40l { padding-left:40px!important } }</style></head><body data-new-gr-c-s-loaded='14.1082.0' style='width:100%;font-family:arial, 'helvetica neue', helvetica, sans-serif;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;padding:0;Margin:0'><div class='es-wrapper-color' style='background-color:#ECEFF4'><!--[if gte mso 9]><v:background xmlns:v='urn:schemas-microsoft-com:vml' fill='t'> <v:fill type='tile' color='#eceff4'></v:fill> </v:background><![endif]--><table class='es-wrapper' width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;padding:0;Margin:0;width:100%;height:100%;background-repeat:repeat;background-position:center top;background-color:#ECEFF4'><tr><td valign='top' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' class='es-header' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-header-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:337px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:337px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0;padding-top:10px;padding-bottom:10px;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:12px'><img src='https://www.vaquinhaanimal.com.br/assets/img/logo_fundo_claro.png' alt='Logo' style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' height='30' title='Logo'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:203px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:203px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:600px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#ffffff;width:600px' bgcolor='#ffffff'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:40px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px;text-align:justify'>Sua campanha recebeu uma doação. A forma de pagamento escolhido pelo doador foi BOLETO.</p></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px;text-align:justify'>Só lembrando que o valor só é compensado após o pagamento do boleto e, só após isso, irá constar no relatório da campanha.</p></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'>Confira os detalhes da doação:</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><br></p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Data:</strong> " + doacao.Data + "</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Valor:</strong> R$ " + (doacao.Valor - doacao.ValorDestinadoPlataforma) + "</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Forma de Pagamento: </strong> Boleto</p></td></tr><tr><td align='center' style='padding:15px;Margin:0'><!--[if mso]><a href='https://www.vaquinhaanimal.com.br' target='_blank' hidden> <v:roundrect xmlns:v='urn:schemas-microsoft-com:vml' xmlns:w='urn:schemas-microsoft-com:office:word' esdevVmlButton href='https://www.vaquinhaanimal.com.br' style='height:41px; v-text-anchor:middle; width:271px' arcsize='50%' stroke='f' fillcolor='#fecd1c'> <w:anchorlock></w:anchorlock> <center style='color:#2e3440; font-family:arial, 'helvetica neue', helvetica, sans-serif; font-size:15px; font-weight:700; line-height:15px; mso-text-raise:1px'>Acessar a plataforma</center> </v:roundrect></a><![endif]--><!--[if !mso]><!-- --><span class='msohide es-button-border' style='border-style:solid;border-color:#4C566A;background:#FECD1C;border-width:0px;display:inline-block;border-radius:30px;width:auto;mso-hide:all'><a href='https://www.vaquinhaanimal.com.br' class='es-button es-button-1' target='_blank' style='mso-style-priority:100 !important;text-decoration:none;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;color:#2E3440;font-size:18px;padding:10px 35px;display:inline-block;background:#FECD1C;border-radius:30px;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-weight:bold;font-style:normal;line-height:22px;width:auto;text-align:center;mso-padding-alt:0;mso-border-alt:10px solid #FECD1C'>Acessar a plataforma</a></span><!--<![endif]--></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Precisa de alguma ajuda?</h1></td></tr></table></td></tr></table></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 class='p_name' style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>Telefone</h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='tel:+(000)123-456-789' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>+55 21 12345-6789</a></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='Margin:0;padding-bottom:15px;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone_1.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>E-mail<br></h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='mailto:contato@vaquinhaanimal.com.br' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>contato@vaquinhaanimal.com.br</a><br></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table class='es-footer' cellspacing='0' cellpadding='0' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' cellspacing='0' cellpadding='0' align='center' bgcolor='#ffffff' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-top:20px;padding-left:20px;padding-right:20px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:560px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Vaquinha Animal</h1></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='Margin:0;padding-top:20px;padding-bottom:20px;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:18px;color:#2E3440;font-size:12px'>© 2023 - Direitos Reservados - CNPJ: 48.173.612/0001-02 - Grupo Doadores Especiais</p></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table></td></tr></table></td></tr></table></td></tr></table></div></body></html>";

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

        private void SendEmailDoacaoRealizadaPixDoador(Doacao doacao, string name, string doadorEmail)
        {
            try
            {
                MailMessage message = new MailMessage("contato@vaquinhaanimal.com.br", doadorEmail);
                message.Subject = "Doação Realizada - Vaquinha Animal";
                message.Body = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'><html xmlns='http://www.w3.org/1999/xhtml' xmlns:o='urn:schemas-microsoft-com:office:office' style='font-family:arial, 'helvetica neue', helvetica, sans-serif'><head><meta charset='UTF-8'><meta content='width=device-width, initial-scale=1' name='viewport'><meta name='x-apple-disable-message-reformatting'><meta http-equiv='X-UA-Compatible' content='IE=edge'><meta content='telephone=no' name='format-detection'><title>New Email</title><!--[if (mso 16)]><style type='text/css'> a {text-decoration: none;} </style><![endif]--><!--[if gte mso 9]><style>sup { font-size: 100% !important; }</style><![endif]--><!--[if gte mso 9]><xml> <o:OfficeDocumentSettings> <o:AllowPNG></o:AllowPNG> <o:PixelsPerInch>96</o:PixelsPerInch> </o:OfficeDocumentSettings> </xml><![endif]--><style type='text/css'>#outlook a { padding:0;}.es-button { mso-style-priority:100!important; text-decoration:none!important;}a[x-apple-data-detectors] { color:inherit!important; text-decoration:none!important; font-size:inherit!important; font-family:inherit!important; font-weight:inherit!important; line-height:inherit!important;}.es-desk-hidden { display:none; float:left; overflow:hidden; width:0; max-height:0; line-height:0; mso-hide:all;}[data-ogsb] .es-button.es-button-1 { padding:10px 35px!important;}@media only screen and (max-width:600px) {p, ul li, ol li, a { line-height:150%!important } h1, h2, h3, h1 a, h2 a, h3 a { line-height:120% } h1 { font-size:30px!important; text-align:center } h2 { font-size:24px!important; text-align:left } h3 { font-size:20px!important; text-align:left } .es-header-body h1 a, .es-content-body h1 a, .es-footer-body h1 a { font-size:30px!important; text-align:center } .es-header-body h2 a, .es-content-body h2 a, .es-footer-body h2 a { font-size:24px!important; text-align:left } .es-header-body h3 a, .es-content-body h3 a, .es-footer-body h3 a { font-size:20px!important; text-align:left } .es-menu td a { font-size:14px!important } .es-header-body p, .es-header-body ul li, .es-header-body ol li, .es-header-body a { font-size:14px!important } .es-content-body p, .es-content-body ul li, .es-content-body ol li, .es-content-body a { font-size:14px!important } .es-footer-body p, .es-footer-body ul li, .es-footer-body ol li, .es-footer-body a { font-size:14px!important } .es-infoblock p, .es-infoblock ul li, .es-infoblock ol li, .es-infoblock a { font-size:12px!important } *[class='gmail-fix'] { display:none!important } .es-m-txt-c, .es-m-txt-c h1, .es-m-txt-c h2, .es-m-txt-c h3 { text-align:center!important } .es-m-txt-r, .es-m-txt-r h1, .es-m-txt-r h2, .es-m-txt-r h3 { text-align:right!important } .es-m-txt-l, .es-m-txt-l h1, .es-m-txt-l h2, .es-m-txt-l h3 { text-align:left!important } .es-m-txt-r img, .es-m-txt-c img, .es-m-txt-l img { display:inline!important } .es-button-border { display:inline-block!important } a.es-button, button.es-button { font-size:18px!important; display:inline-block!important } .es-adaptive table, .es-left, .es-right { width:100%!important } .es-content table, .es-header table, .es-footer table, .es-content, .es-footer, .es-header { width:100%!important; max-width:600px!important } .es-adapt-td { display:block!important; width:100%!important } .adapt-img { width:100%!important; height:auto!important } .es-m-p0 { padding:0!important } .es-m-p0r { padding-right:0!important } .es-m-p0l { padding-left:0!important } .es-m-p0t { padding-top:0!important } .es-m-p0b { padding-bottom:0!important } .es-m-p20b { padding-bottom:20px!important } .es-mobile-hidden, .es-hidden { display:none!important } tr.es-desk-hidden, td.es-desk-hidden, table.es-desk-hidden { width:auto!important; overflow:visible!important; float:none!important; max-height:inherit!important; line-height:inherit!important } tr.es-desk-hidden { display:table-row!important } table.es-desk-hidden { display:table!important } td.es-desk-menu-hidden { display:table-cell!important } .es-menu td { width:1%!important } table.es-table-not-adapt, .esd-block-html table { width:auto!important } table.es-social { display:inline-block!important } table.es-social td { display:inline-block!important } .es-desk-hidden { display:table-row!important; width:auto!important; overflow:visible!important; max-height:inherit!important } .es-m-p5 { padding:5px!important } .es-m-p5t { padding-top:5px!important } .es-m-p5b { padding-bottom:5px!important } .es-m-p5r { padding-right:5px!important } .es-m-p5l { padding-left:5px!important } .es-m-p10 { padding:10px!important } .es-m-p10t { padding-top:10px!important } .es-m-p10b { padding-bottom:10px!important } .es-m-p10r { padding-right:10px!important } .es-m-p10l { padding-left:10px!important } .es-m-p15 { padding:15px!important } .es-m-p15t { padding-top:15px!important } .es-m-p15b { padding-bottom:15px!important } .es-m-p15r { padding-right:15px!important } .es-m-p15l { padding-left:15px!important } .es-m-p20 { padding:20px!important } .es-m-p20t { padding-top:20px!important } .es-m-p20r { padding-right:20px!important } .es-m-p20l { padding-left:20px!important } .es-m-p25 { padding:25px!important } .es-m-p25t { padding-top:25px!important } .es-m-p25b { padding-bottom:25px!important } .es-m-p25r { padding-right:25px!important } .es-m-p25l { padding-left:25px!important } .es-m-p30 { padding:30px!important } .es-m-p30t { padding-top:30px!important } .es-m-p30b { padding-bottom:30px!important } .es-m-p30r { padding-right:30px!important } .es-m-p30l { padding-left:30px!important } .es-m-p35 { padding:35px!important } .es-m-p35t { padding-top:35px!important } .es-m-p35b { padding-bottom:35px!important } .es-m-p35r { padding-right:35px!important } .es-m-p35l { padding-left:35px!important } .es-m-p40 { padding:40px!important } .es-m-p40t { padding-top:40px!important } .es-m-p40b { padding-bottom:40px!important } .es-m-p40r { padding-right:40px!important } .es-m-p40l { padding-left:40px!important } }</style></head><body data-new-gr-c-s-loaded='14.1082.0' style='width:100%;font-family:arial, 'helvetica neue', helvetica, sans-serif;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;padding:0;Margin:0'><div class='es-wrapper-color' style='background-color:#ECEFF4'><!--[if gte mso 9]><v:background xmlns:v='urn:schemas-microsoft-com:vml' fill='t'> <v:fill type='tile' color='#eceff4'></v:fill> </v:background><![endif]--><table class='es-wrapper' width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;padding:0;Margin:0;width:100%;height:100%;background-repeat:repeat;background-position:center top;background-color:#ECEFF4'><tr><td valign='top' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' class='es-header' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-header-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:337px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:337px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0;padding-top:10px;padding-bottom:10px;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:12px'><img src='https://www.vaquinhaanimal.com.br/assets/img/logo_fundo_claro.png' alt='Logo' style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' height='30' title='Logo'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:203px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:203px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:600px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;position:relative'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:12px'><img class='adapt-img' src='https://i.ibb.co/CJD2fQt/image16857378637056256-2.png' alt title width='600' style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic'></a></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#ffffff;width:600px' bgcolor='#ffffff'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:40px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px;text-align:justify'>Olá, " + name + "<br><br>Muito obrigado por ter doado para a Vaquinha Animal.</p></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'>Confira os detalhes da sua doação:</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><br></p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Data:</strong> " + doacao.Data + "</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Valor:</strong> R$ " + doacao.Valor + "</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Forma de Pagamento: </strong> PIX</p></td></tr><tr><td align='center' style='padding:15px;Margin:0'><!--[if mso]><a href='https://www.vaquinhaanimal.com.br' target='_blank' hidden> <v:roundrect xmlns:v='urn:schemas-microsoft-com:vml' xmlns:w='urn:schemas-microsoft-com:office:word' esdevVmlButton href='https://www.vaquinhaanimal.com.br' style='height:41px; v-text-anchor:middle; width:271px' arcsize='50%' stroke='f' fillcolor='#fecd1c'> <w:anchorlock></w:anchorlock> <center style='color:#2e3440; font-family:arial, 'helvetica neue', helvetica, sans-serif; font-size:15px; font-weight:700; line-height:15px; mso-text-raise:1px'>Acessar a plataforma</center> </v:roundrect></a><![endif]--><!--[if !mso]><!-- --><span class='msohide es-button-border' style='border-style:solid;border-color:#4C566A;background:#FECD1C;border-width:0px;display:inline-block;border-radius:30px;width:auto;mso-hide:all'><a href='https://www.vaquinhaanimal.com.br' class='es-button es-button-1' target='_blank' style='mso-style-priority:100 !important;text-decoration:none;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;color:#2E3440;font-size:18px;padding:10px 35px;display:inline-block;background:#FECD1C;border-radius:30px;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-weight:bold;font-style:normal;line-height:22px;width:auto;text-align:center;mso-padding-alt:0;mso-border-alt:10px solid #FECD1C'>Acessar a plataforma</a></span><!--<![endif]--></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Precisa de alguma ajuda?</h1></td></tr></table></td></tr></table></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 class='p_name' style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>Telefone</h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='tel:+(000)123-456-789' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>+55 21 12345-6789</a></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='Margin:0;padding-bottom:15px;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone_1.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>E-mail<br></h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='mailto:contato@vaquinhaanimal.com.br' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>contato@vaquinhaanimal.com.br</a><br></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table class='es-footer' cellspacing='0' cellpadding='0' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' cellspacing='0' cellpadding='0' align='center' bgcolor='#ffffff' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-top:20px;padding-left:20px;padding-right:20px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:560px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Vaquinha Animal</h1></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='Margin:0;padding-top:20px;padding-bottom:20px;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:18px;color:#2E3440;font-size:12px'>© 2023 - Direitos Reservados - CNPJ: 48.173.612/0001-02 - Grupo Doadores Especiais</p></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table></td></tr></table></td></tr></table></td></tr></table></div></body></html>";

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

        private void SendEmailDoacaoRealizadaPixDonoCampanha(Doacao doacao, string donoCampanhaMail)
        {
            try
            {
                MailMessage message = new MailMessage("contato@vaquinhaanimal.com.br", donoCampanhaMail);
                message.Subject = "Doação Recebida - Vaquinha Animal";
                message.Body = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'><html xmlns='http://www.w3.org/1999/xhtml' xmlns:o='urn:schemas-microsoft-com:office:office' style='font-family:arial, 'helvetica neue', helvetica, sans-serif'><head><meta charset='UTF-8'><meta content='width=device-width, initial-scale=1' name='viewport'><meta name='x-apple-disable-message-reformatting'><meta http-equiv='X-UA-Compatible' content='IE=edge'><meta content='telephone=no' name='format-detection'><title>New Email</title><!--[if (mso 16)]><style type='text/css'> a {text-decoration: none;} </style><![endif]--><!--[if gte mso 9]><style>sup { font-size: 100% !important; }</style><![endif]--><!--[if gte mso 9]><xml> <o:OfficeDocumentSettings> <o:AllowPNG></o:AllowPNG> <o:PixelsPerInch>96</o:PixelsPerInch> </o:OfficeDocumentSettings> </xml><![endif]--><style type='text/css'>#outlook a { padding:0;}.es-button { mso-style-priority:100!important; text-decoration:none!important;}a[x-apple-data-detectors] { color:inherit!important; text-decoration:none!important; font-size:inherit!important; font-family:inherit!important; font-weight:inherit!important; line-height:inherit!important;}.es-desk-hidden { display:none; float:left; overflow:hidden; width:0; max-height:0; line-height:0; mso-hide:all;}[data-ogsb] .es-button.es-button-1 { padding:10px 35px!important;}@media only screen and (max-width:600px) {p, ul li, ol li, a { line-height:150%!important } h1, h2, h3, h1 a, h2 a, h3 a { line-height:120% } h1 { font-size:30px!important; text-align:center } h2 { font-size:24px!important; text-align:left } h3 { font-size:20px!important; text-align:left } .es-header-body h1 a, .es-content-body h1 a, .es-footer-body h1 a { font-size:30px!important; text-align:center } .es-header-body h2 a, .es-content-body h2 a, .es-footer-body h2 a { font-size:24px!important; text-align:left } .es-header-body h3 a, .es-content-body h3 a, .es-footer-body h3 a { font-size:20px!important; text-align:left } .es-menu td a { font-size:14px!important } .es-header-body p, .es-header-body ul li, .es-header-body ol li, .es-header-body a { font-size:14px!important } .es-content-body p, .es-content-body ul li, .es-content-body ol li, .es-content-body a { font-size:14px!important } .es-footer-body p, .es-footer-body ul li, .es-footer-body ol li, .es-footer-body a { font-size:14px!important } .es-infoblock p, .es-infoblock ul li, .es-infoblock ol li, .es-infoblock a { font-size:12px!important } *[class='gmail-fix'] { display:none!important } .es-m-txt-c, .es-m-txt-c h1, .es-m-txt-c h2, .es-m-txt-c h3 { text-align:center!important } .es-m-txt-r, .es-m-txt-r h1, .es-m-txt-r h2, .es-m-txt-r h3 { text-align:right!important } .es-m-txt-l, .es-m-txt-l h1, .es-m-txt-l h2, .es-m-txt-l h3 { text-align:left!important } .es-m-txt-r img, .es-m-txt-c img, .es-m-txt-l img { display:inline!important } .es-button-border { display:inline-block!important } a.es-button, button.es-button { font-size:18px!important; display:inline-block!important } .es-adaptive table, .es-left, .es-right { width:100%!important } .es-content table, .es-header table, .es-footer table, .es-content, .es-footer, .es-header { width:100%!important; max-width:600px!important } .es-adapt-td { display:block!important; width:100%!important } .adapt-img { width:100%!important; height:auto!important } .es-m-p0 { padding:0!important } .es-m-p0r { padding-right:0!important } .es-m-p0l { padding-left:0!important } .es-m-p0t { padding-top:0!important } .es-m-p0b { padding-bottom:0!important } .es-m-p20b { padding-bottom:20px!important } .es-mobile-hidden, .es-hidden { display:none!important } tr.es-desk-hidden, td.es-desk-hidden, table.es-desk-hidden { width:auto!important; overflow:visible!important; float:none!important; max-height:inherit!important; line-height:inherit!important } tr.es-desk-hidden { display:table-row!important } table.es-desk-hidden { display:table!important } td.es-desk-menu-hidden { display:table-cell!important } .es-menu td { width:1%!important } table.es-table-not-adapt, .esd-block-html table { width:auto!important } table.es-social { display:inline-block!important } table.es-social td { display:inline-block!important } .es-desk-hidden { display:table-row!important; width:auto!important; overflow:visible!important; max-height:inherit!important } .es-m-p5 { padding:5px!important } .es-m-p5t { padding-top:5px!important } .es-m-p5b { padding-bottom:5px!important } .es-m-p5r { padding-right:5px!important } .es-m-p5l { padding-left:5px!important } .es-m-p10 { padding:10px!important } .es-m-p10t { padding-top:10px!important } .es-m-p10b { padding-bottom:10px!important } .es-m-p10r { padding-right:10px!important } .es-m-p10l { padding-left:10px!important } .es-m-p15 { padding:15px!important } .es-m-p15t { padding-top:15px!important } .es-m-p15b { padding-bottom:15px!important } .es-m-p15r { padding-right:15px!important } .es-m-p15l { padding-left:15px!important } .es-m-p20 { padding:20px!important } .es-m-p20t { padding-top:20px!important } .es-m-p20r { padding-right:20px!important } .es-m-p20l { padding-left:20px!important } .es-m-p25 { padding:25px!important } .es-m-p25t { padding-top:25px!important } .es-m-p25b { padding-bottom:25px!important } .es-m-p25r { padding-right:25px!important } .es-m-p25l { padding-left:25px!important } .es-m-p30 { padding:30px!important } .es-m-p30t { padding-top:30px!important } .es-m-p30b { padding-bottom:30px!important } .es-m-p30r { padding-right:30px!important } .es-m-p30l { padding-left:30px!important } .es-m-p35 { padding:35px!important } .es-m-p35t { padding-top:35px!important } .es-m-p35b { padding-bottom:35px!important } .es-m-p35r { padding-right:35px!important } .es-m-p35l { padding-left:35px!important } .es-m-p40 { padding:40px!important } .es-m-p40t { padding-top:40px!important } .es-m-p40b { padding-bottom:40px!important } .es-m-p40r { padding-right:40px!important } .es-m-p40l { padding-left:40px!important } }</style></head><body data-new-gr-c-s-loaded='14.1082.0' style='width:100%;font-family:arial, 'helvetica neue', helvetica, sans-serif;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;padding:0;Margin:0'><div class='es-wrapper-color' style='background-color:#ECEFF4'><!--[if gte mso 9]><v:background xmlns:v='urn:schemas-microsoft-com:vml' fill='t'> <v:fill type='tile' color='#eceff4'></v:fill> </v:background><![endif]--><table class='es-wrapper' width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;padding:0;Margin:0;width:100%;height:100%;background-repeat:repeat;background-position:center top;background-color:#ECEFF4'><tr><td valign='top' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' class='es-header' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-header-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:337px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:337px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0;padding-top:10px;padding-bottom:10px;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:12px'><img src='https://www.vaquinhaanimal.com.br/assets/img/logo_fundo_claro.png' alt='Logo' style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' height='30' title='Logo'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:203px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:203px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:600px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;position:relative'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:12px'><img class='adapt-img' src='https://i.ibb.co/CJD2fQt/image16857378637056256-2.png' alt title width='600' style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic'></a></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#ffffff;width:600px' bgcolor='#ffffff'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:40px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px;text-align:justify'>Sua campanha recebeu uma doação. A forma de pagamento escolhido pelo doador foi PIX.</p></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'>Confira os detalhes da doação:</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><br></p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Data:</strong> " + doacao.Data + "</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Valor:</strong> R$ " + (doacao.Valor - doacao.ValorDestinadoPlataforma) + "</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Forma de Pagamento: </strong> PIX</p></td></tr><tr><td align='center' style='padding:15px;Margin:0'><!--[if mso]><a href='https://www.vaquinhaanimal.com.br' target='_blank' hidden> <v:roundrect xmlns:v='urn:schemas-microsoft-com:vml' xmlns:w='urn:schemas-microsoft-com:office:word' esdevVmlButton href='https://www.vaquinhaanimal.com.br' style='height:41px; v-text-anchor:middle; width:271px' arcsize='50%' stroke='f' fillcolor='#fecd1c'> <w:anchorlock></w:anchorlock> <center style='color:#2e3440; font-family:arial, 'helvetica neue', helvetica, sans-serif; font-size:15px; font-weight:700; line-height:15px; mso-text-raise:1px'>Acessar a plataforma</center> </v:roundrect></a><![endif]--><!--[if !mso]><!-- --><span class='msohide es-button-border' style='border-style:solid;border-color:#4C566A;background:#FECD1C;border-width:0px;display:inline-block;border-radius:30px;width:auto;mso-hide:all'><a href='https://www.vaquinhaanimal.com.br' class='es-button es-button-1' target='_blank' style='mso-style-priority:100 !important;text-decoration:none;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;color:#2E3440;font-size:18px;padding:10px 35px;display:inline-block;background:#FECD1C;border-radius:30px;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-weight:bold;font-style:normal;line-height:22px;width:auto;text-align:center;mso-padding-alt:0;mso-border-alt:10px solid #FECD1C'>Acessar a plataforma</a></span><!--<![endif]--></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Precisa de alguma ajuda?</h1></td></tr></table></td></tr></table></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 class='p_name' style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>Telefone</h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='tel:+(000)123-456-789' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>+55 21 12345-6789</a></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='Margin:0;padding-bottom:15px;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone_1.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>E-mail<br></h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='mailto:contato@vaquinhaanimal.com.br' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>contato@vaquinhaanimal.com.br</a><br></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table class='es-footer' cellspacing='0' cellpadding='0' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' cellspacing='0' cellpadding='0' align='center' bgcolor='#ffffff' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-top:20px;padding-left:20px;padding-right:20px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:560px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Vaquinha Animal</h1></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='Margin:0;padding-top:20px;padding-bottom:20px;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:18px;color:#2E3440;font-size:12px'>© 2023 - Direitos Reservados - CNPJ: 48.173.612/0001-02 - Grupo Doadores Especiais</p></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table></td></tr></table></td></tr></table></td></tr></table></div></body></html>";

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

        private void SendEmailDoacaoRealizadaCartaoDoador(Doacao doacao, string name, string doadorEmail)
        {
            var status = "";

            if (doacao.Status == "paid")
            {
                status = "Pago";
            } 
            else if (doacao.Status == "pending")
            {
                status = "Pendente";
            }
            else if (doacao.Status == "processing")
            {
                status = "Processando";
            }

            try
            {
                MailMessage message = new MailMessage("contato@vaquinhaanimal.com.br", doadorEmail);
                message.Subject = "Doação Realizada - Vaquinha Animal";
                message.Body = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'><html xmlns='http://www.w3.org/1999/xhtml' xmlns:o='urn:schemas-microsoft-com:office:office' style='font-family:arial, 'helvetica neue', helvetica, sans-serif'><head><meta charset='UTF-8'><meta content='width=device-width, initial-scale=1' name='viewport'><meta name='x-apple-disable-message-reformatting'><meta http-equiv='X-UA-Compatible' content='IE=edge'><meta content='telephone=no' name='format-detection'><title>New Email</title><!--[if (mso 16)]><style type='text/css'> a {text-decoration: none;} </style><![endif]--><!--[if gte mso 9]><style>sup { font-size: 100% !important; }</style><![endif]--><!--[if gte mso 9]><xml> <o:OfficeDocumentSettings> <o:AllowPNG></o:AllowPNG> <o:PixelsPerInch>96</o:PixelsPerInch> </o:OfficeDocumentSettings> </xml><![endif]--><style type='text/css'>#outlook a { padding:0;}.es-button { mso-style-priority:100!important; text-decoration:none!important;}a[x-apple-data-detectors] { color:inherit!important; text-decoration:none!important; font-size:inherit!important; font-family:inherit!important; font-weight:inherit!important; line-height:inherit!important;}.es-desk-hidden { display:none; float:left; overflow:hidden; width:0; max-height:0; line-height:0; mso-hide:all;}[data-ogsb] .es-button.es-button-1 { padding:10px 35px!important;}@media only screen and (max-width:600px) {p, ul li, ol li, a { line-height:150%!important } h1, h2, h3, h1 a, h2 a, h3 a { line-height:120% } h1 { font-size:30px!important; text-align:center } h2 { font-size:24px!important; text-align:left } h3 { font-size:20px!important; text-align:left } .es-header-body h1 a, .es-content-body h1 a, .es-footer-body h1 a { font-size:30px!important; text-align:center } .es-header-body h2 a, .es-content-body h2 a, .es-footer-body h2 a { font-size:24px!important; text-align:left } .es-header-body h3 a, .es-content-body h3 a, .es-footer-body h3 a { font-size:20px!important; text-align:left } .es-menu td a { font-size:14px!important } .es-header-body p, .es-header-body ul li, .es-header-body ol li, .es-header-body a { font-size:14px!important } .es-content-body p, .es-content-body ul li, .es-content-body ol li, .es-content-body a { font-size:14px!important } .es-footer-body p, .es-footer-body ul li, .es-footer-body ol li, .es-footer-body a { font-size:14px!important } .es-infoblock p, .es-infoblock ul li, .es-infoblock ol li, .es-infoblock a { font-size:12px!important } *[class='gmail-fix'] { display:none!important } .es-m-txt-c, .es-m-txt-c h1, .es-m-txt-c h2, .es-m-txt-c h3 { text-align:center!important } .es-m-txt-r, .es-m-txt-r h1, .es-m-txt-r h2, .es-m-txt-r h3 { text-align:right!important } .es-m-txt-l, .es-m-txt-l h1, .es-m-txt-l h2, .es-m-txt-l h3 { text-align:left!important } .es-m-txt-r img, .es-m-txt-c img, .es-m-txt-l img { display:inline!important } .es-button-border { display:inline-block!important } a.es-button, button.es-button { font-size:18px!important; display:inline-block!important } .es-adaptive table, .es-left, .es-right { width:100%!important } .es-content table, .es-header table, .es-footer table, .es-content, .es-footer, .es-header { width:100%!important; max-width:600px!important } .es-adapt-td { display:block!important; width:100%!important } .adapt-img { width:100%!important; height:auto!important } .es-m-p0 { padding:0!important } .es-m-p0r { padding-right:0!important } .es-m-p0l { padding-left:0!important } .es-m-p0t { padding-top:0!important } .es-m-p0b { padding-bottom:0!important } .es-m-p20b { padding-bottom:20px!important } .es-mobile-hidden, .es-hidden { display:none!important } tr.es-desk-hidden, td.es-desk-hidden, table.es-desk-hidden { width:auto!important; overflow:visible!important; float:none!important; max-height:inherit!important; line-height:inherit!important } tr.es-desk-hidden { display:table-row!important } table.es-desk-hidden { display:table!important } td.es-desk-menu-hidden { display:table-cell!important } .es-menu td { width:1%!important } table.es-table-not-adapt, .esd-block-html table { width:auto!important } table.es-social { display:inline-block!important } table.es-social td { display:inline-block!important } .es-desk-hidden { display:table-row!important; width:auto!important; overflow:visible!important; max-height:inherit!important } .es-m-p5 { padding:5px!important } .es-m-p5t { padding-top:5px!important } .es-m-p5b { padding-bottom:5px!important } .es-m-p5r { padding-right:5px!important } .es-m-p5l { padding-left:5px!important } .es-m-p10 { padding:10px!important } .es-m-p10t { padding-top:10px!important } .es-m-p10b { padding-bottom:10px!important } .es-m-p10r { padding-right:10px!important } .es-m-p10l { padding-left:10px!important } .es-m-p15 { padding:15px!important } .es-m-p15t { padding-top:15px!important } .es-m-p15b { padding-bottom:15px!important } .es-m-p15r { padding-right:15px!important } .es-m-p15l { padding-left:15px!important } .es-m-p20 { padding:20px!important } .es-m-p20t { padding-top:20px!important } .es-m-p20r { padding-right:20px!important } .es-m-p20l { padding-left:20px!important } .es-m-p25 { padding:25px!important } .es-m-p25t { padding-top:25px!important } .es-m-p25b { padding-bottom:25px!important } .es-m-p25r { padding-right:25px!important } .es-m-p25l { padding-left:25px!important } .es-m-p30 { padding:30px!important } .es-m-p30t { padding-top:30px!important } .es-m-p30b { padding-bottom:30px!important } .es-m-p30r { padding-right:30px!important } .es-m-p30l { padding-left:30px!important } .es-m-p35 { padding:35px!important } .es-m-p35t { padding-top:35px!important } .es-m-p35b { padding-bottom:35px!important } .es-m-p35r { padding-right:35px!important } .es-m-p35l { padding-left:35px!important } .es-m-p40 { padding:40px!important } .es-m-p40t { padding-top:40px!important } .es-m-p40b { padding-bottom:40px!important } .es-m-p40r { padding-right:40px!important } .es-m-p40l { padding-left:40px!important } }</style></head><body data-new-gr-c-s-loaded='14.1082.0' style='width:100%;font-family:arial, 'helvetica neue', helvetica, sans-serif;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;padding:0;Margin:0'><div class='es-wrapper-color' style='background-color:#ECEFF4'><!--[if gte mso 9]><v:background xmlns:v='urn:schemas-microsoft-com:vml' fill='t'> <v:fill type='tile' color='#eceff4'></v:fill> </v:background><![endif]--><table class='es-wrapper' width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;padding:0;Margin:0;width:100%;height:100%;background-repeat:repeat;background-position:center top;background-color:#ECEFF4'><tr><td valign='top' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' class='es-header' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-header-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:337px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:337px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0;padding-top:10px;padding-bottom:10px;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:12px'><img src='https://www.vaquinhaanimal.com.br/assets/img/logo_fundo_claro.png' alt='Logo' style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' height='30' title='Logo'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:203px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:203px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:600px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#ffffff;width:600px' bgcolor='#ffffff'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:40px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px;text-align:justify'>Olá, " + name + "<br><br>Muito obrigado por ter doado para a Vaquinha Animal. Acompanhe o Status do seu pagamento.</p></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'>Confira os detalhes da sua doação:</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><br></p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Data:</strong> " + doacao.Data + "</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Valor:</strong> R$ " + doacao.Valor + "</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Forma de Pagamento: </strong> Cartão de Crédito</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Status: </strong>" + status + "</p></td></tr><tr><td align='center' style='padding:15px;Margin:0'><!--[if mso]><a href='https://www.vaquinhaanimal.com.br' target='_blank' hidden> <v:roundrect xmlns:v='urn:schemas-microsoft-com:vml' xmlns:w='urn:schemas-microsoft-com:office:word' esdevVmlButton href='https://www.vaquinhaanimal.com.br' style='height:41px; v-text-anchor:middle; width:271px' arcsize='50%' stroke='f' fillcolor='#fecd1c'> <w:anchorlock></w:anchorlock> <center style='color:#2e3440; font-family:arial, 'helvetica neue', helvetica, sans-serif; font-size:15px; font-weight:700; line-height:15px; mso-text-raise:1px'>Acessar a plataforma</center> </v:roundrect></a><![endif]--><!--[if !mso]><!-- --><span class='msohide es-button-border' style='border-style:solid;border-color:#4C566A;background:#FECD1C;border-width:0px;display:inline-block;border-radius:30px;width:auto;mso-hide:all'><a href='https://www.vaquinhaanimal.com.br' class='es-button es-button-1' target='_blank' style='mso-style-priority:100 !important;text-decoration:none;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;color:#2E3440;font-size:18px;padding:10px 35px;display:inline-block;background:#FECD1C;border-radius:30px;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-weight:bold;font-style:normal;line-height:22px;width:auto;text-align:center;mso-padding-alt:0;mso-border-alt:10px solid #FECD1C'>Acessar a plataforma</a></span><!--<![endif]--></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Precisa de alguma ajuda?</h1></td></tr></table></td></tr></table></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 class='p_name' style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>Telefone</h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='tel:+(000)123-456-789' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>+55 21 12345-6789</a></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='Margin:0;padding-bottom:15px;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone_1.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>E-mail<br></h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='mailto:contato@vaquinhaanimal.com.br' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>contato@vaquinhaanimal.com.br</a><br></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table class='es-footer' cellspacing='0' cellpadding='0' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' cellspacing='0' cellpadding='0' align='center' bgcolor='#ffffff' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-top:20px;padding-left:20px;padding-right:20px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:560px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Vaquinha Animal</h1></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='Margin:0;padding-top:20px;padding-bottom:20px;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:18px;color:#2E3440;font-size:12px'>© 2023 - Direitos Reservados - CNPJ: 48.173.612/0001-02 - Grupo Doadores Especiais</p></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table></td></tr></table></td></tr></table></td></tr></table></div></body></html>";

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

        private void SendEmailDoacaoRealizadaCartaoDonoCampanha(Doacao doacao, string donoCampanhaMail)
        {
            var status = "";

            if (doacao.Status == "paid")
            {
                status = "Pago";
            }
            else if (doacao.Status == "pending")
            {
                status = "Pendente";
            }
            else if (doacao.Status == "processing")
            {
                status = "Processando";
            }

            try
            {
                MailMessage message = new MailMessage("contato@vaquinhaanimal.com.br", donoCampanhaMail);
                message.Subject = "Doação Recebida - Vaquinha Animal";
                message.Body = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'><html xmlns='http://www.w3.org/1999/xhtml' xmlns:o='urn:schemas-microsoft-com:office:office' style='font-family:arial, 'helvetica neue', helvetica, sans-serif'><head><meta charset='UTF-8'><meta content='width=device-width, initial-scale=1' name='viewport'><meta name='x-apple-disable-message-reformatting'><meta http-equiv='X-UA-Compatible' content='IE=edge'><meta content='telephone=no' name='format-detection'><title>New Email</title><!--[if (mso 16)]><style type='text/css'> a {text-decoration: none;} </style><![endif]--><!--[if gte mso 9]><style>sup { font-size: 100% !important; }</style><![endif]--><!--[if gte mso 9]><xml> <o:OfficeDocumentSettings> <o:AllowPNG></o:AllowPNG> <o:PixelsPerInch>96</o:PixelsPerInch> </o:OfficeDocumentSettings> </xml><![endif]--><style type='text/css'>#outlook a { padding:0;}.es-button { mso-style-priority:100!important; text-decoration:none!important;}a[x-apple-data-detectors] { color:inherit!important; text-decoration:none!important; font-size:inherit!important; font-family:inherit!important; font-weight:inherit!important; line-height:inherit!important;}.es-desk-hidden { display:none; float:left; overflow:hidden; width:0; max-height:0; line-height:0; mso-hide:all;}[data-ogsb] .es-button.es-button-1 { padding:10px 35px!important;}@media only screen and (max-width:600px) {p, ul li, ol li, a { line-height:150%!important } h1, h2, h3, h1 a, h2 a, h3 a { line-height:120% } h1 { font-size:30px!important; text-align:center } h2 { font-size:24px!important; text-align:left } h3 { font-size:20px!important; text-align:left } .es-header-body h1 a, .es-content-body h1 a, .es-footer-body h1 a { font-size:30px!important; text-align:center } .es-header-body h2 a, .es-content-body h2 a, .es-footer-body h2 a { font-size:24px!important; text-align:left } .es-header-body h3 a, .es-content-body h3 a, .es-footer-body h3 a { font-size:20px!important; text-align:left } .es-menu td a { font-size:14px!important } .es-header-body p, .es-header-body ul li, .es-header-body ol li, .es-header-body a { font-size:14px!important } .es-content-body p, .es-content-body ul li, .es-content-body ol li, .es-content-body a { font-size:14px!important } .es-footer-body p, .es-footer-body ul li, .es-footer-body ol li, .es-footer-body a { font-size:14px!important } .es-infoblock p, .es-infoblock ul li, .es-infoblock ol li, .es-infoblock a { font-size:12px!important } *[class='gmail-fix'] { display:none!important } .es-m-txt-c, .es-m-txt-c h1, .es-m-txt-c h2, .es-m-txt-c h3 { text-align:center!important } .es-m-txt-r, .es-m-txt-r h1, .es-m-txt-r h2, .es-m-txt-r h3 { text-align:right!important } .es-m-txt-l, .es-m-txt-l h1, .es-m-txt-l h2, .es-m-txt-l h3 { text-align:left!important } .es-m-txt-r img, .es-m-txt-c img, .es-m-txt-l img { display:inline!important } .es-button-border { display:inline-block!important } a.es-button, button.es-button { font-size:18px!important; display:inline-block!important } .es-adaptive table, .es-left, .es-right { width:100%!important } .es-content table, .es-header table, .es-footer table, .es-content, .es-footer, .es-header { width:100%!important; max-width:600px!important } .es-adapt-td { display:block!important; width:100%!important } .adapt-img { width:100%!important; height:auto!important } .es-m-p0 { padding:0!important } .es-m-p0r { padding-right:0!important } .es-m-p0l { padding-left:0!important } .es-m-p0t { padding-top:0!important } .es-m-p0b { padding-bottom:0!important } .es-m-p20b { padding-bottom:20px!important } .es-mobile-hidden, .es-hidden { display:none!important } tr.es-desk-hidden, td.es-desk-hidden, table.es-desk-hidden { width:auto!important; overflow:visible!important; float:none!important; max-height:inherit!important; line-height:inherit!important } tr.es-desk-hidden { display:table-row!important } table.es-desk-hidden { display:table!important } td.es-desk-menu-hidden { display:table-cell!important } .es-menu td { width:1%!important } table.es-table-not-adapt, .esd-block-html table { width:auto!important } table.es-social { display:inline-block!important } table.es-social td { display:inline-block!important } .es-desk-hidden { display:table-row!important; width:auto!important; overflow:visible!important; max-height:inherit!important } .es-m-p5 { padding:5px!important } .es-m-p5t { padding-top:5px!important } .es-m-p5b { padding-bottom:5px!important } .es-m-p5r { padding-right:5px!important } .es-m-p5l { padding-left:5px!important } .es-m-p10 { padding:10px!important } .es-m-p10t { padding-top:10px!important } .es-m-p10b { padding-bottom:10px!important } .es-m-p10r { padding-right:10px!important } .es-m-p10l { padding-left:10px!important } .es-m-p15 { padding:15px!important } .es-m-p15t { padding-top:15px!important } .es-m-p15b { padding-bottom:15px!important } .es-m-p15r { padding-right:15px!important } .es-m-p15l { padding-left:15px!important } .es-m-p20 { padding:20px!important } .es-m-p20t { padding-top:20px!important } .es-m-p20r { padding-right:20px!important } .es-m-p20l { padding-left:20px!important } .es-m-p25 { padding:25px!important } .es-m-p25t { padding-top:25px!important } .es-m-p25b { padding-bottom:25px!important } .es-m-p25r { padding-right:25px!important } .es-m-p25l { padding-left:25px!important } .es-m-p30 { padding:30px!important } .es-m-p30t { padding-top:30px!important } .es-m-p30b { padding-bottom:30px!important } .es-m-p30r { padding-right:30px!important } .es-m-p30l { padding-left:30px!important } .es-m-p35 { padding:35px!important } .es-m-p35t { padding-top:35px!important } .es-m-p35b { padding-bottom:35px!important } .es-m-p35r { padding-right:35px!important } .es-m-p35l { padding-left:35px!important } .es-m-p40 { padding:40px!important } .es-m-p40t { padding-top:40px!important } .es-m-p40b { padding-bottom:40px!important } .es-m-p40r { padding-right:40px!important } .es-m-p40l { padding-left:40px!important } }</style></head><body data-new-gr-c-s-loaded='14.1082.0' style='width:100%;font-family:arial, 'helvetica neue', helvetica, sans-serif;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;padding:0;Margin:0'><div class='es-wrapper-color' style='background-color:#ECEFF4'><!--[if gte mso 9]><v:background xmlns:v='urn:schemas-microsoft-com:vml' fill='t'> <v:fill type='tile' color='#eceff4'></v:fill> </v:background><![endif]--><table class='es-wrapper' width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;padding:0;Margin:0;width:100%;height:100%;background-repeat:repeat;background-position:center top;background-color:#ECEFF4'><tr><td valign='top' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' class='es-header' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-header-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:337px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:337px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0;padding-top:10px;padding-bottom:10px;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:12px'><img src='https://www.vaquinhaanimal.com.br/assets/img/logo_fundo_claro.png' alt='Logo' style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' height='30' title='Logo'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:203px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:203px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:600px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#ffffff;width:600px' bgcolor='#ffffff'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:40px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px;text-align:justify'>Sua campanha recebeu uma doação. A forma de pagamento escolhido pelo doador foi CARTÃO DE CRÉDITO.</p></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px;text-align:justify'>Só lembrando que o valor só é compensado quando o status muda para PAGO e, só após isso, irá constar no relatório da campanha.</p></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'>Confira os detalhes da doação:</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><br></p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Data:</strong> " + doacao.Data + "</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Valor:</strong> R$ " + (doacao.Valor - doacao.ValorDestinadoPlataforma) + "</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Forma de Pagamento: </strong> Cartão de Crédito</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Status: </strong>" + status + "</p></td></tr><tr><td align='center' style='padding:15px;Margin:0'><!--[if mso]><a href='https://www.vaquinhaanimal.com.br' target='_blank' hidden> <v:roundrect xmlns:v='urn:schemas-microsoft-com:vml' xmlns:w='urn:schemas-microsoft-com:office:word' esdevVmlButton href='https://www.vaquinhaanimal.com.br' style='height:41px; v-text-anchor:middle; width:271px' arcsize='50%' stroke='f' fillcolor='#fecd1c'> <w:anchorlock></w:anchorlock> <center style='color:#2e3440; font-family:arial, 'helvetica neue', helvetica, sans-serif; font-size:15px; font-weight:700; line-height:15px; mso-text-raise:1px'>Acessar a plataforma</center> </v:roundrect></a><![endif]--><!--[if !mso]><!-- --><span class='msohide es-button-border' style='border-style:solid;border-color:#4C566A;background:#FECD1C;border-width:0px;display:inline-block;border-radius:30px;width:auto;mso-hide:all'><a href='https://www.vaquinhaanimal.com.br' class='es-button es-button-1' target='_blank' style='mso-style-priority:100 !important;text-decoration:none;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;color:#2E3440;font-size:18px;padding:10px 35px;display:inline-block;background:#FECD1C;border-radius:30px;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-weight:bold;font-style:normal;line-height:22px;width:auto;text-align:center;mso-padding-alt:0;mso-border-alt:10px solid #FECD1C'>Acessar a plataforma</a></span><!--<![endif]--></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Precisa de alguma ajuda?</h1></td></tr></table></td></tr></table></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 class='p_name' style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>Telefone</h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='tel:+(000)123-456-789' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>+55 21 12345-6789</a></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='Margin:0;padding-bottom:15px;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone_1.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>E-mail<br></h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='mailto:contato@vaquinhaanimal.com.br' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>contato@vaquinhaanimal.com.br</a><br></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table class='es-footer' cellspacing='0' cellpadding='0' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' cellspacing='0' cellpadding='0' align='center' bgcolor='#ffffff' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-top:20px;padding-left:20px;padding-right:20px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:560px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Vaquinha Animal</h1></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='Margin:0;padding-top:20px;padding-bottom:20px;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:18px;color:#2E3440;font-size:12px'>© 2023 - Direitos Reservados - CNPJ: 48.173.612/0001-02 - Grupo Doadores Especiais</p></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table></td></tr></table></td></tr></table></td></tr></table></div></body></html>";

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

        //private void SendEmailDoacaoRealizadaAssinaturaDoador(Doacao doacao, string name, string doadorEmail)
        //{
        //    var status = "";

        //    if (doacao.Status == "paid")
        //    {
        //        status = "Pago";
        //    }
        //    else if (doacao.Status == "pending")
        //    {
        //        status = "Pendente";
        //    }
        //    else if (doacao.Status == "processing")
        //    {
        //        status = "Processando";
        //    }

        //    try
        //    {
        //        MailMessage message = new MailMessage("contato@doadoresespeciais.com.br", doadorEmail);
        //        message.Subject = "Doação Realizada - Vaquinha Animal";
        //        message.Body = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'><html xmlns='http://www.w3.org/1999/xhtml' xmlns:o='urn:schemas-microsoft-com:office:office' style='font-family:arial, 'helvetica neue', helvetica, sans-serif'><head><meta charset='UTF-8'><meta content='width=device-width, initial-scale=1' name='viewport'><meta name='x-apple-disable-message-reformatting'><meta http-equiv='X-UA-Compatible' content='IE=edge'><meta content='telephone=no' name='format-detection'><title>New Email</title><!--[if (mso 16)]><style type='text/css'> a {text-decoration: none;} </style><![endif]--><!--[if gte mso 9]><style>sup { font-size: 100% !important; }</style><![endif]--><!--[if gte mso 9]><xml> <o:OfficeDocumentSettings> <o:AllowPNG></o:AllowPNG> <o:PixelsPerInch>96</o:PixelsPerInch> </o:OfficeDocumentSettings> </xml><![endif]--><style type='text/css'>#outlook a { padding:0;}.es-button { mso-style-priority:100!important; text-decoration:none!important;}a[x-apple-data-detectors] { color:inherit!important; text-decoration:none!important; font-size:inherit!important; font-family:inherit!important; font-weight:inherit!important; line-height:inherit!important;}.es-desk-hidden { display:none; float:left; overflow:hidden; width:0; max-height:0; line-height:0; mso-hide:all;}[data-ogsb] .es-button.es-button-1 { padding:10px 35px!important;}@media only screen and (max-width:600px) {p, ul li, ol li, a { line-height:150%!important } h1, h2, h3, h1 a, h2 a, h3 a { line-height:120% } h1 { font-size:30px!important; text-align:center } h2 { font-size:24px!important; text-align:left } h3 { font-size:20px!important; text-align:left } .es-header-body h1 a, .es-content-body h1 a, .es-footer-body h1 a { font-size:30px!important; text-align:center } .es-header-body h2 a, .es-content-body h2 a, .es-footer-body h2 a { font-size:24px!important; text-align:left } .es-header-body h3 a, .es-content-body h3 a, .es-footer-body h3 a { font-size:20px!important; text-align:left } .es-menu td a { font-size:14px!important } .es-header-body p, .es-header-body ul li, .es-header-body ol li, .es-header-body a { font-size:14px!important } .es-content-body p, .es-content-body ul li, .es-content-body ol li, .es-content-body a { font-size:14px!important } .es-footer-body p, .es-footer-body ul li, .es-footer-body ol li, .es-footer-body a { font-size:14px!important } .es-infoblock p, .es-infoblock ul li, .es-infoblock ol li, .es-infoblock a { font-size:12px!important } *[class='gmail-fix'] { display:none!important } .es-m-txt-c, .es-m-txt-c h1, .es-m-txt-c h2, .es-m-txt-c h3 { text-align:center!important } .es-m-txt-r, .es-m-txt-r h1, .es-m-txt-r h2, .es-m-txt-r h3 { text-align:right!important } .es-m-txt-l, .es-m-txt-l h1, .es-m-txt-l h2, .es-m-txt-l h3 { text-align:left!important } .es-m-txt-r img, .es-m-txt-c img, .es-m-txt-l img { display:inline!important } .es-button-border { display:inline-block!important } a.es-button, button.es-button { font-size:18px!important; display:inline-block!important } .es-adaptive table, .es-left, .es-right { width:100%!important } .es-content table, .es-header table, .es-footer table, .es-content, .es-footer, .es-header { width:100%!important; max-width:600px!important } .es-adapt-td { display:block!important; width:100%!important } .adapt-img { width:100%!important; height:auto!important } .es-m-p0 { padding:0!important } .es-m-p0r { padding-right:0!important } .es-m-p0l { padding-left:0!important } .es-m-p0t { padding-top:0!important } .es-m-p0b { padding-bottom:0!important } .es-m-p20b { padding-bottom:20px!important } .es-mobile-hidden, .es-hidden { display:none!important } tr.es-desk-hidden, td.es-desk-hidden, table.es-desk-hidden { width:auto!important; overflow:visible!important; float:none!important; max-height:inherit!important; line-height:inherit!important } tr.es-desk-hidden { display:table-row!important } table.es-desk-hidden { display:table!important } td.es-desk-menu-hidden { display:table-cell!important } .es-menu td { width:1%!important } table.es-table-not-adapt, .esd-block-html table { width:auto!important } table.es-social { display:inline-block!important } table.es-social td { display:inline-block!important } .es-desk-hidden { display:table-row!important; width:auto!important; overflow:visible!important; max-height:inherit!important } .es-m-p5 { padding:5px!important } .es-m-p5t { padding-top:5px!important } .es-m-p5b { padding-bottom:5px!important } .es-m-p5r { padding-right:5px!important } .es-m-p5l { padding-left:5px!important } .es-m-p10 { padding:10px!important } .es-m-p10t { padding-top:10px!important } .es-m-p10b { padding-bottom:10px!important } .es-m-p10r { padding-right:10px!important } .es-m-p10l { padding-left:10px!important } .es-m-p15 { padding:15px!important } .es-m-p15t { padding-top:15px!important } .es-m-p15b { padding-bottom:15px!important } .es-m-p15r { padding-right:15px!important } .es-m-p15l { padding-left:15px!important } .es-m-p20 { padding:20px!important } .es-m-p20t { padding-top:20px!important } .es-m-p20r { padding-right:20px!important } .es-m-p20l { padding-left:20px!important } .es-m-p25 { padding:25px!important } .es-m-p25t { padding-top:25px!important } .es-m-p25b { padding-bottom:25px!important } .es-m-p25r { padding-right:25px!important } .es-m-p25l { padding-left:25px!important } .es-m-p30 { padding:30px!important } .es-m-p30t { padding-top:30px!important } .es-m-p30b { padding-bottom:30px!important } .es-m-p30r { padding-right:30px!important } .es-m-p30l { padding-left:30px!important } .es-m-p35 { padding:35px!important } .es-m-p35t { padding-top:35px!important } .es-m-p35b { padding-bottom:35px!important } .es-m-p35r { padding-right:35px!important } .es-m-p35l { padding-left:35px!important } .es-m-p40 { padding:40px!important } .es-m-p40t { padding-top:40px!important } .es-m-p40b { padding-bottom:40px!important } .es-m-p40r { padding-right:40px!important } .es-m-p40l { padding-left:40px!important } }</style></head><body data-new-gr-c-s-loaded='14.1082.0' style='width:100%;font-family:arial, 'helvetica neue', helvetica, sans-serif;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;padding:0;Margin:0'><div class='es-wrapper-color' style='background-color:#ECEFF4'><!--[if gte mso 9]><v:background xmlns:v='urn:schemas-microsoft-com:vml' fill='t'> <v:fill type='tile' color='#eceff4'></v:fill> </v:background><![endif]--><table class='es-wrapper' width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;padding:0;Margin:0;width:100%;height:100%;background-repeat:repeat;background-position:center top;background-color:#ECEFF4'><tr><td valign='top' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' class='es-header' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-header-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:337px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:337px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0;padding-top:10px;padding-bottom:10px;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:12px'><img src='https://www.vaquinhaanimal.com.br/assets/img/logo_fundo_claro.png' alt='Logo' style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' height='30' title='Logo'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:203px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:203px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:600px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;position:relative'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:12px'><img class='adapt-img' src='https://i.ibb.co/CJD2fQt/image16857378637056256-2.png' alt title width='600' style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic'></a></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#ffffff;width:600px' bgcolor='#ffffff'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:40px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px;text-align:justify'>Olá, " + name + "<br><br>Muito obrigado por ter doado para a Vaquinha Animal. Acompanhe o Status do seu pagamento.</p></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'>Confira os detalhes da sua doação:</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><br></p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Data:</strong> " + doacao.Data + "</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Valor:</strong> R$ " + doacao.Valor + "</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Forma de Pagamento: </strong> Cartão de Crédito</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Status: </strong>" + status + "</p></td></tr><tr><td align='center' style='padding:15px;Margin:0'><!--[if mso]><a href='https://www.vaquinhaanimal.com.br' target='_blank' hidden> <v:roundrect xmlns:v='urn:schemas-microsoft-com:vml' xmlns:w='urn:schemas-microsoft-com:office:word' esdevVmlButton href='https://www.vaquinhaanimal.com.br' style='height:41px; v-text-anchor:middle; width:271px' arcsize='50%' stroke='f' fillcolor='#fecd1c'> <w:anchorlock></w:anchorlock> <center style='color:#2e3440; font-family:arial, 'helvetica neue', helvetica, sans-serif; font-size:15px; font-weight:700; line-height:15px; mso-text-raise:1px'>Acessar a plataforma</center> </v:roundrect></a><![endif]--><!--[if !mso]><!-- --><span class='msohide es-button-border' style='border-style:solid;border-color:#4C566A;background:#FECD1C;border-width:0px;display:inline-block;border-radius:30px;width:auto;mso-hide:all'><a href='https://www.vaquinhaanimal.com.br' class='es-button es-button-1' target='_blank' style='mso-style-priority:100 !important;text-decoration:none;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;color:#2E3440;font-size:18px;padding:10px 35px;display:inline-block;background:#FECD1C;border-radius:30px;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-weight:bold;font-style:normal;line-height:22px;width:auto;text-align:center;mso-padding-alt:0;mso-border-alt:10px solid #FECD1C'>Acessar a plataforma</a></span><!--<![endif]--></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Precisa de alguma ajuda?</h1></td></tr></table></td></tr></table></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 class='p_name' style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>Telefone</h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='tel:+(000)123-456-789' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>+55 21 12345-6789</a></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='Margin:0;padding-bottom:15px;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone_1.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>E-mail<br></h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='mailto:contato@vaquinhaanimal.com.br' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>contato@vaquinhaanimal.com.br</a><br></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table class='es-footer' cellspacing='0' cellpadding='0' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' cellspacing='0' cellpadding='0' align='center' bgcolor='#ffffff' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-top:20px;padding-left:20px;padding-right:20px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:560px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Vaquinha Animal</h1></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='Margin:0;padding-top:20px;padding-bottom:20px;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:18px;color:#2E3440;font-size:12px'>© 2023 - Direitos Reservados - CNPJ: 48.173.612/0001-02 - Grupo Doadores Especiais</p></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table></td></tr></table></td></tr></table></td></tr></table></div></body></html>";

        //        MailAddress copy = new MailAddress("contato@doadoresespeciais.com.br");
        //        message.CC.Add(copy);
        //        SmtpClient smtp = new SmtpClient();
        //        smtp.Host = "smtp.zoho.com";
        //        smtp.Port = 587;
        //        smtp.Credentials = new NetworkCredential("contato@doadoresespeciais.com.br", "Vasco10@");
        //        smtp.EnableSsl = true;

        //        message.IsBodyHtml = true;
        //        message.Priority = MailPriority.Normal;
        //        smtp.Send(message);
        //    }

        //    catch (Exception ex)
        //    {
        //        throw new Exception(ex.ToString());
        //    }

        //}

        //private void SendEmailDoacaoRealizadaAssinaturaDonoCampanha(Doacao doacao, string donoCampanhaMail)
        //{
        //    var status = "";

        //    if (doacao.Status == "paid")
        //    {
        //        status = "Pago";
        //    }
        //    else if (doacao.Status == "pending")
        //    {
        //        status = "Pendente";
        //    }
        //    else if (doacao.Status == "processing")
        //    {
        //        status = "Processando";
        //    }

        //    try
        //    {
        //        MailMessage message = new MailMessage("contato@doadoresespeciais.com.br", donoCampanhaMail);
        //        message.Subject = "Doação Recebida - Vaquinha Animal";
        //        message.Body = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'><html xmlns='http://www.w3.org/1999/xhtml' xmlns:o='urn:schemas-microsoft-com:office:office' style='font-family:arial, 'helvetica neue', helvetica, sans-serif'><head><meta charset='UTF-8'><meta content='width=device-width, initial-scale=1' name='viewport'><meta name='x-apple-disable-message-reformatting'><meta http-equiv='X-UA-Compatible' content='IE=edge'><meta content='telephone=no' name='format-detection'><title>New Email</title><!--[if (mso 16)]><style type='text/css'> a {text-decoration: none;} </style><![endif]--><!--[if gte mso 9]><style>sup { font-size: 100% !important; }</style><![endif]--><!--[if gte mso 9]><xml> <o:OfficeDocumentSettings> <o:AllowPNG></o:AllowPNG> <o:PixelsPerInch>96</o:PixelsPerInch> </o:OfficeDocumentSettings> </xml><![endif]--><style type='text/css'>#outlook a { padding:0;}.es-button { mso-style-priority:100!important; text-decoration:none!important;}a[x-apple-data-detectors] { color:inherit!important; text-decoration:none!important; font-size:inherit!important; font-family:inherit!important; font-weight:inherit!important; line-height:inherit!important;}.es-desk-hidden { display:none; float:left; overflow:hidden; width:0; max-height:0; line-height:0; mso-hide:all;}[data-ogsb] .es-button.es-button-1 { padding:10px 35px!important;}@media only screen and (max-width:600px) {p, ul li, ol li, a { line-height:150%!important } h1, h2, h3, h1 a, h2 a, h3 a { line-height:120% } h1 { font-size:30px!important; text-align:center } h2 { font-size:24px!important; text-align:left } h3 { font-size:20px!important; text-align:left } .es-header-body h1 a, .es-content-body h1 a, .es-footer-body h1 a { font-size:30px!important; text-align:center } .es-header-body h2 a, .es-content-body h2 a, .es-footer-body h2 a { font-size:24px!important; text-align:left } .es-header-body h3 a, .es-content-body h3 a, .es-footer-body h3 a { font-size:20px!important; text-align:left } .es-menu td a { font-size:14px!important } .es-header-body p, .es-header-body ul li, .es-header-body ol li, .es-header-body a { font-size:14px!important } .es-content-body p, .es-content-body ul li, .es-content-body ol li, .es-content-body a { font-size:14px!important } .es-footer-body p, .es-footer-body ul li, .es-footer-body ol li, .es-footer-body a { font-size:14px!important } .es-infoblock p, .es-infoblock ul li, .es-infoblock ol li, .es-infoblock a { font-size:12px!important } *[class='gmail-fix'] { display:none!important } .es-m-txt-c, .es-m-txt-c h1, .es-m-txt-c h2, .es-m-txt-c h3 { text-align:center!important } .es-m-txt-r, .es-m-txt-r h1, .es-m-txt-r h2, .es-m-txt-r h3 { text-align:right!important } .es-m-txt-l, .es-m-txt-l h1, .es-m-txt-l h2, .es-m-txt-l h3 { text-align:left!important } .es-m-txt-r img, .es-m-txt-c img, .es-m-txt-l img { display:inline!important } .es-button-border { display:inline-block!important } a.es-button, button.es-button { font-size:18px!important; display:inline-block!important } .es-adaptive table, .es-left, .es-right { width:100%!important } .es-content table, .es-header table, .es-footer table, .es-content, .es-footer, .es-header { width:100%!important; max-width:600px!important } .es-adapt-td { display:block!important; width:100%!important } .adapt-img { width:100%!important; height:auto!important } .es-m-p0 { padding:0!important } .es-m-p0r { padding-right:0!important } .es-m-p0l { padding-left:0!important } .es-m-p0t { padding-top:0!important } .es-m-p0b { padding-bottom:0!important } .es-m-p20b { padding-bottom:20px!important } .es-mobile-hidden, .es-hidden { display:none!important } tr.es-desk-hidden, td.es-desk-hidden, table.es-desk-hidden { width:auto!important; overflow:visible!important; float:none!important; max-height:inherit!important; line-height:inherit!important } tr.es-desk-hidden { display:table-row!important } table.es-desk-hidden { display:table!important } td.es-desk-menu-hidden { display:table-cell!important } .es-menu td { width:1%!important } table.es-table-not-adapt, .esd-block-html table { width:auto!important } table.es-social { display:inline-block!important } table.es-social td { display:inline-block!important } .es-desk-hidden { display:table-row!important; width:auto!important; overflow:visible!important; max-height:inherit!important } .es-m-p5 { padding:5px!important } .es-m-p5t { padding-top:5px!important } .es-m-p5b { padding-bottom:5px!important } .es-m-p5r { padding-right:5px!important } .es-m-p5l { padding-left:5px!important } .es-m-p10 { padding:10px!important } .es-m-p10t { padding-top:10px!important } .es-m-p10b { padding-bottom:10px!important } .es-m-p10r { padding-right:10px!important } .es-m-p10l { padding-left:10px!important } .es-m-p15 { padding:15px!important } .es-m-p15t { padding-top:15px!important } .es-m-p15b { padding-bottom:15px!important } .es-m-p15r { padding-right:15px!important } .es-m-p15l { padding-left:15px!important } .es-m-p20 { padding:20px!important } .es-m-p20t { padding-top:20px!important } .es-m-p20r { padding-right:20px!important } .es-m-p20l { padding-left:20px!important } .es-m-p25 { padding:25px!important } .es-m-p25t { padding-top:25px!important } .es-m-p25b { padding-bottom:25px!important } .es-m-p25r { padding-right:25px!important } .es-m-p25l { padding-left:25px!important } .es-m-p30 { padding:30px!important } .es-m-p30t { padding-top:30px!important } .es-m-p30b { padding-bottom:30px!important } .es-m-p30r { padding-right:30px!important } .es-m-p30l { padding-left:30px!important } .es-m-p35 { padding:35px!important } .es-m-p35t { padding-top:35px!important } .es-m-p35b { padding-bottom:35px!important } .es-m-p35r { padding-right:35px!important } .es-m-p35l { padding-left:35px!important } .es-m-p40 { padding:40px!important } .es-m-p40t { padding-top:40px!important } .es-m-p40b { padding-bottom:40px!important } .es-m-p40r { padding-right:40px!important } .es-m-p40l { padding-left:40px!important } }</style></head><body data-new-gr-c-s-loaded='14.1082.0' style='width:100%;font-family:arial, 'helvetica neue', helvetica, sans-serif;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;padding:0;Margin:0'><div class='es-wrapper-color' style='background-color:#ECEFF4'><!--[if gte mso 9]><v:background xmlns:v='urn:schemas-microsoft-com:vml' fill='t'> <v:fill type='tile' color='#eceff4'></v:fill> </v:background><![endif]--><table class='es-wrapper' width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;padding:0;Margin:0;width:100%;height:100%;background-repeat:repeat;background-position:center top;background-color:#ECEFF4'><tr><td valign='top' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' class='es-header' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-header-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:337px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:337px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0;padding-top:10px;padding-bottom:10px;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:12px'><img src='https://www.vaquinhaanimal.com.br/assets/img/logo_fundo_claro.png' alt='Logo' style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' height='30' title='Logo'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:203px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:203px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='padding:0;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:600px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;position:relative'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:12px'><img class='adapt-img' src='https://i.ibb.co/CJD2fQt/image16857378637056256-2.png' alt title width='600' style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic'></a></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#ffffff;width:600px' bgcolor='#ffffff'><tr><td align='left' style='Margin:0;padding-left:20px;padding-right:20px;padding-top:30px;padding-bottom:40px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px;text-align:justify'>Sua campanha recebeu uma doação. A forma de pagamento escolhido pelo doador foi CARTÃO DE CRÉDITO.</p></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px;text-align:justify'>Só lembrando que o valor só é compensado quando o status muda para PAGO e, só após isso, irá constar no relatório da campanha.</p></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-bottom:20px'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'>Confira os detalhes da doação:</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><br></p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Data:</strong> " + doacao.Data + "</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Valor:</strong> R$ " + doacao.Valor + "</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Forma de Pagamento: </strong> Cartão de Crédito</p><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><strong>Status: </strong>" + status + "</p></td></tr><tr><td align='center' style='padding:15px;Margin:0'><!--[if mso]><a href='https://www.vaquinhaanimal.com.br' target='_blank' hidden> <v:roundrect xmlns:v='urn:schemas-microsoft-com:vml' xmlns:w='urn:schemas-microsoft-com:office:word' esdevVmlButton href='https://www.vaquinhaanimal.com.br' style='height:41px; v-text-anchor:middle; width:271px' arcsize='50%' stroke='f' fillcolor='#fecd1c'> <w:anchorlock></w:anchorlock> <center style='color:#2e3440; font-family:arial, 'helvetica neue', helvetica, sans-serif; font-size:15px; font-weight:700; line-height:15px; mso-text-raise:1px'>Acessar a plataforma</center> </v:roundrect></a><![endif]--><!--[if !mso]><!-- --><span class='msohide es-button-border' style='border-style:solid;border-color:#4C566A;background:#FECD1C;border-width:0px;display:inline-block;border-radius:30px;width:auto;mso-hide:all'><a href='https://www.vaquinhaanimal.com.br' class='es-button es-button-1' target='_blank' style='mso-style-priority:100 !important;text-decoration:none;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;color:#2E3440;font-size:18px;padding:10px 35px;display:inline-block;background:#FECD1C;border-radius:30px;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-weight:bold;font-style:normal;line-height:22px;width:auto;text-align:center;mso-padding-alt:0;mso-border-alt:10px solid #FECD1C'>Acessar a plataforma</a></span><!--<![endif]--></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r' align='center' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Precisa de alguma ajuda?</h1></td></tr></table></td></tr></table></td></tr><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 class='p_name' style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>Telefone</h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='tel:+(000)123-456-789' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>+55 21 12345-6789</a></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><tr><td align='left' style='Margin:0;padding-bottom:15px;padding-left:20px;padding-right:20px;padding-top:30px'><!--[if mso]><table style='width:560px' cellpadding='0' cellspacing='0'><tr><td style='width:100px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' align='left' class='es-left' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left'><tr><td class='es-m-p20b' align='center' valign='top' style='padding:0;Margin:0;width:100px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:separate;border-spacing:0px;border-radius:10px'><tr><td align='center' class='es-m-txt-l' style='padding:0;Margin:0;font-size:0px'><a target='_blank' href='https://viewstripo.email' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'><img src='https://mvfpsk.stripocdn.email/content/guids/CABINET_b9f9398148f621b5f62ddbec551be81b/images/phone_1.png' alt style='display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic' width='100'></a></td></tr></table></td></tr></table><!--[if mso]></td><td style='width:20px'></td><td style='width:440px' valign='top'><![endif]--><table cellpadding='0' cellspacing='0' class='es-right' align='right' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right'><tr><td align='left' style='padding:0;Margin:0;width:440px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;padding-top:5px;padding-bottom:25px'><h2 style='Margin:0;line-height:29px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:24px;font-style:normal;font-weight:bold;color:#2E3440'>E-mail<br></h2></td></tr><tr><td align='left' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:21px;color:#2E3440;font-size:14px'><a target='_blank' href='mailto:contato@vaquinhaanimal.com.br' style='-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;text-decoration:none;color:#2E3440;font-size:14px'>contato@vaquinhaanimal.com.br</a><br></p></td></tr></table></td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table class='es-footer' cellspacing='0' cellpadding='0' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' cellspacing='0' cellpadding='0' align='center' bgcolor='#ffffff' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-top:20px;padding-left:20px;padding-right:20px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td class='es-m-p0r es-m-p20b' valign='top' align='center' style='padding:0;Margin:0;width:560px'><table width='100%' cellspacing='0' cellpadding='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;padding-bottom:10px;padding-top:20px'><h1 style='Margin:0;line-height:48px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:40px;font-style:normal;font-weight:bold;color:#2E3440'>Vaquinha Animal</h1></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table bgcolor='#ffffff' class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#D8DEE9;width:600px'><tr><td align='left' style='Margin:0;padding-top:20px;padding-bottom:20px;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' class='es-m-txt-c' style='padding:0;Margin:0'><p style='Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;line-height:18px;color:#2E3440;font-size:12px'>© 2023 - Direitos Reservados - CNPJ: 48.173.612/0001-02 - Grupo Doadores Especiais</p></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-content' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%'><tr><td align='center' style='padding:0;Margin:0'><table class='es-content-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:0;Margin:0;padding-left:20px;padding-right:20px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' valign='top' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' height='40' style='padding:0;Margin:0'></td></tr></table></td></tr></table></td></tr></table></td></tr></table><table cellpadding='0' cellspacing='0' class='es-footer' align='center' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top'><tr><td align='center' style='padding:0;Margin:0'><table class='es-footer-body' align='center' cellpadding='0' cellspacing='0' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:transparent;width:600px'><tr><td align='left' style='padding:20px;Margin:0'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='left' style='padding:0;Margin:0;width:560px'><table cellpadding='0' cellspacing='0' width='100%' style='mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px'><tr><td align='center' style='padding:0;Margin:0;display:none'></td></tr></table></td></tr></table></td></tr></table></td></tr></table></td></tr></table></div></body></html>";

        //        SmtpClient smtp = new SmtpClient();
        //        smtp.Host = "smtp.zoho.com";
        //        smtp.Port = 587;
        //        smtp.Credentials = new NetworkCredential("contato@doadoresespeciais.com.br", "Vasco10@");
        //        smtp.EnableSsl = true;

        //        message.IsBodyHtml = true;
        //        message.Priority = MailPriority.Normal;
        //        smtp.Send(message);
        //    }

        //    catch (Exception ex)
        //    {
        //        throw new Exception(ex.ToString());
        //    }

        //}
        #endregion

        #region UPLAND TESTES
        [HttpPost("webhook")]
        public ActionResult WebhookTesteUpland(object response)
        {
            var obj = response;
            return Ok();
        }
        #endregion
    }
}