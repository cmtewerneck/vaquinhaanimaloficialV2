import { Component, Inject, OnInit } from '@angular/core';
import { Campanha } from '../model/Campanha';
import { PagedResult } from 'src/app/_utils/pagedResult';
import { environment } from 'src/environments/environment';
import { CampanhaService } from '../campanha.service';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import * as moment from 'moment';
import { DOCUMENT } from '@angular/common';

@Component({
  selector: 'app-listar-campanhas',
  templateUrl: './listar-todas.component.html'
})
export class ListarTodasComponent implements OnInit {

  campanhas!: Campanha[];
  campanhasPaginado!: PagedResult<Campanha>;
  imagens: string = environment.imagensUrl;
  paginasPaginador: number[] = [];
  numeroPaginas!: number;

  // PAGINAÇÃO
  pageSize: number = 9;
  pageNumber: number = 1;

  constructor(private campanhaService: CampanhaService,
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
    this.campanhaService.obterTodosPaginado(this.pageSize, this.pageNumber).subscribe(
      (_campanhas: PagedResult<Campanha>) => {
        this.campanhasPaginado = _campanhas;
        this.numeroDePaginas();

        this.campanhasPaginado.data.forEach(campanha => {
          let percentual = (campanha.total_arrecadado! / campanha.valor_desejado) * 100;
          campanha.percentual_arrecadado = Math.trunc(percentual);

          let inicio = moment();
          let termino = moment(campanha.data_encerramento);
          let duracao_restante = termino.diff(inicio, 'days');
          campanha.restam = duracao_restante;

        });
        this.spinner.hide();
      }, error => {
        this.spinner.hide();
        this.toastr.error("Erro de carregamento!");
        console.log(error);
      });
  }

  getWidth(percentual: number): any {
    var x = percentual + '%';

    if (percentual > 100) {
      return '100%';
    }

    return x;
  }

  numeroDePaginas() {
    let res = Math.ceil(this.campanhasPaginado.totalRecords / 9);

    this.numeroPaginas = res;

    for (let i = 1; i <= res; i++) {
      this.paginasPaginador.push(i);
    }

    console.log(this.paginasPaginador);
  }

}