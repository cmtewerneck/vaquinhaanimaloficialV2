import { NgModule } from '@angular/core';
import { CommonModule, CurrencyPipe, registerLocaleData } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { ToastrModule } from 'ngx-toastr';
import { NgxMaskDirective, NgxMaskPipe, NgxMaskService, provideNgxMask } from 'ngx-mask';
import { NgxSpinnerModule } from 'ngx-spinner';
import { DoacoesComponent } from './doacao.component';
import { DoacaoService } from './doacao.service';
import { LOCALE_ID, DEFAULT_CURRENCY_CODE } from '@angular/core';
import localePt from '@angular/common/locales/pt';

import { DoacoesRoutingModule } from './doacao.routing';
import { MinhasDoacoesComponent } from './minhas-doacoes/minhas-doacoes.component';
import { DoacaoGuard } from './doacao.guard';
import { FormaPagamentoPipe } from './forma.pagamento.pipe';
import { StatusCampanhaPipe } from '../campanhas/status.campanha.pipe';
import { StatusDoacaoPipe } from './status.doacao.pipe';
registerLocaleData(localePt);

@NgModule({
  imports: [
    CommonModule,
    DoacoesRoutingModule,
    RouterModule,
    NgxSpinnerModule,
    ReactiveFormsModule,
    HttpClientModule,
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
    FormsModule
  ],
  declarations: [
    DoacoesComponent,
    FormaPagamentoPipe,
    StatusDoacaoPipe,
    MinhasDoacoesComponent
  ],
  providers: [
    DoacaoService,
    DoacaoGuard,
    provideNgxMask(),
    
    CurrencyPipe,
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
export class DoacoesModule { }
