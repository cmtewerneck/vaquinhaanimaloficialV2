import { Injectable } from '@angular/core';
import { Router, CanActivate, ActivatedRoute } from '@angular/router';
import { LocalStorageUtils } from '../_utils/localStorage';

@Injectable()
export class CampanhaAdminGuard implements CanActivate {

    localStorageUtils = new LocalStorageUtils();

    constructor(private router: Router, private route: ActivatedRoute){}

    canActivate() {
        let userLogado = this.localStorageUtils.obterUsuarioSession();
        if(userLogado.email != 'contato@vaquinhaanimal.com.br'){
            this.router.navigate(['/auth/login/'], { queryParams: { returnUrl: '/campanhas/criar' }});
        }

        return true;  
    }
}