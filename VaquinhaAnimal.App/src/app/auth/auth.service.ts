import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { JwtHelperService } from '@auth0/angular-jwt';
import { catchError, map, tap } from 'rxjs/operators';
import { JWToken, ListCard, PagarmeCard, PagarmeCardResponse, PagarmeResponse, ResetPassword, ResetPasswordUser, User, UserToken } from './User';
import { BaseService } from '../_bases/base.service';
import { BehaviorSubject, Observable } from 'rxjs';
import { UserPassword } from './UserPassword';
import { BuscaCep } from './BuscaCep';

@Injectable({
  providedIn: 'root',
})
export class AuthService extends BaseService {

  private userChanged$ = new BehaviorSubject<any>(null);
  userChanged = this.userChanged$.asObservable();

  jwtHelper = new JwtHelperService();
  decodedToken: any;
  userLogged = new JWToken();
  userLocalStorage: any;

  constructor(private http: HttpClient) 
  { 
    super();
    
    this.userLocalStorage = this.LocalStorage.obterUsuarioSession();
    console.log(this.userLocalStorage);

    if(this.userLocalStorage != null){

      this.userLogged.userToken = new UserToken();
      
      this.userLogged.userToken.email = this.userLocalStorage.email;
      this.userLogged.userToken.nome = this.userLocalStorage.nome;
      this.userLogged.userToken.id = this.userLocalStorage.id;

      console.log("User Logado");
      console.log(this.userLogged);
      
      this.userChanged$.next(this.userLogged);
    }
  }

  login(user: User): Observable<User>{
    return this.http
      .post(this.urlServiceV1 + 'entrar', user, this.obterHeaderJson())
      .pipe(
          map(this.extractData),
          tap(x => this.userChanged$.next(x)),
          catchError(this.serviceError)
    );
  }

  confirmEmail(username: string, token: string): Observable<boolean>{
    return this.http
      .post(this.urlServiceV1 + 'confirm-email/' + username + "/" + token, this.obterHeaderJson())
      .pipe(
          map(this.extractData),
          catchError(this.serviceError)
    );
  }

  logout() {
    this.LocalStorage.limparDadosLocaisUsuarioSession();
    this.userChanged$.next(null);
  }

  register(user: User): Observable<User>{
    return this.http
      .post(this.urlServiceV1 + 'nova-conta', user, this.obterHeaderJson())
      .pipe(
          map(this.extractData),
          tap(x => this.userChanged$.next(x)),
          catchError(this.serviceError)
    );
  }

  resetPassword(username: ResetPassword): Observable<ResetPassword>{
    return this.http
      .post(this.urlServiceV1 + 'reset-password-token', username, this.obterHeaderJson())
      .pipe(
          map(this.extractData),
          catchError(this.serviceError)
    );
  }

  resetPasswordUser(user: ResetPasswordUser): Observable<ResetPasswordUser>{
    return this.http
      .post(this.urlServiceV1 + 'reset-password-user', user, this.obterHeaderJson())
      .pipe(
          map(this.extractData),
          catchError(this.serviceError)
    );
  }

  editUser(user: User): Observable<User> {
    return this.http
        .put(this.urlServiceV1 + 'atualizar-dados', user, this.ObterAuthHeaderJson())
        .pipe(
            map(super.extractData),
            catchError(super.serviceError));
  }

  editUserPassword(user: UserPassword): Observable<UserPassword> {
    return this.http
        .put(this.urlServiceV1 + 'atualizar-senha', user, this.ObterAuthHeaderJson())
        .pipe(
            map(super.extractData),
            catchError(super.serviceError));
  }

  usuarioLogado() {
    const token = localStorage.getItem('token');
    return !this.jwtHelper.isTokenExpired(token!);
  }

  obterTodos(): Observable<PagarmeCard[]> {
    return this.http
        .get<PagarmeCard[]>(this.urlServiceV1 + 'campanhas', this.ObterAuthHeaderJson())
        .pipe(catchError(super.serviceError));
  }

  novoCartao(cartao: PagarmeCard): Observable<PagarmeCard>{
    return this.http
      .post(this.urlServiceV1 + 'transacoes/add-card', cartao, this.ObterAuthHeaderJson())
      .pipe(
          map(this.extractData),
          catchError(this.serviceError)
    );
  }

  obterMeusCartoes(): Observable<PagarmeResponse<PagarmeCardResponse>> {
    return this.http
        .get<PagarmeResponse<PagarmeCardResponse>>(this.urlServiceV1 + 'transacoes/list-card', this.ObterAuthHeaderJson())
        .pipe(catchError(super.serviceError));
  }

  obterMeusCartoes2(): Observable<ListCard[]> {
    return this.http
        .get<ListCard[]>(this.urlServiceV1 + 'transacoes/list-user-cards', this.ObterAuthHeaderJson())
        .pipe(catchError(super.serviceError));
  }

  deletarCartao(id: string): Observable<PagarmeCard> {
    return this.http
        .delete(this.urlServiceV1 + 'transacoes/delete-card/' + id, this.ObterAuthHeaderJson())
        .pipe(
            map(super.extractData),
            catchError(super.serviceError));
  }

  obterPorIdBackend(): Observable<User> {
    return this.http
        .get<User>(this.urlServiceV1 + 'por-id', this.ObterAuthHeaderJson())
        .pipe(catchError(super.serviceError));
  }

  obterPorId(id: string): Observable<User> {
    return this.http
        .get<User>(this.urlServiceV1 + 'por-id/' + id, this.ObterAuthHeaderJson())
        .pipe(catchError(super.serviceError));
  }

buscaCep(cep: string): Observable<BuscaCep> {
  return this.http
      .get<BuscaCep>(`https://viacep.com.br/ws/${cep}/json/`)
      .pipe(catchError(super.serviceError));
}

}