import { NgModule } from '@angular/core';
import { CommonModule, CurrencyPipe, registerLocaleData } from '@angular/common';
import { AdocoesRoutingModule } from './adocao.routing';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { ToastrModule } from 'ngx-toastr';
import { IConfig, NgxMaskDirective, NgxMaskPipe, provideNgxMask } from 'ngx-mask'
import { NgxSpinnerModule } from 'ngx-spinner';
import { AdocaoComponent } from './adocao.component';
import { AdocaoGuard } from './adocao.guard';
import { CriarComponent } from './criar/criar.component';
import { AdocaoService } from './adocao.service';
import { LOCALE_ID, DEFAULT_CURRENCY_CODE } from '@angular/core';
import localePt from '@angular/common/locales/pt';
import { CurrencyMaskModule } from 'ng2-currency-mask';
import { AdocaoResolve } from './adocao.resolve';
import { ModalModule } from 'ngx-bootstrap/modal';
import { SafePipe } from './safe.pipe';
import { TipoPetPipe } from './tipo_pet.pipe';
import { BoolPipe } from './bool_pipe';
import { ListarTodasComponent } from './listar-todas/listar-todas.component';
import { DetailComponent } from './detail/detail.component';
import { FaixaEtariaPipe } from './faixa_etaria.pipe';
import { TipoAnunciantePipe } from './tipo_anunciante.pipe';
import { MeusPetsComponent } from './meus-pets/meus-pets.component';
registerLocaleData(localePt);

@NgModule({
  imports: [
    CommonModule,
    AdocoesRoutingModule,
    RouterModule,
    NgxSpinnerModule,
    CurrencyMaskModule,
    ReactiveFormsModule,
    NgxMaskDirective,
    NgxMaskPipe,
    ModalModule.forRoot(),
    HttpClientModule,
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
    AdocaoComponent,
    ListarTodasComponent,
    DetailComponent,
    MeusPetsComponent,
    CriarComponent,
    TipoPetPipe,
    TipoAnunciantePipe,
    FaixaEtariaPipe,
    BoolPipe,
    SafePipe
  ],
  providers: [
    AdocaoGuard,
    AdocaoService,
    AdocaoResolve,
    CurrencyPipe,
    provideNgxMask(),
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
export class AdocoesModule { }
