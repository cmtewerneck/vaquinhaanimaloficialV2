import { Injectable } from '@angular/core';
import { Router, CanActivate, ActivatedRoute, CanDeactivate } from '@angular/router';
import { LocalStorageUtils } from '../_utils/localStorage';
import { CriarComponent } from './criar/criar.component';

@Injectable()
export class AdocaoGuard implements CanDeactivate<CriarComponent>, CanActivate {

    localStorageUtils = new LocalStorageUtils();

    constructor(private router: Router, private route: ActivatedRoute){}

    canActivate() {
        if(!this.localStorageUtils.obterTokenUsuarioSession()){
            this.router.navigate(['/auth/login/'], { queryParams: { returnUrl: '/adocoes/criar' }});
        }

        return true;  
    }

    canDeactivate(component: CriarComponent): boolean {
        if(component.adocaoForm.dirty){
            return window.confirm('Eiii, não desista de listar pra adoção. Alguém super especial pode aparecer!');
        }

        return true;
    }
}