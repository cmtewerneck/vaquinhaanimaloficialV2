using AutoMapper;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using VaquinhaAnimal.Api.Controllers;
using VaquinhaAnimal.Api.ViewModels;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Entities.Pagarme;
using VaquinhaAnimal.Domain.Interfaces;

namespace VaquinhaAnimal.App.V1.Controllers
{
    [Authorize]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/doacoes")]
    public class DoacoesController : MainController
    {
        #region VARIABLES
        private readonly IDoacaoRepository _doacaoRepository;
        private readonly IDoacaoService _doacaoService;
        private readonly IMapper _mapper;
        private readonly IIdentityRepository _identityRepository;
        private readonly IUser _user;
        static BaseFont fonteBase = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false);
        #endregion

        #region CONSTRUCTOR
        public DoacoesController(IDoacaoRepository doacaoRepository,
                                 IDoacaoService doacaoService,
                                 IIdentityRepository identityRepository,
                                 IMapper mapper,
                                 IConfiguration configuration,
                                 INotificador notificador, IUser user) : base(notificador, user, configuration)
        {
            _doacaoRepository = doacaoRepository;
            _mapper = mapper;
            _user = user;
            _identityRepository = identityRepository;
            _doacaoService = doacaoService;
        }
        #endregion

        #region CRUD
        [HttpPost]
        public async Task<ActionResult<PagarmePedido>> Adicionar(PagarmePedido pedido)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            foreach (var item in pedido.items)
            {
                item.code = "1";
            }

            try
            {
                //Set Basic Auth
                //var userPagarme = test_key;
                //var password = "";
                //var base64String = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userPagarme}:{password}"));
                //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64String);
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                //foreach (var item in pedido.items)
                //{
                //    itensPedido.Add(new PagarmePedidoItens { 
                //        amount = item.amount,
                //        description = item.description,
                //        quantity = item.quantity
                //    });
                //}

                //foreach (var pagamento in pedido.payments)
                //{
                //    pagamentos.Add(new PagarmePedidoPagamentos
                //    {
                //        payment_method = pagamento.payment_method,
                //        credit_card = new PagarmePedidoCartaoCredito
                //        {
                //            recurrence = pagamento.credit_card.recurrence,
                //            installments = pagamento.credit_card.installments,
                //            statement_descriptor = pagamento.credit_card.statement_descriptor,
                //            card = new PagarmePedidoCartaoCreditoUsado
                //            {
                //                number = pagamento.credit_card.card.number,
                //                cvv = pagamento.credit_card.card.cvv,
                //                exp_month = pagamento.credit_card.card.exp_month,
                //                exp_year = pagamento.credit_card.card.exp_year,
                //                holder_name = pagamento.credit_card.card.holder_name,
                //                billing_address = new PagarmePedidoBillingAddress
                //                {
                //                    line_1 = pagamento.credit_card.card.billing_address.line_1,
                //                    city = pagamento.credit_card.card.billing_address.city,
                //                    country = pagamento.credit_card.card.billing_address.country,
                //                    state = pagamento.credit_card.card.billing_address.state,
                //                    zip_code = pagamento.credit_card.card.billing_address.zip_code
                //                }
                //            }
                //        }
                //    });
                //}

                var usuarioId = _user.GetUserId(); // Pega o ID do usuário
                var idPagarme = _identityRepository.GetCodigoPagarme(usuarioId.ToString()); // Pega o ID da Pagarme do usuário

                //var pedidoToAdd = new PagarmePedido()
                //{
                //    customer_id = idPagarme,
                //    items = itensPedido,
                //    payments = pagamentos
                //};                

                HttpResponseMessage response = await client.PostAsJsonAsync(urlPagarme + "orders/", pedido);

                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();

                //var pedidoRecebido = JsonConvert.DeserializeObject<PagarmeCardResponse>(responseBody);

                // APÓS ADICIONADO NA PAGARME, ADICIONANDO PEDIDO NO BANCO DE DADOS
                //var cartaoAdd = new Cartao
                //{
                //    Customer_Id = idPagarme,
                //    Card_Id = cartaoRecebido.Id,
                //    First_Six_Digits = cartaoRecebido.First_Six_Digits,
                //    Last_Four_Digits = cartaoRecebido.Last_Four_Digits,
                //    Exp_Month = cartaoRecebido.Exp_Month,
                //    Exp_Year = cartaoRecebido.Exp_Year,
                //    Status = cartaoRecebido.Status
                //};

                //await _cartaoService.Adicionar(cartaoAdd);

                return Ok(responseBody);
            }
            catch (HttpRequestException e)
            {
                return BadRequest("Erro: " + e);
            }

            //await _doacaoService.Adicionar(_mapper.Map<Doacao>(doacaoViewModel));

            //return CustomResponse(doacaoViewModel);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<DoacaoViewModel>> Atualizar(Guid id, DoacaoViewModel doacaoViewModel)
        {
            if (id != doacaoViewModel.id)
            {
                NotificarErro("Os ids informados não são iguais!");
                return CustomResponse();
            }

            var doacaoAtualizacao = await ObterDoacao(id);

            if (!ModelState.IsValid) return CustomResponse(ModelState);

            doacaoAtualizacao.data = doacaoViewModel.data;
            doacaoAtualizacao.valor = doacaoViewModel.valor;
            doacaoAtualizacao.customer_id = doacaoViewModel.customer_id;
            doacaoAtualizacao.transacao_id = doacaoViewModel.transacao_id;
            doacaoAtualizacao.usuario_id = doacaoViewModel.usuario_id;
            doacaoAtualizacao.forma_pagamento = doacaoViewModel.forma_pagamento;
            doacaoAtualizacao.status = doacaoViewModel.status;
            doacaoAtualizacao.campanha_id = doacaoViewModel.campanha_id;
            doacaoAtualizacao.charge_id = doacaoViewModel.charge_id;

            await _doacaoService.Atualizar(_mapper.Map<Doacao>(doacaoAtualizacao));

            return CustomResponse(doacaoViewModel);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<DoacaoViewModel>> Excluir(Guid id)
        {
            var doacao = await ObterDoacao(id);

            if (doacao == null)
            {
                NotificarErro("O id da doação não foi encontrado.");
                return CustomResponse(doacao);
            }

            await _doacaoService.Remover(id);

            return CustomResponse(doacao);
        }
        #endregion

        #region METHODS
        [HttpGet]
        public async Task<List<DoacaoViewModel>> ObterTodos()
        {
            return _mapper.Map<List<DoacaoViewModel>>(await _doacaoRepository.GetAllAsync());
        }

        [AllowAnonymous]
        [HttpGet("total-doadores/{campanhaId:guid}")]
        public async Task<int> ObterTotalDoadoresPorCampanha(Guid campanhaId)
        {
            var result = await _doacaoRepository.ObterTotalDoadoresPorCampanha(campanhaId);

            return result;
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<DoacaoViewModel>> ObterDoacaoPorId(Guid id)
        {
            var doacao = _mapper.Map<DoacaoViewModel>(await _doacaoRepository.GetByIdAsync(id));

            if (doacao == null) return NotFound();

            return doacao;
        }

        private async Task<DoacaoViewModel> ObterDoacao(Guid id)
        {
            return _mapper.Map<DoacaoViewModel>(await _doacaoRepository.GetByIdAsync(id));
        }

        [HttpGet("minhas-doacoes")]
        public async Task<List<DoacaoViewModel>> ObterMinhasDoacoes()
        {
            var usuarioLogadoId = _user.GetUserId();
            var doacoesToReturn = await _doacaoRepository.GetAllMyDonationsAsync(usuarioLogadoId);
            return _mapper.Map<List<DoacaoViewModel>>(doacoesToReturn);
        }

        [HttpGet("minhas-doacoes-paginado/{PageSize:int}/{PageNumber:int}")]
        public async Task<ActionResult> ObterMinhasDoacoesPaginado(int PageSize, int PageNumber)
        {
            var userId = _user.GetUserId();

            var result = await _doacaoRepository.ListMyDonationsAsync(PageSize, PageNumber, userId);

            return Ok(result);
        }

        [HttpGet("relatorio-pdf/{campanhaId:guid}")]
        public async Task<ActionResult> GerarRelatórioPdf(Guid campanhaId)
        {
            // PEGAR DOACOES DA CAMPANHA ENVIADA
            var doacoes = await _doacaoRepository.ObterDoacoesDaCampanha(campanhaId);

            if (doacoes.Count <= 0)
            {
                NotificarErro("Doações não encontrada ou inexistentes");
                return CustomResponse();
            }

            // CONFIGURAÇÃO DO DOCUMENTO
            var pxPorMm = 72 / 25.2F;
            var pdf = new Document(PageSize.A4.Rotate(), 15 * pxPorMm, 15 * pxPorMm, 15 * pxPorMm, 20 * pxPorMm);
            var path = $"relatorio_vaquinha_animal.{DateTime.Now.ToString("dd.MM.yyyy.HH.mm")}.pdf";
            var arquivo = new FileStream(path, FileMode.Create);
            var writer = PdfWriter.GetInstance(pdf, arquivo);
            pdf.Open();

            // ADICIONANDO TÍTULO
            var fonteTitulo = new Font(fonteBase, 28, Font.NORMAL, BaseColor.Black);
            var fonteNomeCampanha = new Font(fonteBase, 24, Font.NORMAL, BaseColor.Black);
            //var titulo = new Paragraph("Relatório de Campanha: " + doacoes[0].Campanha.Titulo + " \n\n", fonteTitulo);
            var titulo = new Paragraph("Relatório de Campanha \n\n", fonteTitulo);
            var nomeCampanha = new Paragraph(doacoes[0].Campanha.Titulo, fonteNomeCampanha);
            titulo.Alignment = Element.ALIGN_LEFT;
            titulo.SpacingAfter = 10;
            nomeCampanha.Alignment = Element.ALIGN_LEFT;
            nomeCampanha.SpacingAfter = 10;
            pdf.Add(titulo);
            pdf.Add(nomeCampanha);

            // ADICIONANDO A LOGOMARCA
            var pathImagem = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/logomarca.png");
            if (System.IO.File.Exists(pathImagem))
            {
                Image logo = Image.GetInstance(pathImagem);
                float razaoLarguraAltura = logo.Width / logo.Height;
                float alturaLogo = 60;
                float larguraLogo = alturaLogo * razaoLarguraAltura;
                logo.ScaleToFit(larguraLogo, alturaLogo);
                var margemEsquerda = pdf.PageSize.Width - pdf.RightMargin - larguraLogo;
                var margemTopo = pdf.PageSize.Height - pdf.TopMargin - 54;
                logo.SetAbsolutePosition(margemEsquerda, margemTopo);
                writer.DirectContent.AddImage(logo, false);
            }

            // ADICIONANDO A TABELA
            var tabela = new PdfPTable(7);
            float[] largurasColunas = { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 2.0f, 2.0f };
            tabela.SetWidths(largurasColunas);
            tabela.DefaultCell.BorderWidth = 0;
            tabela.WidthPercentage = 100;

            // ADICIONAR TÍTULOS
            CriarCelulaTexto(tabela, "Data", PdfPCell.ALIGN_LEFT, true);
            CriarCelulaTexto(tabela, "Valor Doado", PdfPCell.ALIGN_LEFT, true);
            CriarCelulaTexto(tabela, "Valor Plataforma", PdfPCell.ALIGN_LEFT, true);
            CriarCelulaTexto(tabela, "Taxas", PdfPCell.ALIGN_LEFT, true);
            CriarCelulaTexto(tabela, "Valor Final", PdfPCell.ALIGN_LEFT, true);
            CriarCelulaTexto(tabela, "Forma de Pagamento", PdfPCell.ALIGN_LEFT, true);
            CriarCelulaTexto(tabela, "ID da Transação", PdfPCell.ALIGN_LEFT, true);

            foreach (var doacao in doacoes)
            {
                CriarCelulaTexto(tabela, doacao.Data.ToString("dd/MM/yyyy"), PdfPCell.ALIGN_LEFT);
                CriarCelulaTexto(tabela, "R$ " + doacao.Valor.ToString(), PdfPCell.ALIGN_LEFT);
                CriarCelulaTexto(tabela, "R$ " + doacao.ValorPlataforma.ToString(), PdfPCell.ALIGN_LEFT);
                CriarCelulaTexto(tabela, "R$ " + doacao.ValorTaxa.ToString(), PdfPCell.ALIGN_LEFT);
                CriarCelulaTexto(tabela, "R$ " + doacao.ValorBeneficiario.ToString(), PdfPCell.ALIGN_LEFT);

                if (doacao.FormaPagamento == "billing")
                {
                    CriarCelulaTexto(tabela, "Boleto", PdfPCell.ALIGN_LEFT);
                }
                else if (doacao.FormaPagamento == "pix")
                {
                    CriarCelulaTexto(tabela, "PIX", PdfPCell.ALIGN_LEFT);
                }
                else if (doacao.FormaPagamento == "credit_card")
                {
                    CriarCelulaTexto(tabela, "Cartão de Crédito", PdfPCell.ALIGN_LEFT);
                }

                CriarCelulaTexto(tabela, doacao.Transacao_Id, PdfPCell.ALIGN_LEFT);
            }

            pdf.Add(tabela);

            //ADICIONAR CNPJ
            var fonteDireitosReservados = new Font(fonteBase, 9, Font.NORMAL, BaseColor.Black);
            Paragraph copyright = new Paragraph("© 2023 Direitos Reservados - Vaquinha Animal - CNPJ: 48.173.612/0001-02 - Grupo Doadores Especiais.", fonteDireitosReservados);
            PdfPTable footerTbl = new PdfPTable(1);
            footerTbl.TotalWidth = 500;
            PdfPCell cell = new PdfPCell(copyright);
            cell.Border = 1;
            footerTbl.AddCell(cell);
            footerTbl.WriteSelectedRows(0, -1, 30, 30, writer.DirectContent);

            // FECHANDO O PDF
            pdf.Close();
            arquivo.Close();

            // ABRINDO O ARQUIVO
            var caminhoPdf = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/docs/" + path);
            if (System.IO.File.Exists(caminhoPdf))
            {
                Process.Start(new ProcessStartInfo()
                {
                    Arguments = $"/c start {caminhoPdf}",
                    FileName = "cmd.exe",
                    CreateNoWindow = true
                });
            }

            return CustomResponse();
        }

        static void CriarCelulaTexto(PdfPTable tabela, string texto, int alinhamentoHorz = PdfPCell.ALIGN_LEFT, bool negrito = false, bool italico = false, int tamanhoFonte = 10, int alturaCelula = 25)
        {
            int estilo = Font.NORMAL;

            if (negrito && italico)
            {
                estilo = Font.BOLDITALIC;
            }
            else if (negrito)
            {
                estilo = Font.BOLD;
            }
            else if (italico)
            {
                estilo = Font.ITALIC;
            }

            var fonteCelula = new Font(fonteBase, tamanhoFonte, estilo, BaseColor.Black);

            var bgColor = BaseColor.White;
            if (tabela.Rows.Count % 2 == 1)
                bgColor = new BaseColor(0.95F, 0.95F, 0.95F);

            var celula = new PdfPCell(new Phrase(texto, fonteCelula));
            celula.HorizontalAlignment = alinhamentoHorz;
            celula.VerticalAlignment = PdfPCell.ALIGN_MIDDLE;
            celula.Border = 0;
            celula.BorderWidthBottom = 1;
            celula.FixedHeight = alturaCelula;
            celula.PaddingBottom = 5;
            celula.BackgroundColor = bgColor;
            tabela.AddCell(celula);
        }

        [AllowAnonymous]
        [HttpGet("comprovante-pdf/{doacaoId:guid}")]
        public async Task<ActionResult> GerarComprovantePdf(Guid doacaoId)
        {
            #region PEGANDO DOAÇÃO
            // PEGAR DOACOES DA CAMPANHA ENVIADA
            var doacao = await _doacaoRepository.GetDonationsWithCampaignAsync(doacaoId);

            if (doacao == null)
            {
                NotificarErro("Doação não encontrada");
                return CustomResponse();
            }
            #endregion

            // Create a memory stream to hold the PDF
            MemoryStream ms = new MemoryStream();

            // CONFIGURAÇÃO DO DOCUMENTO
            var pxPorMm = 72 / 25.2F;
            var pdf = new Document(PageSize.A4.Rotate(), 15 * pxPorMm, 15 * pxPorMm, 15 * pxPorMm, 20 * pxPorMm);
            var writer = PdfWriter.GetInstance(pdf, ms);
            pdf.Open();

            #region DOCUMENTO EDITADO
            // ADICIONANDO TÍTULO
            var fonteTitulo = new Font(fonteBase, 28, Font.NORMAL, BaseColor.Black);
            var titulo = new Paragraph("Comprovante de Doação\n\n", fonteTitulo);
            titulo.Alignment = Element.ALIGN_LEFT;
            titulo.SpacingAfter = 10;
            pdf.Add(titulo);

            // ADICIONANDO A LOGOMARCA
            var pathImagem = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/logomarca.png");
            if (System.IO.File.Exists(pathImagem))
            {
                Image logo = Image.GetInstance(pathImagem);
                float razaoLarguraAltura = logo.Width / logo.Height;
                float alturaLogo = 60;
                float larguraLogo = alturaLogo * razaoLarguraAltura;
                logo.ScaleToFit(larguraLogo, alturaLogo);
                var margemEsquerda = pdf.PageSize.Width - pdf.RightMargin - larguraLogo;
                var margemTopo = pdf.PageSize.Height - pdf.TopMargin - 54;
                logo.SetAbsolutePosition(margemEsquerda, margemTopo);
                writer.DirectContent.AddImage(logo, false);
            }

            // ADICIONANDO A TABELA
            var tabela = new PdfPTable(4);
            float[] largurasColunas = { 0.5f, 1.5f, 1.0f, 1.2f };
            tabela.SetWidths(largurasColunas);
            tabela.DefaultCell.BorderWidth = 0;
            tabela.WidthPercentage = 100;

            // ADICIONAR TÍTULOS
            CriarCelulaTexto(tabela, "Data", PdfPCell.ALIGN_LEFT, true);
            CriarCelulaTexto(tabela, "Campanha", PdfPCell.ALIGN_LEFT, true);
            CriarCelulaTexto(tabela, "Valor Doado", PdfPCell.ALIGN_CENTER, true);
            CriarCelulaTexto(tabela, "Forma de Pagamento", PdfPCell.ALIGN_CENTER, true);

            CriarCelulaTexto(tabela, doacao.Data.ToString("dd/MM/yyyy"), PdfPCell.ALIGN_LEFT);
            CriarCelulaTexto(tabela, doacao.Campanha.Titulo, PdfPCell.ALIGN_LEFT);
            CriarCelulaTexto(tabela, "R$ " + doacao.Valor.ToString(), PdfPCell.ALIGN_CENTER);

            if (doacao.FormaPagamento == "billing")
            {
                CriarCelulaTexto(tabela, "Boleto", PdfPCell.ALIGN_CENTER);
            }
            else if (doacao.FormaPagamento == "pix")
            {
                CriarCelulaTexto(tabela, "PIX", PdfPCell.ALIGN_CENTER);
            }
            else if (doacao.FormaPagamento == "credit_card")
            {
                CriarCelulaTexto(tabela, "Cartão de Crédito", PdfPCell.ALIGN_CENTER);
            }

            pdf.Add(tabela);

            //ADICIONAR CNPJ
            var fonteDireitosReservados = new Font(fonteBase, 9, Font.NORMAL, BaseColor.Black);
            Paragraph copyright = new Paragraph("© 2023 Direitos Reservados - Vaquinha Animal - CNPJ: 48.173.612/0001-02 - Grupo Doadores Especiais.", fonteDireitosReservados);
            PdfPTable footerTbl = new PdfPTable(1);
            footerTbl.TotalWidth = 500;
            PdfPCell cell = new PdfPCell(copyright);
            cell.Border = 1;
            footerTbl.AddCell(cell);
            footerTbl.WriteSelectedRows(0, -1, 30, 30, writer.DirectContent);
            #endregion

            // FECHANDO O PDF
            pdf.Close();
            writer.Close();

            // Set the response content type
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new ByteArrayContent(ms.ToArray());
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            // Set the content disposition header for download
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = "Comprovante.pdf"
            };

            return File(ms.ToArray(), "application/pdf");
        }
        #endregion
    }
}