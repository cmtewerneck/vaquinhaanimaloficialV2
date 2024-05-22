import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../auth.service';
import { UserPassword } from '../UserPassword';
import { DOCUMENT } from '@angular/common';

@Component({
  selector: 'app-edit-password',
  templateUrl: './editPassword.component.html'
})
export class EditPasswordComponent implements OnInit {
  
  editPasswordForm!: FormGroup;
  errors: any[] = [];
  user!: UserPassword;
  passwordInputType: string = "password";
  
  constructor(
    private router: Router, 
    private toastr: ToastrService,
    private authService: AuthService, 
    private spinner: NgxSpinnerService,
    @Inject(DOCUMENT) private _document: any,
    public fb: FormBuilder) { }
    
    ngOnInit() {
      this.createForm();
      
      var window = this._document.defaultView;
      window.scrollTo(0, 0);
    }
    
    createForm(){
      this.editPasswordForm = this.fb.group({
        currentPassword: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(12)]],
        newPassword: ['', [Validators.required, Validators.minLength(6),Validators.maxLength(12)]],
        confirmPassword: ['', [Validators.required, Validators.minLength(6),Validators.maxLength(12)]]
      });
    }
    
    editPassword() {
      this.spinner.show();
      
      if (this.editPasswordForm.dirty && this.editPasswordForm.valid) {
        this.user = Object.assign(this.editPasswordForm.value);
        console.log(this.user);
        
        this.authService.editUserPassword(this.user).subscribe(
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
      this.editPasswordForm.reset();
      this.errors = [];
      
      let toast = this.toastr.success('Senha atualizada com sucesso!', 'Sucesso!');
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

    onCheckboxChange(event: any) {
      if(event.target.checked){
        this.passwordInputType = "text";
      } else this.passwordInputType = "password";
    }
    
    resetForm(){
      this.editPasswordForm.reset();
    }
      
}
    