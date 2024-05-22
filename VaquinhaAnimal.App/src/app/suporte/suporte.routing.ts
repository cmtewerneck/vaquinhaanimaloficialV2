import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ListaSuporteAdminComponent } from './lista-suporte-admin/listaSuporteAdmin.component';
import { SuporteAppComponent } from './suporte.app.component';
import { SuporteComponent } from './suporte.component';
import { SuporteGuard } from './suporte.guard';
import { SuporteAdminGuard } from './suporte.admin.guard';

const suporteRouterConfig: Routes = [
    {
        path: '', component: SuporteAppComponent,
        children: [
            { 
                path: 'listar-suporte', component: SuporteComponent,
                canActivate: [SuporteGuard]
            },
            { 
                path: 'listar-suporte-admin', component: ListaSuporteAdminComponent,
                canActivate: [SuporteAdminGuard]
            }
        ]
    }
    
];

@NgModule({
    imports: [
        RouterModule.forChild(suporteRouterConfig)
    ],
    exports: [RouterModule]
})
export class SuporteRoutingModule { }