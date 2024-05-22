import { Injectable } from '@angular/core';
import { Router, CanActivate, ActivatedRouteSnapshot, ActivatedRoute } from '@angular/router';
import { LocalStorageUtils } from '../_utils/localStorage';
import { CampanhaService } from './campanha.service';
import { Campanha } from './model/Campanha';

@Injectable()
export class CampanhaEditGuard implements CanActivate {
    
    localStorageUtils = new LocalStorageUtils();
    campanha!: Campanha;
    user = this.localStorageUtils.obterUsuarioSession();
    
    constructor(private router: Router, private route: ActivatedRoute, private campanhaService: CampanhaService){}
    
    canActivate(routeAc: ActivatedRouteSnapshot) {
        if(!this.localStorageUtils.obterTokenUsuarioSession()){
            this.router.navigate(['/auth/login/'], { queryParams: { returnUrl: this.router.url }});
        }
        
        // VERIFICAR SE A CAMPANHA ESTÁ REALMENTE COMO EDITÁVEL (STATUS 1)
        this.campanhaService.obterUrl(routeAc.params['url_campanha']).subscribe(
            (response: Campanha) => { 
                this.campanha = response;
                
                if(this.campanha.status_campanha != 1){
                    this.navegarAcessoNegado();
                }

                // VERIFICAR SE O USUÁRIO DA CAMPANHA É O QUE ESTÁ ACESSANDO
                if(this.campanha.usuario_id != this.user.id){
                    this.navegarAcessoNegado();
                }
            }, 
            error => {
                this.navegarAcessoNegado();
            });

        
            
        return true;  
        }
        
        navegarAcessoNegado() {
            this.router.navigate(['/acesso-negado']);
        }
    }