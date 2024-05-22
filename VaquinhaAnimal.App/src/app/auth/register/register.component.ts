import { Component, ElementRef, Inject, NgZone, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';
import { User } from '../User';
import { ToastrService } from 'ngx-toastr';
import { NgxSpinnerService } from 'ngx-spinner';
import { DOCUMENT } from '@angular/common';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html'
})
export class RegisterComponent implements OnInit {

  @ViewChild('divRecaptcha')
        divRecaptcha!: ElementRef<HTMLDivElement>;

    get grecaptcha(): any {
      const w = window as any;
      return w['grecaptcha'];
    }
  
  registerForm!: FormGroup;
  errors: any[] = [];
  user!: User;
  passwordInputType: string = "password";
  document_mask: string = "000.000.000-00";
  document_toggle: string = "CPF";
  key: string = "6LcbyVwnAAAAAEXUaEsI9VXbxJkFZeDmvcwoNhF5";
  
  constructor(
    private router: Router, 
    private ngZone: NgZone,
    private toastr: ToastrService,
    private authService: AuthService, 
    private spinner: NgxSpinnerService,
    @Inject(DOCUMENT) private _document: any,
    public fb: FormBuilder) { this.renderizarReCaptcha(); }
    
    ngOnInit() {
      if (localStorage.getItem('token') !== null) {
        this.router.navigate(['/index']);
      }
      this.validation();

      var window = this._document.defaultView;
      window.scrollTo(0, 0);
    }

    renderizarReCaptcha() {
      this.ngZone.runOutsideAngular(() => {
        if (!this.grecaptcha || !this.divRecaptcha) {
          setTimeout(() => {
            this.renderizarReCaptcha();
          }, 500);
  
          return;
        }

        const idElemento =
          this.divRecaptcha.nativeElement.getAttribute('id');
  
        this.grecaptcha.render(idElemento, {
          sitekey: this.key,
          callback: (response: string) => {
            this.ngZone.run(() => {
              console.log("CAPTCHA: "+response);
              this.registerForm.get('recaptcha')?.setValue(response);
            });
          },
        });
      });
    }
    
    validation() {
      this.registerForm = this.fb.group({
        recaptcha: [null, Validators.required],
        name: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(64)]],
        email: ['', [Validators.required, Validators.minLength(5), Validators.email, Validators.maxLength(64)]],
        type: ['', [Validators.required, Validators.minLength(7), Validators.maxLength(10)]],
        document: [''],
        password: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(12)]],
        confirmPassword: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(12)]],
        foto: ['']
      });
    }

    documentSelected(event: any){
      console.log(event.target.value);
      
      if(event.target.value == "individual"){
        this.document_mask = "000.000.000-00";
        this.document_toggle = "CPF";
      } else if(event.target.value == "company"){
        this.document_mask = "00.000.000/0000-00"
        this.document_toggle = "CNPJ";
      }

      this.setDocumentValidation();
    }
    
    register() {
      this.spinner.show();
      
      if (this.registerForm.dirty && this.registerForm.valid) {
        this.user = Object.assign(this.registerForm.value);
        console.log(this.user);
        
        this.authService.register(this.user).subscribe(
          success => { 
            this.processarSucesso(success); 
          },
          error => {
            this.processarFalha(error);
          },
          );
          
        }
      }

      setDocumentValidation(){
        this.registerForm.controls['document'].clearValidators();
        this.registerForm.controls['document'].setValue("");

        if (this.document_toggle == "CPF"){
          this.registerForm.controls['document'].setValidators([Validators.required, Validators.minLength(11), Validators.maxLength(11)]);
        } else if(this.document_toggle == "CNPJ"){
          this.registerForm.controls['document'].setValidators([Validators.required, Validators.minLength(14), Validators.maxLength(14)]);
        }

        this.registerForm.controls['document'].updateValueAndValidity();

        console.log(this.registerForm);
      }
      
      processarSucesso(response: any) {
        this.spinner.hide();
        this.registerForm.reset();
        //this.toastr.success('Cadastro realizado com sucesso', 'Parabéns!');
        this.toastr.success('Acesse seu e-mail e ative sua conta', 'Cadastro realizado!');
        this.errors = [];
        
        this.authService.LocalStorage.salvarDadosLocaisUsuarioSession(response);
        
        this.router.navigate(['/campanhas']);
      }
      
      processarFalha(fail: any) {
        this.spinner.hide();
        this.errors = fail.error.errors;
        
        this.toastr.error(this.errors[0], 'Erro!');
      }
      
      onCheckboxChange(event: any) {
        if(event.target.checked){
          this.passwordInputType = "text";
        } else this.passwordInputType = "password";
      }
      
      resetForm(){
        this.registerForm.reset();
      }
      
}