import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AssinaturaRoutingModule } from './assinatura.routing';
import { RouterModule } from '@angular/router';
import { NgxSpinnerModule } from 'ngx-spinner';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { ToastrModule } from 'ngx-toastr';
import { ModalModule } from 'ngx-bootstrap/modal';

import { AssinaturaAppComponent } from './assinatura.app.component';
import { MinhasAssinaturasComponent } from './minhasAssinaturas/minhasAssinaturas.component';

import { AssinaturaGuard } from './assinatura.guard';
import { AssinaturaService } from './assinatura.service';
import { StatusAssinaturaPipe } from './status.assinatura.pipe';

@NgModule({
  imports: [
    CommonModule,
    AssinaturaRoutingModule,
    RouterModule,
    NgxSpinnerModule,
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
    }),
    FormsModule
  ],
  declarations: [
    AssinaturaAppComponent,
    MinhasAssinaturasComponent,
    StatusAssinaturaPipe
  ],
  providers: [
    AssinaturaService,
    AssinaturaGuard
  ]
})
export class AssinaturaModule { }
