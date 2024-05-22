import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BaseService } from '../_bases/base.service';
import { Observable } from 'rxjs';
import { Suporte } from './Model/Suporte';
import { catchError, map } from 'rxjs/operators';

@Injectable()
export class SuporteService extends BaseService {

  constructor(private http: HttpClient) { super() }

  obterMeusTickets(): Observable<Suporte[]> {
    return this.http
        .get<Suporte[]>(this.urlServiceV1 + 'tickets/meus-tickets', this.ObterAuthHeaderJson())
        .pipe(catchError(super.serviceError));
  }

  obterTodosTickets(): Observable<Suporte[]> {
    return this.http
        .get<Suporte[]>(this.urlServiceV1 + 'tickets/all-tickets', this.ObterAuthHeaderJson())
        .pipe(catchError(super.serviceError));
  }

  addTicket(suporte: Suporte): Observable<Suporte> {
    return this.http
        .post(this.urlServiceV1 + 'tickets', suporte, this.ObterAuthHeaderJson())
        .pipe(
            map(super.extractData),
            catchError(super.serviceError));
}

excluirTicket(id: string): Observable<Suporte> {
  return this.http
      .delete(this.urlServiceV1 + 'tickets/' + id, this.ObterAuthHeaderJson())
      .pipe(
          map(super.extractData),
          catchError(super.serviceError));
}

atualizarTicket(ticket: Suporte): Observable<Suporte> {
  return this.http
      .put(this.urlServiceV1 + 'tickets/' + ticket.id, ticket, this.ObterAuthHeaderJson())
      .pipe(
          map(super.extractData),
          catchError(super.serviceError));
}

respostaTicket(ticket: Suporte): Observable<Suporte> {
  return this.http
      .put(this.urlServiceV1 + 'tickets/resposta-ticket/' + ticket.id, ticket, this.ObterAuthHeaderJson())
      .pipe(
          map(super.extractData),
          catchError(super.serviceError));
}

}