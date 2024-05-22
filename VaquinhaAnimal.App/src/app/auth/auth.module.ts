import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthComponent } from './auth.component';
import { AuthRoutingModule } from './auth.routing';
import { RouterModule } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { ToastrModule } from 'ngx-toastr';
import { AddCardComponent } from './wallet/addCard.component';
import { MyWalletComponent } from './wallet/myWallet.component';
import { NgxMaskDirective, NgxMaskPipe, NgxMaskService, provideNgxMask } from 'ngx-mask';
import { EditPasswordComponent } from './editPassword/editPassword.component';
import { AuthResolve } from './auth.resolve';
import { NgxSpinnerModule } from 'ngx-spinner';
import { ResetPasswordComponent } from './resetPassword/resetPassword.component';
import { ResetPasswordUserComponent } from './resetPasswordUser/resetPasswordUser.component';
import { AuthGuard } from './auth.guard';
import { EmailConfirmationComponent } from './emailConfirmation/emailConfirmation.component';

@NgModule({
  imports: [
    CommonModule,
    AuthRoutingModule,
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
    AuthComponent,
    LoginComponent,
    EmailConfirmationComponent,
    AddCardComponent,
    MyWalletComponent,
    ResetPasswordComponent,
    ResetPasswordUserComponent,
    EditPasswordComponent,
    RegisterComponent
  ],
  providers: [
    //AuthService,
    AuthResolve,
    AuthGuard,
    NgxMaskService,
    provideNgxMask()
  ]
})
export class AuthModule { }
