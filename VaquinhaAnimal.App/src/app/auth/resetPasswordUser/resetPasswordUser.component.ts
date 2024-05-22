import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../auth.service';
import { ResetPasswordUser } from '../User';

@Component({
  selector: 'app-reset-password-user',
  templateUrl: './resetPasswordUser.component.html'
})
export class ResetPasswordUserComponent implements OnInit {
  
  resetPasswordUserForm!: FormGroup;
  errors: any[] = [];
  user!: ResetPasswordUser;
  token!: string;
  username!: string;
  passwordInputType: string = "password";
  
  constructor(
    private router: Router, 
    private toastr: ToastrService,
    private authService: AuthService, 
    private activatedRoute: ActivatedRoute,
    private spinner: NgxSpinnerService,
    public fb: FormBuilder) { 
      this.username = activatedRoute.snapshot.url[1].path; 
      this.token = activatedRoute.snapshot.url[2].path; 
    }
    
    ngOnInit() {
      this.createForm();
      this.setUrlValues();
    }

    createForm(){
      this.resetPasswordUserForm = this.fb.group({
        username: ['', [Validators.required, Validators.email]],
        newPassword: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(12)]],
        confirmPassword: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(12)]],
        token: ['', Validators.required]
      });
    }

    setUrlValues(){
      this.resetPasswordUserForm.get('username')?.setValue(this.username);
      this.resetPasswordUserForm.get('token')?.setValue(this.token);
    }

    resetPasswordUser() {
      this.spinner.show();

      if (this.resetPasswordUserForm.dirty && this.resetPasswordUserForm.valid) {
        this.user = Object.assign(this.resetPasswordUserForm.value);
        console.log(this.user);

        this.authService.resetPasswordUser(this.user).subscribe(
          success => { 
            this.processarSucesso(success); 
          },
          error => {
            this.processarFalha(error);
          },
        );

      }
    }

    onCheckboxChange(event: any) {
      if(event.target.checked){
        this.passwordInputType = "text";
      } else this.passwordInputType = "password";
    }

    resetForm(){
      this.resetPasswordUserForm.reset();
    }
  
    processarSucesso(response: any) {
      this.spinner.hide();
      this.toastr.success('Senha atualizada com sucesso.', 'Parabéns!');
      this.errors = [];
      this.router.navigate(['/']);
    }
  
    processarFalha(fail: any) {
      this.spinner.hide();
      this.errors = fail.error.errors;
      this.toastr.error(this.errors[0], 'Erro!');
    }   
      
}    