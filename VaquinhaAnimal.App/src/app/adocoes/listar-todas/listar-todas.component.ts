import { Component, Inject, OnInit } from '@angular/core';
import { Adocao } from '../model/Adocao';
import { PagedResult } from 'src/app/_utils/pagedResult';
import { environment } from 'src/environments/environment';
import { AdocaoService } from '../adocao.service';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { DOCUMENT } from '@angular/common';

@Component({
  selector: 'app-listar-campanhas',
  templateUrl: './listar-todas.component.html'
})
export class ListarTodasComponent implements OnInit {

  adocoes!: Adocao[];
  adocoesPaginado!: PagedResult<Adocao>;
  imagens: string = environment.imagensUrl;
  paginasPaginador: number[] = [];
  numeroPaginas!: number;

  // PAGINAÇÃO
  pageSize: number = 9;
  pageNumber: number = 1;

  constructor(private adocaoService: AdocaoService,
              private spinner: NgxSpinnerService,
              private toastr: ToastrService,
              @Inject(DOCUMENT) private _document: any) { }

  ngOnInit() {
    this.spinner.show();

    this.ObterTodosPaginado();

    var window = this._document.defaultView;
    window.scrollTo(0, 0);
  }

  pageChanged(event: any) {
    this.pageNumber = event;
    this.paginasPaginador = [];
    this.ObterTodosPaginado();
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

  ObterTodosPaginado() {
    this.adocaoService.obterTodosPaginado(this.pageSize, this.pageNumber).subscribe(
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

  numeroDePaginas() {
    let res = Math.ceil(this.adocoesPaginado.totalRecords / 9);

    this.numeroPaginas = res;

    for (let i = 1; i <= res; i++) {
      this.paginasPaginador.push(i);
    }

    console.log(this.paginasPaginador);
  }

}