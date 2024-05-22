import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../auth.service';
import { PagarmeCard } from '../User';

@Component({
  selector: 'app-add-card',
  templateUrl: './addCard.component.html'
})
export class AddCardComponent implements OnInit {

  errors: any[] = [];
  cartaoForm!: FormGroup;
  cartao!: PagarmeCard;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private spinner: NgxSpinnerService, 
    private router: Router,
    private toastr: ToastrService) { }
    
    ngOnInit(): void {
      this.cartaoForm = this.fb.group({
        number: ['', Validators.required],
        holder_name: ['', Validators.required],
        holder_document: ['', Validators.required],
        exp_month: ['', Validators.required],
        exp_year: ['', Validators.required],
        cvv: ['', Validators.required],
        brand: ['', Validators.required],
        billing_address: this.fb.group({
          line_1: ['', Validators.required],
          line_2: [''],
          state: ['', Validators.required],
          city: ['', Validators.required],
          country: ['', Validators.required],
          zip_code: ['', Validators.required]
          })
      });
    }
    
    adicionarCartao() {
      this.spinner.show();

      if (this.cartaoForm.dirty && this.cartaoForm.valid) {
        this.cartao = Object.assign({}, this.cartao, this.cartaoForm.value);
        
        this.authService.novoCartao(this.cartao)
        .subscribe(
          sucesso => { this.processarSucesso(sucesso) },
          falha => { this.processarFalha(falha) }
          );
        }
      }
      
      processarSucesso(response: any) {
        this.spinner.hide();
        this.cartaoForm.reset();
        this.errors = [];
        
        let toast = this.toastr.success('Cartão cadastrado com sucesso!', 'Sucesso!');
        if (toast) {
          toast.onHidden.subscribe(() => {
            this.router.navigate(['auth/wallet']);
          });
        }
      }
      
      processarFalha(fail: any) {
        this.spinner.hide();
        this.errors = fail.error.errors;
        this.toastr.error('Ocorreu um erro!', 'Opa :(');
      }

      resetForm(){
        this.cartaoForm.reset();
      }
}
