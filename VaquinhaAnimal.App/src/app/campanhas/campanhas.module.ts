import { NgModule } from '@angular/core';
import { CommonModule, CurrencyPipe, registerLocaleData } from '@angular/common';
import { CampanhasRoutingModule } from './campanhas.routing';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { ToastrModule } from 'ngx-toastr';
import { NgxSpinnerModule } from 'ngx-spinner';
import { CampanhasComponent } from './campanhas.component';
import { ListarTodasComponent } from './listar-todas/listar-todas.component';
import { CampanhaGuard } from './campanha.guard';
import { CriarComponent } from './criar/criar.component';
import { LoadImageService } from 'ngx-image-cropper';
import { CampanhaService } from './campanha.service';
import { LOCALE_ID, DEFAULT_CURRENCY_CODE } from '@angular/core';
import localePt from '@angular/common/locales/pt';
import { SafePipe } from './safe.pipe';
import { TagCampanhaPipe } from './tag_campanha.pipe';
import { MinhasCampanhasComponent } from './minhas-campanhas/minhas-campanhas.component';
import { StatusCampanhaPipe } from './status.campanha.pipe';
import { DetailComponent } from './detail/detail.component';
import { CampanhaResolve } from './campanha.resolve';
import { ModalModule } from 'ngx-bootstrap/modal';
import { ListaAdminComponent } from './lista-admin/listaAdmin.component';
import { CampanhaAdminGuard } from './campanha.admin.guard';
import { CampanhaEditGuard } from './campanha.edit.guard';
import { EditComponent } from './edit/edit.component';
import { TextFormatPipe } from './textFormat.pipe';
import { NgxMaskDirective, NgxMaskPipe, NgxMaskService, provideNgxMask } from 'ngx-mask';
import { CURRENCY_MASK_CONFIG, CurrencyMaskConfig, CurrencyMaskModule } from 'ng2-currency-mask';
registerLocaleData(localePt);

export const CustomCurrencyMaskConfig: CurrencyMaskConfig = {
  align: "right",
  allowNegative: true,
  decimal: ",",
  precision: 2,
  prefix: "R$ ",
  suffix: "",
  thousands: "."
};

@NgModule({
  imports: [
    CommonModule,
    CampanhasRoutingModule,
    RouterModule,
    NgxSpinnerModule,
    ReactiveFormsModule,
    ModalModule,
    HttpClientModule,
    NgxMaskDirective,
    NgxMaskPipe,
    CurrencyMaskModule,
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
    CampanhasComponent,
    ListarTodasComponent,
    ListaAdminComponent,
    EditComponent,
    CriarComponent,
    DetailComponent,
    MinhasCampanhasComponent,
    SafePipe,
    TextFormatPipe,
    StatusCampanhaPipe,
    TagCampanhaPipe
  ],
  exports: [
    TagCampanhaPipe
  ],
  providers: [
    CampanhaGuard,
    CampanhaEditGuard,
    CampanhaAdminGuard,
    CampanhaService,
    CampanhaResolve,
    CurrencyPipe,
    NgxMaskService,
    provideNgxMask(),
    { provide: CURRENCY_MASK_CONFIG, useValue: CustomCurrencyMaskConfig },
    {
      provide: LOCALE_ID,
      useValue: "pt-BR"
    },
    {
      provide:  DEFAULT_CURRENCY_CODE,
      useValue: 'BRL'
    }
  ]
})
export class CampanhasModule { }
