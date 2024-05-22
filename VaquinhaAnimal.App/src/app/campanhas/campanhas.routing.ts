import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { CampanhasComponent } from './campanhas.component';
import { ListarTodasComponent } from './listar-todas/listar-todas.component';
import { CriarComponent } from './criar/criar.component';
import { CampanhaGuard } from './campanha.guard';
import { MinhasCampanhasComponent } from './minhas-campanhas/minhas-campanhas.component';
import { DetailComponent } from './detail/detail.component';
import { CampanhaResolve } from './campanha.resolve';
import { ListaAdminComponent } from './lista-admin/listaAdmin.component';
import { CampanhaAdminGuard } from './campanha.admin.guard';
import { EditComponent } from './edit/edit.component';
import { CampanhaEditGuard } from './campanha.edit.guard';

const campanhasRouterConfig: Routes = [
    {
        path: '', component: CampanhasComponent,
        children: [
            { path: 'listar-todas', component: ListarTodasComponent },
            {
                path: 'listar-todos-admin', component: ListaAdminComponent,
                canActivate: [CampanhaAdminGuard]
            },
            {
                path: 'criar', component: CriarComponent,
                canActivate: [CampanhaGuard],
                canDeactivate: [CampanhaGuard]
            },
            { 
                path: 'minhas-campanhas', component: MinhasCampanhasComponent,
                canActivate: [CampanhaGuard]
            },
            {
                path: 'editar/:url_campanha', component: EditComponent,
                canActivate: [CampanhaEditGuard],
                resolve: {
                    campanha: CampanhaResolve
                }
            },
            {
                path: 'detalhes/:url_campanha', component: DetailComponent,
                resolve: {
                    campanha: CampanhaResolve
                }
            },
            { path: '**', redirectTo: 'listar-todas', pathMatch: 'full' }
        ]
    }
];

@NgModule({
    imports: [
        RouterModule.forChild(campanhasRouterConfig)
    ],
    exports: [RouterModule]
})
export class CampanhasRoutingModule { }