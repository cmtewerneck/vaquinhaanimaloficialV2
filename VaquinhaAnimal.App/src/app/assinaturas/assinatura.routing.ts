import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { AssinaturaAppComponent } from './assinatura.app.component';
import { MinhasAssinaturasComponent } from './minhasAssinaturas/minhasAssinaturas.component';

import { AssinaturaGuard } from './assinatura.guard';

const assinaturaRouterConfig: Routes = [
    {
        path: '', component: AssinaturaAppComponent,
        children: [
            { 
                path: 'minhas-assinaturas', component: MinhasAssinaturasComponent,
                canActivate: [AssinaturaGuard]
            }
        ]
    }
];

@NgModule({
    imports: [
        RouterModule.forChild(assinaturaRouterConfig)
    ],
    exports: [RouterModule]
})
export class AssinaturaRoutingModule { }