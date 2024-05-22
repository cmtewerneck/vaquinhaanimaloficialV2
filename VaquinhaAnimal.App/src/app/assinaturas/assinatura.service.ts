import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Assinatura } from './model/Assinatura';
import { BaseService } from '../_bases/base.service';
import { Observable } from 'rxjs';
import { PagarmeResponse } from '../auth/User';

@Injectable()
export class AssinaturaService extends BaseService {

  constructor(private http: HttpClient) { super() }

    obterMinhasAssinaturas(pageSize: number, pageNumber: number): Observable<PagarmeResponse<Assinatura>> {
        return this.http
            .get<PagarmeResponse<Assinatura>>(this.urlServiceV1 + 'transacoes/list-recurrencies/' + pageSize + "/" + pageNumber, this.ObterAuthHeaderJson())
            .pipe(catchError(super.serviceError));
    }

    cancelarAssinatura(id: string): Observable<any> {
      return this.http
          .delete(this.urlServiceV1 + 'transacoes/delete-recorrencia/' + id, this.ObterAuthHeaderJson())
          .pipe(
              map(super.extractData),
              catchError(super.serviceError));
    }

}