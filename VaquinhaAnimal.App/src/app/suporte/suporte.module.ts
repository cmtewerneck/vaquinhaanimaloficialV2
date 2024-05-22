import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SuporteRoutingModule } from './suporte.routing';
import { RouterModule } from '@angular/router';
import { NgxSpinnerModule } from 'ngx-spinner';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { ToastrModule } from 'ngx-toastr';
import { ModalModule } from 'ngx-bootstrap/modal';

import { SuporteAppComponent } from './suporte.app.component';

import { SuporteGuard } from './suporte.guard';
import { SuporteService } from './suporte.service';
import { BrowserModule } from '@angular/platform-browser';
import { SuporteComponent } from './suporte.component';
import { ListaSuporteAdminComponent } from './lista-suporte-admin/listaSuporteAdmin.component';
import { SuporteAdminGuard } from './suporte.admin.guard';

@NgModule({
  imports: [
    CommonModule,
    SuporteRoutingModule,
    RouterModule,
    NgxSpinnerModule,
    FormsModule,
    ReactiveFormsModule,
    HttpClientModule,
    ModalModule.forRoot(),
    ToastrModule.forRoot({
      timeOut: 3000,
      positionClass: 'toast-bottom-right',
      preventDuplicates: true,
      maxOpened: 0,
      progressBar: true,
      progressAnimation: 'decreasing'
    })
  ],
  declarations: [
    SuporteAppComponent,
    ListaSuporteAdminComponent,
    SuporteComponent
  ],
  providers: [
    SuporteService,
    SuporteAdminGuard,
    SuporteGuard
  ]
})
export class SuporteModule { }
