import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../auth.service';
import { ResetPassword } from '../User';
import { DOCUMENT } from '@angular/common';

@Component({
  selector: 'app-reset-password',
  templateUrl: './resetPassword.component.html'
})
export class ResetPasswordComponent implements OnInit {
  
  resetPasswordForm!: FormGroup;
  errors: any[] = [];
  user!: ResetPassword;
  
  constructor(
    private router: Router, 
    private toastr: ToastrService,
    private authService: AuthService, 
    private spinner: NgxSpinnerService,
    @Inject(DOCUMENT) private _document: any,
    public fb: FormBuilder) { }
    
    ngOnInit() {
      this.resetPasswordForm = this.fb.group({
        username: ['', [Validators.required, Validators.email]]
      });

      var window = this._document.defaultView;
      window.scrollTo(0, 0);
    }
    
    resetPassword() {
      this.spinner.show();

      if (this.resetPasswordForm.dirty && this.resetPasswordForm.valid) {
        this.user = Object.assign(this.resetPasswordForm.value);
        console.log(this.user);
        
        this.authService.resetPassword(this.user).subscribe(
          success => { 
            this.processarSucesso(success); 
          },
          error => {
            this.processarFalha(error);
          },
          );
          
      }
    }
      
    processarSucesso(response: any) {
      this.spinner.hide();
      this.resetPasswordForm.reset();
      this.errors = [];
      
      let toast = this.toastr.success('Link de atualização enviado por email!', 'Sucesso!');
      if (toast) {
        toast.onHidden.subscribe(() => {
          this.router.navigate(['/']);
        });
      }
    }
      
    processarFalha(fail: any) {
      this.spinner.hide();
      this.errors = fail.error.errors;
      this.toastr.error(this.errors[0]);
    }

    resetForm(){
      this.resetPasswordForm.reset();
    }
      
}    