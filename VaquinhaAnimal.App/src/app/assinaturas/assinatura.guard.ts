import { Injectable } from '@angular/core';
import { Router, CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { LocalStorageUtils } from '../_utils/localStorage';

@Injectable()
export class AssinaturaGuard implements CanActivate {

    localStorageUtils = new LocalStorageUtils();

    constructor(private router: Router){}

    canActivate(routeAc: ActivatedRouteSnapshot, state: RouterStateSnapshot) {
        if(!this.localStorageUtils.obterTokenUsuarioSession()){
            this.router.navigate(['/auth/login/'], { queryParams: { returnUrl: this.router.url }});
        }

        let user = this.localStorageUtils.obterUsuarioSession();
        let claim: any = routeAc.data[0];
        
        if (claim !== undefined) {
            let claim = routeAc.data[0]['claim'];

            if (claim) {
                if (!user.claims) {
                    this.navegarAcessoNegado();
                }
                
                let userClaims = user.claims.find((x: any) => x.type === claim.nome);
                
                if(!userClaims){
                    this.navegarAcessoNegado();
                }
                
                let valoresClaim = userClaims.value as string;

                if (!valoresClaim.includes(claim.valor)) {
                    this.navegarAcessoNegado();
                }
            }
        }

        return true;  
    }

    navegarAcessoNegado() {
        this.router.navigate(['/acesso-negado']);
    }
}