import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ArtigoRoutingModule } from './artigo.routing';
import { RouterModule } from '@angular/router';
import { NgxSpinnerModule } from 'ngx-spinner';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { ToastrModule } from 'ngx-toastr';
import { ModalModule } from 'ngx-bootstrap/modal';
import { NgxPaginationModule } from 'ngx-pagination';
import { NgxMaskDirective, NgxMaskPipe, NgxMaskService, provideNgxMask } from 'ngx-mask';

import { ArtigoAppComponent } from './artigo.app.component';
import { AddComponent } from './add/add.component';
import { ListaComponent } from './lista/lista.component';

import { ArtigoGuard } from './artigo.guard';
import { ArtigoService } from './artigo.service';
import { ImageCropperComponent } from 'ngx-image-cropper';
import { SafePipe } from './safe.pipe';
import { DetailComponent } from './detail/detail.component';
import { ArtigoResolve } from './artigo.resolve';
import { ListaAdminComponent } from './lista-admin/lista-admin.component';
import { EditComponent } from './edit/edit.component';
import { ArtigoEditGuard } from './artigo.edit.guard';

@NgModule({
  imports: [
    CommonModule,
    ArtigoRoutingModule,
    RouterModule,
    NgxSpinnerModule,
    ReactiveFormsModule,
    HttpClientModule,
    ImageCropperComponent,
    ModalModule.forRoot(),
    NgxMaskDirective,
    NgxMaskPipe,
    ToastrModule.forRoot({
      timeOut: 3000,
      positionClass: 'toast-bottom-right',
      preventDuplicates: true,
      maxOpened: 0,
      progressBar: true,
      progressAnimation: 'decreasing'
    }),
    FormsModule,
    NgxPaginationModule
  ],
  declarations: [
    ArtigoAppComponent,
    AddComponent,
    EditComponent,
    ListaAdminComponent,
    DetailComponent,
    ListaComponent,
    SafePipe
  ],
  providers: [
    ArtigoService,
    ArtigoResolve,
    ArtigoEditGuard,
    ArtigoGuard,
    provideNgxMask()
  ]
})
export class ArtigoModule { }
