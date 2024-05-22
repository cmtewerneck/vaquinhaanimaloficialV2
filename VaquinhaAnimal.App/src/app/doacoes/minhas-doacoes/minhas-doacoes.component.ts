import { Component, Inject, OnInit } from '@angular/core';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { DOCUMENT } from '@angular/common';
import { Doacao } from '../model/Doacao';
import { DoacaoService } from '../doacao.service';
import { PagedResult } from 'src/app/_utils/pagedResult';

@Component({
  selector: 'app-minhas-doacoes',
  templateUrl: './minhas-doacoes.component.html'
})
export class MinhasDoacoesComponent implements OnInit {

  doacoes!: Doacao[];
  doacao!: Doacao;
  doacoesPaginado!: PagedResult<Doacao>;
  errors!: any[];

  // PAGINAÇÃO
  pageSize: number = 10;
  pageNumber: number = 1;
  paginasPaginador: number[] = [];
  numeroPaginas!: number;
  
  constructor(private doacaoService: DoacaoService, 
              private spinner: NgxSpinnerService, 
              private toastr: ToastrService, 
              @Inject(DOCUMENT) private _document: any) {}

  ngOnInit() {
    this.spinner.show();

    this.ObterTodosPaginado();

    var window = this._document.defaultView;
    window.scrollTo(0, 0);
  }

  ObterTodosPaginado() {
    this.doacaoService.obterMinhasDoacoesPaginado(this.pageSize, this.pageNumber).subscribe(
      (_doacoes: PagedResult<Doacao>) => {
        this.doacoesPaginado = _doacoes;
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
    let res = Math.ceil(this.doacoesPaginado.totalRecords / 10);

    this.numeroPaginas = res;

    for (let i = 1; i <= res; i++) {
      this.paginasPaginador.push(i);
    }

    console.log(this.paginasPaginador);
  }

  exportToPdf(doacaoId: string) {
    this.doacaoService.exportToPdf(doacaoId).subscribe(response => {
        console.log(response);

        const blob = new Blob([response], { type: 'application/pdf' });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = 'Comprovante.pdf';
        link.click();
        window.URL.revokeObjectURL(url);

    }, error => {
        console.log(error);
    });
  }  

}