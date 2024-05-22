import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdocaoComponent } from './adocao.component';
import { CriarComponent } from './criar/criar.component';
import { AdocaoGuard } from './adocao.guard';
import { AdocaoResolve } from './adocao.resolve';
import { ListarTodasComponent } from './listar-todas/listar-todas.component';
import { DetailComponent } from './detail/detail.component';
import { MeusPetsComponent } from './meus-pets/meus-pets.component';

const adocoesRouterConfig: Routes = [
    {
        path: '', component: AdocaoComponent,
        children: [
            { path: 'listar-todas', component: ListarTodasComponent },
            {
                path: 'criar', component: CriarComponent,
                canActivate: [AdocaoGuard],
                canDeactivate: [AdocaoGuard]
            },
            { 
                path: 'meus-pets', component: MeusPetsComponent,
                canActivate: [AdocaoGuard]
            },
            {
                path: 'detalhes/:url_adocao', component: DetailComponent,
                resolve: {
                    adocao: AdocaoResolve
                }
            },
            { path: '**', redirectTo: 'listar-todas', pathMatch: 'full' }
        ]
    }
];

@NgModule({
    imports: [
        RouterModule.forChild(adocoesRouterConfig)
    ],
    exports: [RouterModule]
})
export class AdocoesRoutingModule { }