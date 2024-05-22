import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Artigo } from './model/Artigo';
import { BaseService } from '../_bases/base.service';
import { Observable } from 'rxjs';

@Injectable()
export class ArtigoService extends BaseService {

  constructor(private http: HttpClient) { super() }

    obterTodos(): Observable<Artigo[]> {
        return this.http
            .get<Artigo[]>(this.urlServiceV1 + 'artigos', this.ObterAuthHeaderJson())
            .pipe(catchError(super.serviceError));
    }

    obterUrl(url_artigo: string): Observable<Artigo> {
        return this.http
            .get<Artigo>(this.urlServiceV1 + 'artigos/obter_url/' + url_artigo, this.ObterAuthHeaderJson())
            .pipe(catchError(super.serviceError));
    }

    novoArtigo(artigo: Artigo): Observable<Artigo> {
        return this.http
            .post(this.urlServiceV1 + 'artigos', artigo, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

    obterPorId(id: string): Observable<Artigo> {
        return this.http
            .get<Artigo>(this.urlServiceV1 + 'artigos/' + id, this.ObterAuthHeaderJson())
            .pipe(catchError(super.serviceError));
    }

    atualizarArtigo(artigo: Artigo): Observable<Artigo> {
        return this.http
            .put(this.urlServiceV1 + 'artigos/' + artigo.id, artigo, this.ObterAuthHeaderJson())
            .pipe(
                map(super.extractData),
                catchError(super.serviceError));
    }

}