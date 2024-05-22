import { Component, OnInit, TemplateRef } from '@angular/core';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../auth.service';
import { ListCard, PagarmeCard, PagarmeCardResponse, PagarmeResponse } from '../User';

@Component({
  selector: 'app-my-wallet',
  templateUrl: './myWallet.component.html'
})
export class MyWalletComponent implements OnInit {
  
  cartoes!: PagarmeResponse<PagarmeCardResponse>;
  cartao!: PagarmeCard;
  errors!: any[];
  errorMessage!: string;
  cartaoId!: string;

  constructor(
    private authService: AuthService, 
    private spinner: NgxSpinnerService,
    private toastr: ToastrService) {}

  ngOnInit() {
    this.spinner.show();
    this.ObterTodos();
  }

  salvarId(id: string){
    this.cartaoId = id;
  }

  limparId(){
    this.cartaoId = '';
  }

  ObterTodos() {
    this.authService.obterMeusCartoes().subscribe(
      (_cartoes: PagarmeResponse<PagarmeCardResponse>) => {
      this.cartoes = _cartoes;
      this.spinner.hide();
    }, error => {
        this.spinner.hide();
        this.toastr.error(`Erro de carregamento: ${error.error.errors}`);
        console.log(error);
    });
  }

  deletarCartao() {
    this.spinner.show();
    this.authService.deletarCartao(this.cartaoId)
    .subscribe(
      campanha => { 
        this.processarSucesso(campanha) 
      },
      falha => { this.processarFalha(falha) }
      )
   }

   processarSucesso(response: any) {
    this.spinner.hide();
    this.errors = [];
    this.toastr.success('Cartão removido com sucesso!', 'Sucesso!');
    this.ObterTodos();
    this.cartaoId = "";
  }
  
  processarFalha(fail: any) {
    this.spinner.hide();
    this.errors = fail.error.errors;
    this.toastr.error('Ocorreu um erro!', 'Opa :(');
    this.cartaoId = "";
  }

}