import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomepageComponent } from './homepage/homepage.component';
import { CampanhasComponent } from './campanhas/campanhas.component';
import { BlogComponent } from './blog/blog.component';
import { ContatoComponent } from './contato/contato.component';
import { AcessoNegadoComponent } from './acesso-negado/acesso-negado.component';
import { TermosComponent } from './termos/termos.component';

const routes: Routes = [
  { path: 'homepage', redirectTo: '', pathMatch: 'full' },
  { path: '', component: HomepageComponent },
  { path: 'termos', component: TermosComponent },
  { path: 'auth',
            loadChildren: () => import('./auth/auth.module')
            .then(m => m.AuthModule)
  },
  { path: 'campanhas',
            loadChildren: () => import('./campanhas/campanhas.module')
            .then(m => m.CampanhasModule)
  },
  { path: 'artigos',
            loadChildren: () => import('./artigos/artigo.module')
            .then(m => m.ArtigoModule)
  },
  { path: 'doacoes',
            loadChildren: () => import('./doacoes/doacao.module')
            .then(m => m.DoacoesModule)
  },
  { path: 'adocoes',
            loadChildren: () => import('./adocoes/adocao.module')
            .then(m => m.AdocoesModule)
  },
  { path: 'suporte',
            loadChildren: () => import('./suporte/suporte.module')
            .then(m => m.SuporteModule)
  },
  { path: 'assinaturas',
            loadChildren: () => import('./assinaturas/assinatura.module')
            .then(m => m.AssinaturaModule)
  },
  { path: 'contato',component: ContatoComponent },
  { path: 'acesso-negado',component: AcessoNegadoComponent },
  { path: 'artigos',component: BlogComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, {anchorScrolling: 'enabled'})],
  exports: [RouterModule]
})
export class AppRoutingModule { }
