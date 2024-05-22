import { Injectable } from '@angular/core';
import { Resolve, ActivatedRouteSnapshot } from '@angular/router';
import { User } from './User';
import { AuthService } from './auth.service';
import { LocalStorageUtils } from '../_utils/localStorage';

@Injectable()
export class AuthResolve implements Resolve<User> {

    constructor(private authService: AuthService) { }

    resolve(route: ActivatedRouteSnapshot) {
        var idToReturn = this.authService.obterPorId(route.params['id']);
        return idToReturn;
    }
}