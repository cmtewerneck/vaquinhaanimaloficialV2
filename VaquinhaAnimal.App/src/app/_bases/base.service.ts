import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { throwError } from 'rxjs';
import { environment } from 'src/environments/environment';
import { LocalStorageUtils } from '../_utils/localStorage';

export abstract class BaseService {

    public LocalStorage = new LocalStorageUtils();
    protected urlServiceV1: string = environment.apiUrlV1;

    protected obterHeaderJson() {
        return {
            headers: new HttpHeaders({
                'Content-Type': 'application/json'
            })
        };
    }

    protected ObterAuthHeaderJson() {
        return {
            headers: new HttpHeaders({
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.LocalStorage.obterTokenUsuarioSession()}`
            })
        };
    }

    protected PdfHeader() {
        return {
            headers: new HttpHeaders({
                'Accept': 'application/json',
            })
        };
      }

    protected extractData(response: any) {
        return response.data || {};
    }

    protected serviceError(response: Response | any) {
        let customErro: string[] = [];

        if (response instanceof HttpErrorResponse) {
            if (response.statusText === 'Unknown Error') {
                customErro.push('Ocorreu um erro desconhecido');
                response.error.errors = customErro;
            }
        }

        console.error(response);
        return throwError(response);
    }
}