import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseService } from './_bases/base.service';
import { PagedResult } from './_utils/pagedResult';
import { PagarmeCardResponse, PagarmeResponse } from './auth/User';
import { Campanha } from './campanhas/model/Campanha';

@Injectable()
export class AppService extends BaseService {
  
  constructor(private http: HttpClient) { super() }
  
  obterMeusCartoes(): Observable<PagarmeResponse<PagarmeCardResponse>> {
    return this.http
    .get<PagarmeResponse<PagarmeCardResponse>>(this.urlServiceV1 + 'transacoes/list-card', this.ObterAuthHeaderJson())
    .pipe(catchError(super.serviceError));
  }
  
  obterQuantidadeDoadores(campanhaId: string): Observable<number> {
    return this.http
    .get<number>(this.urlServiceV1 + 'doacoes/total-doadores/' + campanhaId, this.ObterAuthHeaderJson())
    .pipe(catchError(super.serviceError));
  }

  obterCampanhasPremium(): Observable<Campanha[]> {
    return this.http
        .get<Campanha[]>(this.urlServiceV1 + 'campanhas/campanhas-destaque', this.ObterAuthHeaderJson())
        .pipe(catchError(super.serviceError));
}
  
}