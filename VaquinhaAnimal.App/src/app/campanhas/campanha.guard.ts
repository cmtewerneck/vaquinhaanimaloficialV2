import { Injectable } from '@angular/core';
import { Router, CanActivate, ActivatedRoute, CanDeactivate } from '@angular/router';
import { LocalStorageUtils } from '../_utils/localStorage';
import { CriarComponent } from './criar/criar.component';
import { FormGroup } from '@angular/forms';

@Injectable()
export class CampanhaGuard implements CanDeactivate<CriarComponent>, CanActivate {

    localStorageUtils = new LocalStorageUtils();

    constructor(private router: Router, private route: ActivatedRoute){}

    canActivate() {
        if(!this.localStorageUtils.obterTokenUsuarioSession()){
            this.router.navigate(['/auth/login/'], { queryParams: { returnUrl: '/campanhas/criar' }});
        }

        return true;  
    }

    canDeactivate(component: CriarComponent): boolean {
        if(component.campanhaForm.dirty){
            return window.confirm('Eiii, não desista dessa campanha. Estamos aqui pra te ajudar!');
        }

        return true;
    }
}