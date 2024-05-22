import { Injectable } from '@angular/core';
import { Router, CanActivate, ActivatedRouteSnapshot, ActivatedRoute } from '@angular/router';
import { LocalStorageUtils } from '../_utils/localStorage';
import { ArtigoService } from './artigo.service';
import { Artigo } from './model/Artigo';

@Injectable()
export class ArtigoEditGuard implements CanActivate {
    
    localStorageUtils = new LocalStorageUtils();
    artigo!: Artigo;
    user = this.localStorageUtils.obterUsuarioSession();
    
    constructor(private router: Router, private route: ActivatedRoute, private artigoService: ArtigoService){}
    
    canActivate() {
        let userLogado = this.localStorageUtils.obterUsuarioSession();
        if(userLogado.email != 'contato@vaquinhaanimal.com.br'){
            this.router.navigate(['/auth/login/'], { queryParams: { returnUrl: '/artigos/listar-todos-admin' }});
        }
        
        return true;  
    }
}