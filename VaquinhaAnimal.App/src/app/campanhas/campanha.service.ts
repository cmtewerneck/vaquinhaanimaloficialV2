import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Campanha } from './model/Campanha';
import { BaseService } from '../_bases/base.service';
import { Observable } from 'rxjs';
import { PagarmeCardResponse, PagarmeResponse } from '../auth/User';
import { PagedResult } from '../_utils/pagedResult';
import { Order } from './model/Order';
import { OrderCartaoNovo } from './model/OrderCartaoNovo';
import { OrderBoleto } from './model/OrderBoleto';
import { OrderPix } from './model/OrderPix';
import { OrderPixRapido } from './model/OrderPixRapido';
import { OrderCartao } from './model/OrderCartao';
import { OrderAssinatura } from './model/OrderAssinatura';

@Injectable()
export class CampanhaService extends BaseService {

  constructor(private http: HttpClient) { super() }

    obterTodos(): Observable<Campanha[]> {
        return this.http
            .get<Campanha[]>(this.urlServiceV1 + 'campanhas', this.ObterAuthHeaderJson())
            .pipe(catchError(super.serviceError));
    }

    obterOutrasCampanhas(): Observable<Campanha[]> {
        return this.http
            .get<Campanha[]>(this.urlServiceV1 + 'campanhas/menores-valores', this.ObterAuthHeaderJson())
            .pipe(catchError(super.serviceError));
    }

    obterTodosPaginado(pageSize: number, pageNumber: number): Observable<PagedResult<Campanha>> {
        return this.http
            .get<PagedResult<Campanha>>(this.urlServiceV1 + 'campanhas/todos-paginado/' + pageSize + '/' + pageNumber, this.ObterAuthHeaderJson())
            .pipe(catchError(super.serviceError));
    }

    obterMinhasCampanhasPaginado(pageSize: number, pageNumber: number): Observable<PagedResult<Campanha>> {
        return this.http
            .get<PagedResult<Campanha>>(this.urlServiceV1 + 'campanhas/minhas-campanhas-paginado/' + pageSize + '/' + pageNumber, this.ObterAuthHeaderJson())
            .pipe(catchError(super.serviceError));
    }

    exportToPdf(campanhaId: string) {
        return this.http
            .get(this.urlServiceV1 + 'doacoes/export-to-pdf/' + campanhaId, this.ObterAuthHeaderJson());
    }

    obterMinhasCampanhas(): Observable<Campanha[]> {
        return this.http
            .get<Campanha[]>(this.urlServiceV1 + 'campanhas/minhas-campanhas', this.ObterAuthHeaderJson())
            .pipe(catchError(super.serviceError));
    }

    obterQuantidadeDoadores(campanhaId: string): Observable<number> {
        return this.http
            .get<number>(this.urlServiceV1 + 'doacoes/total-doadores/' + campanhaId, this.ObterAuthHeaderJson())
            .pipe(catchError(super.serviceError));
    }

    obterPorId(id: string): Observable<Campanha> {
        return this.http
            .get<Campanha>(this.urlServiceV1 + 'campanhas/' + id, this.ObterAuthHeaderJson())
            .pipe(catchError(super.serviceError));
    }

    baixarRelatorio(campanhaId: string) {
        return this.http
            .get(this.urlServiceV1 + 'doacoes/relatorio-pdf/' + campanhaId, this.ObterAuthHeaderJson());
      }

    obterUrl(url_campanha: string): Observable<Campanha> {
        return this.http
            .get<Campanha>(this.urlServiceV1 + 'campanhas/obter_url/' + url_campanha, this.ObterAuthHeaderJson())
            .pipe(catchError(super.serviceError));
    }

    novaCampanha(campanha: Campanha): Observable<Campanha> {
        return this.http
            .post(this.urlServiceV1 + 'campanhas', campanha, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    atualizarCampanha(campanha: Campanha): Observable<Campanha> {
        return this.http
            .put(this.urlServiceV1 + 'campanhas/' + campanha.id, campanha, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    excluirCampanha(id: string): Observable<Campanha> {
        return this.http
            .delete(this.urlServiceV1 + 'campanhas/' + id, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    enviarParaAnalise(id: string): Observable<Campanha> {
        return this.http
            .put(this.urlServiceV1 + 'campanhas/enviar-para-analise/' + id, null, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    iniciarCampanha(id: string): Observable<Campanha> {
        return this.http
            .put(this.urlServiceV1 + 'campanhas/iniciar-campanha/' + id, null, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    pararCampanha(id: string): Observable<Campanha> {
        return this.http
            .put(this.urlServiceV1 + 'campanhas/parar-campanha/' + id, null, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    retornarCampanha(id: string): Observable<Campanha> {
        return this.http
            .put(this.urlServiceV1 + 'campanhas/retornar-campanha/' + id, null, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    rejeitarCampanha(id: string, motivoreprovacao: string): Observable<Campanha> {
        return this.http
            .put(this.urlServiceV1 + 'campanhas/rejeitar-campanha/' + id + "/" + motivoreprovacao, null, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    realizarDoacao(doacao: Order, campanhaId: string): Observable<Order> {
        return this.http
            .post(this.urlServiceV1 + 'transacoes/add-order/' + campanhaId, doacao, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    realizarDoacaoCartaoNovo(doacaoCartaoNovo: OrderCartaoNovo, campanhaId: string): Observable<Order> {
        return this.http
            .post(this.urlServiceV1 + 'transacoes/add-order-cartao-novo/' + campanhaId, doacaoCartaoNovo, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    realizarDoacaoBoleto(doacaoBoleto: OrderBoleto, campanhaId: string): Observable<OrderBoleto> {
        return this.http
            .post(this.urlServiceV1 + 'transacoes/add-order-boleto/' + campanhaId, doacaoBoleto, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    realizarAssinatura(assinatura: OrderAssinatura, campanhaId: string): Observable<OrderAssinatura> {
        return this.http
            .post(this.urlServiceV1 + 'transacoes/add-assinatura/' + campanhaId, assinatura, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    realizarDoacaoPix(doacaoPix: OrderPix, campanhaId: string): Observable<any> {
        return this.http
            .post(this.urlServiceV1 + 'transacoes/add-order-pix/' + campanhaId, doacaoPix, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    realizarDoacaoCartao(doacaoCartao: OrderCartao, campanhaId: string): Observable<any> {
        return this.http
            .post(this.urlServiceV1 + 'transacoes/add-order/' + campanhaId, doacaoCartao, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    realizarDoacaoPixRapido(doacaoPixRapido: OrderPixRapido, campanhaId: string): Observable<any> {
        return this.http
            .post(this.urlServiceV1 + 'transacoes/add-order-pix-rapido/' + campanhaId, doacaoPixRapido, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    obterMeusCartoes(): Observable<PagarmeResponse<PagarmeCardResponse>> {
        return this.http
            .get<PagarmeResponse<PagarmeCardResponse>>(this.urlServiceV1 + 'transacoes/list-card', this.ObterAuthHeaderJson())
            .pipe(catchError(super.serviceError));
      }

}