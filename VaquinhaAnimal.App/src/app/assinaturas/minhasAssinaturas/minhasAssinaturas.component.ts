import { Component, Inject, OnInit, TemplateRef } from '@angular/core';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { PagarmeResponse } from 'src/app/auth/User';
import { AssinaturaService } from '../assinatura.service';
import { Assinatura } from '../model/Assinatura';
import { DOCUMENT } from '@angular/common';

@Component({
  selector: 'app-minhas-assinaturas',
  templateUrl: './minhasAssinaturas.component.html'
})
export class MinhasAssinaturasComponent implements OnInit {
  
  assinaturas!: PagarmeResponse<Assinatura>;
  assinatura!: Assinatura;
  errors!: any[];
  assinaturaId!: string;

  // PAGINAÇÃO
  pageSize: number = 10;
  pageNumber: number = 1;
  paginasPaginador: number[] = [];
  numeroPaginas!: number;

  constructor(
    private assinaturaService: AssinaturaService, 
    private spinner: NgxSpinnerService, 
    @Inject(DOCUMENT) private _document: any,
    private toastr: ToastrService) {}

  ngOnInit() {
    this.spinner.show();

    this.ObterTodos();

    var window = this._document.defaultView;
    window.scrollTo(0, 0);
  }

  salvarId(id: string){
    this.assinaturaId = id;
  }

  limparId(){
    this.assinaturaId = '';
  }

  ObterTodos() {
    this.assinaturaService.obterMinhasAssinaturas(this.pageSize, this.pageNumber).subscribe(
      (_assinaturas: PagarmeResponse<Assinatura>) => {
      this.assinaturas = _assinaturas;
      this.numeroDePaginas();
      
      this.spinner.hide();
    }, error => {
      this.spinner.hide();
        this.toastr.error(`Erro de carregamento: ${error.error.errors}`);
        console.log(error);
    });
  }

  cancelarAssinatura() {
    this.spinner.show();

    this.assinaturaService.cancelarAssinatura(this.assinaturaId)
    .subscribe(
      assinatura => { 
        this.processarSucesso(assinatura) 
      },
      falha => { this.processarFalha(falha) }
      )
   }

  processarSucesso(response: any) {
    this.spinner.hide();
    this.errors = [];
    this.toastr.success('Assinatura cancelada com sucesso!', 'Sucesso!');
    this.ObterTodos();
    this.assinaturaId = "";
  }
  
  processarFalha(fail: any) {
    this.spinner.hide();
    this.errors = fail.error.errors;
    this.toastr.error('Ocorreu um erro!', 'Opa :(');
    this.assinaturaId = "";
  }

  pageChanged(event: any) {
    this.pageNumber = event;
    this.paginasPaginador = [];
    this.ObterTodos();
    window.scrollTo(0, 0);
  }

  previousOrNext(order: string) {
    if (order == "previous" && this.pageNumber > 1) {
      this.pageNumber = this.pageNumber - 1;
    } else if (order == "next" && this.pageNumber < this.numeroPaginas) {
      this.pageNumber = this.pageNumber + 1;
    }
    this.paginasPaginador = [];
    this.ObterTodos();
  }

  numeroDePaginas() {
    this.paginasPaginador = [];
    let res = Math.ceil(this.assinaturas.paging.total / 10);

    this.numeroPaginas = res;

    for (let i = 1; i <= res; i++) {
      this.paginasPaginador.push(i);
    }

    console.log(this.paginasPaginador);
  }

}