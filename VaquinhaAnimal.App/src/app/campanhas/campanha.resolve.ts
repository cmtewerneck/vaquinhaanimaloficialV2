import { Injectable } from '@angular/core';
import { Resolve, ActivatedRouteSnapshot } from '@angular/router';
import { Campanha } from './model/Campanha';
import { CampanhaService } from './campanha.service';

@Injectable()
export class CampanhaResolve implements Resolve<Campanha> {

    constructor(private campanhaService: CampanhaService) { }

    resolve(route: ActivatedRouteSnapshot) {
        var campanha = this.campanhaService.obterUrl(route.params['url_campanha']);
        return campanha;
    }
}