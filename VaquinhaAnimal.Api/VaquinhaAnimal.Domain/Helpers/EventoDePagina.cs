using iTextSharp.text;
using iTextSharp.text.pdf;
using System;

namespace VaquinhaAnimal.Domain.Helpers
{
    public class EventoDePagina : PdfPageEventHelper
    {
        private BaseFont fonteBaseRodape { get; set; }
        private Font fonteRodape { get; set; }
        private PdfContentByte wdc;
        private int totalPaginas;

        public EventoDePagina(int totalPaginas)
        {
            fonteBaseRodape = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false);
            fonteRodape = new Font(fonteBaseRodape, 8f, Font.NORMAL, BaseColor.Black);
            this.totalPaginas = totalPaginas;
        }

        public override void OnEndPage(PdfWriter writer, Document document)
        {
            base.OnEndPage(writer, document);
            AdicionarMomentoGeracaoRelatorio(writer, document);
            AdicionarNumeracaoPagina(writer, document);
        }

        public override void OnOpenDocument(PdfWriter writer, Document document)
        {
            base.OnOpenDocument(writer, document);
            this.wdc = writer.DirectContent;
        }

        private void AdicionarMomentoGeracaoRelatorio(PdfWriter writer, Document document)
        {
            var textoMomentoGeracao = $"Gerado em {DateTime.Now.ToShortDateString()} às " + $"{DateTime.Now.ToShortTimeString()}";

            wdc.BeginText();
            wdc.SetFontAndSize(fonteRodape.BaseFont, fonteRodape.Size);
            wdc.SetTextMatrix(document.LeftMargin, document.BottomMargin * 0.75f);
            wdc.ShowText(textoMomentoGeracao);
            wdc.EndText();
        }

        private void AdicionarNumeracaoPagina(PdfWriter writer, Document document)
        {
            int paginaAtual = writer.PageNumber;
            var textoPaginacao = $"Página {paginaAtual} de {totalPaginas}";

            float larguraTextoPaginacao = fonteBaseRodape.GetWidthPoint(textoPaginacao, fonteRodape.Size);

            var tamanhoPagina = document.PageSize;

            wdc.BeginText();
            wdc.SetFontAndSize(fonteRodape.BaseFont, fonteRodape.Size);
            wdc.SetTextMatrix(tamanhoPagina.Width - document.RightMargin - larguraTextoPaginacao, document.BottomMargin * 0.75f);
            wdc.ShowText(textoPaginacao);
            wdc.EndText();
        }
    }
}
