import { Component, Inject, OnInit } from '@angular/core';
import { Adocao } from '../model/Adocao';
import { AdocaoService } from '../adocao.service';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { DOCUMENT } from '@angular/common';
import { PagedResult } from 'src/app/_utils/pagedResult';

@Component({
  selector: 'app-meus-pets',
  templateUrl: './meus-pets.component.html'
})
export class MeusPetsComponent implements OnInit {

  adocoes!: Adocao[];
  adocoesPaginado!: PagedResult<Adocao>;
  adocao!: Adocao;
  errors!: any[];
  adocaoId!: string;

  // PAGINAÇÃO
  pageSize: number = 10;
  pageNumber: number = 1;
  paginasPaginador: number[] = [];
  numeroPaginas!: number;
  
  constructor(private adocaoService: AdocaoService, private spinner: NgxSpinnerService, private toastr: ToastrService, @Inject(DOCUMENT) private _document: any) {}

  ngOnInit() {
    this.spinner.show();

    this.ObterTodosPaginado();

    var window = this._document.defaultView;
    window.scrollTo(0, 0);
  }

  ObterTodosPaginado() {
    this.adocaoService.obterMinhasAdocoesPaginado(this.pageSize, this.pageNumber).subscribe(
      (_adocoes: PagedResult<Adocao>) => {
        this.adocoesPaginado = _adocoes;
        this.numeroDePaginas();

        this.spinner.hide();
      }, error => {
        this.spinner.hide();
        this.toastr.error("Erro de carregamento!");
        console.log(error);
      });
  }

  pageChanged(event: any) {
    this.pageNumber = event;
    this.paginasPaginador = [];
    this.ObterTodosPaginado();
    window.scrollTo(0, 0);
  }

  previousOrNext(order: string) {
    if (order == "previous" && this.pageNumber > 1) {
      this.pageNumber = this.pageNumber - 1;
    } else if (order == "next" && this.pageNumber < this.numeroPaginas) {
      this.pageNumber = this.pageNumber + 1;
    }
    this.paginasPaginador = [];
    this.ObterTodosPaginado();
  }

  numeroDePaginas() {
    this.paginasPaginador = [];
    let res = Math.ceil(this.adocoesPaginado.totalRecords / 10);

    this.numeroPaginas = res;

    for (let i = 1; i <= res; i++) {
      this.paginasPaginador.push(i);
    }

    console.log(this.paginasPaginador);
  }

  salvarId(id: string){
    this.adocaoId = id;
  }

  limparId(){
    this.adocaoId = '';
  }

  deletarAdocao() {
    this.spinner.show();

    this.adocaoService.excluirAdocao(this.adocaoId)
    .subscribe(
      adocao => { 
        this.processarSucessoExclusao(adocao) 
      },
      falha => { this.processarFalha(falha) }
      )
  }

  marcarAdotado(){
    this.spinner.show();

    this.adocaoService.marcarAdotado(this.adocaoId)
    .subscribe(
      adocao => { 
        this.processarSucessoAdotado(adocao) 
      },
      falha => { this.processarFalha(falha) }
      )
  }

  marcarListado(){
    this.spinner.show();

    this.adocaoService.marcarListado(this.adocaoId)
    .subscribe(
      adocao => { 
        this.processarSucessoListado(adocao) 
      },
      falha => { this.processarFalha(falha) }
      )
  }

  processarSucessoExclusao(response: any) {
    this.spinner.hide();
    this.errors = [];
    this.toastr.success('Pet excluído com sucesso!', 'Excluído!');
    this.pageNumber = 1;
    this.ObterTodosPaginado();
    this.adocaoId = "";
  }

  processarSucessoAdotado(response: any) {
    this.spinner.hide();
    this.errors = [];
    this.toastr.success('Pet marcado como adotado.', 'Sucesso!');
    this.pageNumber = 1;
    this.ObterTodosPaginado();
    this.adocaoId = "";
  }

  processarSucessoListado(response: any) {
    this.spinner.hide();
    this.errors = [];
    this.toastr.success('Pet marcado como listado.', 'Sucesso!');
    this.pageNumber = 1;
    this.ObterTodosPaginado();
    this.adocaoId = "";
  }

  processarFalha(fail: any) {
    this.spinner.hide();
    this.errors = fail.error.errors;
    this.toastr.error('Ocorreu um erro!', 'Opa :(');
    this.adocaoId = "";
  }
  
}