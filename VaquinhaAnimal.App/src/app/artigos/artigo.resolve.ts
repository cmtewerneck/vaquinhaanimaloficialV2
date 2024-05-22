import { Injectable } from '@angular/core';
import { Resolve, ActivatedRouteSnapshot } from '@angular/router';
import { Artigo } from './model/Artigo';
import { ArtigoService } from './artigo.service';

@Injectable()
export class ArtigoResolve implements Resolve<Artigo> {

    constructor(private artigoService: ArtigoService) { }

    resolve(route: ActivatedRouteSnapshot) {
        var artigo = this.artigoService.obterUrl(route.params['url_artigo']);
        return artigo;
    }
}