import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { ArtigoAppComponent } from './artigo.app.component';
import { ListaComponent } from './lista/lista.component';
import { AddComponent } from './add/add.component';
import { ArtigoGuard } from './artigo.guard';
import { DetailComponent } from './detail/detail.component';
import { ArtigoResolve } from './artigo.resolve';
import { ListaAdminComponent } from './lista-admin/lista-admin.component';
import { EditComponent } from './edit/edit.component';
import { ArtigoEditGuard } from './artigo.edit.guard';

const artigoRouterConfig: Routes = [
    {
        path: '', component: ArtigoAppComponent,
        children: [
            {
                 path: 'listar-todos', component: ListaComponent
            },
            {
                path: 'listar-todos-admin', component: ListaAdminComponent,
                canActivate: [ArtigoGuard]
            },
            {
                path: 'adicionar-novo', component: AddComponent,
                canActivate: [ArtigoGuard]
            },
            {
                path: 'editar/:id', component: EditComponent,
                canActivate: [ArtigoGuard],
                resolve: {
                    artigo: ArtigoResolve
                }
            },
            {
                path: 'detalhes/:url_artigo', component: DetailComponent,
                resolve: {
                    artigo: ArtigoResolve
                }
            },
        ]
    }
];

@NgModule({
    imports: [
        RouterModule.forChild(artigoRouterConfig)
    ],
    exports: [RouterModule]
})
export class ArtigoRoutingModule { }