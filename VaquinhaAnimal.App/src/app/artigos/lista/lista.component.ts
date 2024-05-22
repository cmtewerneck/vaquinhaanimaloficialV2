import { Component, Inject, OnInit } from '@angular/core';
import { ArtigoService } from '../artigo.service';
import { Artigo } from '../model/Artigo';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { environment } from 'src/environments/environment';
import { DOCUMENT } from '@angular/common';

@Component({
  selector: 'app-lista',
  templateUrl: './lista.component.html'
})
export class ListaComponent implements OnInit {

  artigos!: Artigo[];
  imagens: string = environment.imagensUrl;

  constructor(private artigoService: ArtigoService, 
              private spinner: NgxSpinnerService, 
              @Inject(DOCUMENT) private _document: any,
              private toastr: ToastrService) { }

  ngOnInit() {
    this.spinner.show();

    var window = this._document.defaultView;
    window.scrollTo(0, 0);

    this.ObterTodos();
  }

  ObterTodos() {
    this.artigoService.obterTodos().subscribe(
      (_artigos: Artigo[]) => {
      this.artigos = _artigos;

      this.spinner.hide();
    }, error => {
        this.spinner.hide();
        this.toastr.error(`Erro de carregamento: ${error.error.errors}`);
        console.log(error);
    });
  }


}