import { Injectable } from '@angular/core';
import { CanDeactivate } from '@angular/router';
import { RegisterComponent } from './register/register.component';

@Injectable()
export class AuthGuard implements CanDeactivate<RegisterComponent> {

    constructor(){}

    canDeactivate(component: RegisterComponent): boolean {
        if(component.registerForm.dirty){
            return window.confirm('Tem certeza que deseja abandonar o preenchimento do formulário?');
        }

        return true;
    }
}
