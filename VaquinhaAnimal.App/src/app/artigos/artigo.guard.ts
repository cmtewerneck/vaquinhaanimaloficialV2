import { Injectable } from '@angular/core';
import { Router, CanActivate } from '@angular/router';
import { LocalStorageUtils } from '../_utils/localStorage';

@Injectable()
export class ArtigoGuard implements CanActivate {

    localStorageUtils = new LocalStorageUtils();

    constructor(private router: Router){}

    canActivate() {
        let userLogado = this.localStorageUtils.obterUsuarioSession();
        if(userLogado.email != 'contato@vaquinhaanimal.com.br'){
            this.router.navigate(['/acesso-negado']);
        }

        return true;  
    }
}