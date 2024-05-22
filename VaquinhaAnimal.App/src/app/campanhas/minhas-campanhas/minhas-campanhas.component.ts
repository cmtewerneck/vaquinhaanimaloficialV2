import { Component, Inject, OnInit } from '@angular/core';
import { Campanha } from '../model/Campanha';
import { CampanhaService } from '../campanha.service';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { DOCUMENT } from '@angular/common';
import { PagedResult } from 'src/app/_utils/pagedResult';

@Component({
  selector: 'app-minhas-campanhas',
  templateUrl: './minhas-campanhas.component.html'
})
export class MinhasCampanhasComponent implements OnInit {

  campanhas!: Campanha[];
  campanhasPaginado!: PagedResult<Campanha>;
  campanha!: Campanha;
  errors!: any[];
  campanhaId!: string;

  // PAGINAÇÃO
  pageSize: number = 10;
  pageNumber: number = 1;
  paginasPaginador: number[] = [];
  numeroPaginas!: number;
  
  constructor(private campanhaService: CampanhaService, private spinner: NgxSpinnerService, private toastr: ToastrService, @Inject(DOCUMENT) private _document: any) {}

  ngOnInit() {
    this.spinner.show();

    this.ObterTodosPaginado();

    var window = this._document.defaultView;
    window.scrollTo(0, 0);
  }

  baixarRelatorio() {
    this.campanhaService.baixarRelatorio(this.campanhaId).subscribe(res => {
        console.log(res);
    }, error => {
        console.log(error);
    });
}

  ObterTodos() {
    this.campanhaService.obterMinhasCampanhas().subscribe(
      (_campanhas: Campanha[]) => {
      this.campanhas = _campanhas;

      this.spinner.hide();

    }, error => {
        this.spinner.hide();
        this.toastr.error(`Erro de carregamento: ${error.error.errors}`);
        console.log(error);
    });
  }

  ObterTodosPaginado() {
    this.campanhaService.obterMinhasCampanhasPaginado(this.pageSize, this.pageNumber).subscribe(
      (_campanhas: PagedResult<Campanha>) => {
        this.campanhasPaginado = _campanhas;
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
    let res = Math.ceil(this.campanhasPaginado.totalRecords / 10);

    this.numeroPaginas = res;

    for (let i = 1; i <= res; i++) {
      this.paginasPaginador.push(i);
    }

    console.log(this.paginasPaginador);
  }

  salvarId(id: string){
    this.campanhaId = id;
  }

  limparId(){
    this.campanhaId = '';
  }

  enviarParaAnalise() {
    this.spinner.show();

    this.campanhaService.enviarParaAnalise(this.campanhaId)
    .subscribe(
      campanha => { 
        this.processarSucesso(campanha) 
      },
      falha => { this.processarFalha(falha) }
      )
  }

  pararCampanha() {
    this.spinner.show();
    
    this.campanhaService.pararCampanha(this.campanhaId)
    .subscribe(
      campanha => { 
        this.processarSucessoCampanhaFinalizada(campanha) 
      },
      falha => { this.processarFalha(falha) }
      )
  }

  deletarCampanha() {
    this.spinner.show();

    this.campanhaService.excluirCampanha(this.campanhaId)
    .subscribe(
      campanha => { 
        this.processarSucessoExclusao(campanha) 
      },
      falha => { this.processarFalha(falha) }
      )
  }

  processarSucesso(response: any) {
    this.spinner.hide();
    this.errors = [];
    this.toastr.success('Campanha enviada para análise!', 'Sucesso!');
    this.ObterTodosPaginado();
    this.campanhaId = "";
  }

  processarSucessoExclusao(response: any) {
    this.spinner.hide();
    this.errors = [];
    this.toastr.success('Campanha excluída com sucesso!', 'Sucesso!');
    this.pageNumber = 1;
    this.ObterTodosPaginado();
    this.campanhaId = "";
  }

  processarSucessoCampanhaFinalizada(response: any) {
    this.spinner.hide();
    this.errors = [];
    this.toastr.success('Campanha finalizada com sucesso!', 'Sucesso!');
    this.ObterTodosPaginado();
    this.campanhaId = "";
  }
  
  processarFalha(fail: any) {
    this.spinner.hide();
    this.errors = fail.error.errors;
    this.toastr.error('Ocorreu um erro!', 'Opa :(');
    this.campanhaId = "";
  }
  
}