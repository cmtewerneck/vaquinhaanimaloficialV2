import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { BaseService } from '../_bases/base.service';
import { Observable } from 'rxjs';
import { Doacao } from './model/Doacao';
import { PagedResult } from '../_utils/pagedResult';

@Injectable()
export class DoacaoService extends BaseService {

  constructor(private http: HttpClient) { super() }

  obterMinhasDoacoes(): Observable<Doacao[]> {
    return this.http
        .get<Doacao[]>(this.urlServiceV1 + 'doacoes/minhas-doacoes', this.ObterAuthHeaderJson())
        .pipe(catchError(super.serviceError));
  }
  
  obterMinhasDoacoesPaginado(pageSize: number, pageNumber: number): Observable<PagedResult<Doacao>> {
    return this.http
        .get<PagedResult<Doacao>>(this.urlServiceV1 + 'doacoes/minhas-doacoes-paginado/' + pageSize + '/' + pageNumber, this.ObterAuthHeaderJson())
        .pipe(catchError(super.serviceError));
  }

  exportToPdf(doacaoId: string) {
    return this.http
        .get(this.urlServiceV1 + 'doacoes/comprovante-pdf/' + doacaoId, { responseType: 'blob' })
        .pipe(
          catchError(super.serviceError));
  }    

}