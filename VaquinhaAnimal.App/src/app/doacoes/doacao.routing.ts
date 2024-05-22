import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { DoacoesComponent } from './doacao.component';
import { MinhasDoacoesComponent } from './minhas-doacoes/minhas-doacoes.component';
import { DoacaoGuard } from './doacao.guard';

const doacoesRouterConfig: Routes = [
    {
        path: '', component: DoacoesComponent,
        children: [
            { 
                path: 'minhas-doacoes', component: MinhasDoacoesComponent, canActivate: [DoacaoGuard]
            }
        ]
    }
];

@NgModule({
    imports: [
        RouterModule.forChild(doacoesRouterConfig)
    ],
    exports: [RouterModule]
})
export class DoacoesRoutingModule { }