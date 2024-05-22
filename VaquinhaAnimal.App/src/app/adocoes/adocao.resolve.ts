import { Injectable } from '@angular/core';
import { Resolve, ActivatedRouteSnapshot } from '@angular/router';
import { Adocao } from './model/Adocao';
import { AdocaoService } from './adocao.service';

@Injectable()
export class AdocaoResolve implements Resolve<Adocao> {

    constructor(private adocaoService: AdocaoService) { }

    resolve(route: ActivatedRouteSnapshot) {
        var adocao = this.adocaoService.obterUrl(route.params['url_adocao']);
        return adocao;
    }
}