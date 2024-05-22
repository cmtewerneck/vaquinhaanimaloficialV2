import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AuthComponent } from './auth.component';
import { AuthResolve } from './auth.resolve';
import { EditPasswordComponent } from './editPassword/editPassword.component';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { ResetPasswordComponent } from './resetPassword/resetPassword.component';
import { ResetPasswordUserComponent } from './resetPasswordUser/resetPasswordUser.component';
import { AddCardComponent } from './wallet/addCard.component';
import { MyWalletComponent } from './wallet/myWallet.component';
import { AuthGuard } from './auth.guard';
import { EmailConfirmationComponent } from './emailConfirmation/emailConfirmation.component';

const authRouterConfig: Routes = [
    {
        path: '', component: AuthComponent,
        children: [
            { path: 'login', component: LoginComponent },
            { path: 'register', component: RegisterComponent, canDeactivate: [AuthGuard] },
            { path: 'edit-password', component: EditPasswordComponent },
            { path: 'reset-password', component: ResetPasswordComponent },
            { path: 'reset-password-user/:username/:token', component: ResetPasswordUserComponent },
            { path: 'email-confirmation/:username/:token', component: EmailConfirmationComponent },
            { path: 'add-card', component: AddCardComponent },
            { path: 'wallet', component: MyWalletComponent }
        ]
    }
];

@NgModule({
    imports: [
        RouterModule.forChild(authRouterConfig)
    ],
    exports: [RouterModule]
})
export class AuthRoutingModule { }