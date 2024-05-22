import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Adocao } from './model/Adocao';
import { BaseService } from '../_bases/base.service';
import { Observable } from 'rxjs';
import { PagarmeCardResponse, PagarmeResponse } from '../auth/User';
import { PagedResult } from '../_utils/pagedResult';

@Injectable()
export class AdocaoService extends BaseService {

  constructor(private http: HttpClient) { super() }

    obterTodos(): Observable<Adocao[]> {
        return this.http
            .get<Adocao[]>(this.urlServiceV1 + 'adocoes', this.ObterAuthHeaderJson())
            .pipe(catchError(super.serviceError));
    }

    obterUrl(url_adocao: string): Observable<Adocao> {
        return this.http
            .get<Adocao>(this.urlServiceV1 + 'adocoes/obter_url/' + url_adocao, this.ObterAuthHeaderJson())
            .pipe(catchError(super.serviceError));
    }

    obterTodosPaginado(pageSize: number, pageNumber: number): Observable<PagedResult<Adocao>> {
        return this.http
            .get<PagedResult<Adocao>>(this.urlServiceV1 + 'adocoes/todos-paginado/' + pageSize + '/' + pageNumber, this.ObterAuthHeaderJson())
            .pipe(catchError(super.serviceError));
    }

    obterMinhasAdocoesPaginado(pageSize: number, pageNumber: number): Observable<PagedResult<Adocao>> {
        return this.http
            .get<PagedResult<Adocao>>(this.urlServiceV1 + 'adocoes/minhas-adocoes-paginado/' + pageSize + '/' + pageNumber, this.ObterAuthHeaderJson())
            .pipe(catchError(super.serviceError));
    }

    obterPorId(id: string): Observable<Adocao> {
        return this.http
            .get<Adocao>(this.urlServiceV1 + 'adocoes/' + id, this.ObterAuthHeaderJson())
            .pipe(catchError(super.serviceError));
    }

    novaAdocao(adocao: Adocao): Observable<Adocao> {
        return this.http
            .post(this.urlServiceV1 + 'adocoes', adocao, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    atualizarAdocao(adocao: Adocao): Observable<Adocao> {
        return this.http
            .put(this.urlServiceV1 + 'adocoes/' + adocao.id, adocao, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    marcarAdotado(adocaoId: string): Observable<Adocao> {
        return this.http
            .put(this.urlServiceV1 + 'adocoes/marcar-adotado/' + adocaoId, null, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    marcarListado(adocaoId: string): Observable<Adocao> {
        return this.http
            .put(this.urlServiceV1 + 'adocoes/marcar-listado/' + adocaoId, null, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    excluirAdocao(id: string): Observable<Adocao> {
        return this.http
            .delete(this.urlServiceV1 + 'adocoes/' + id, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }
}