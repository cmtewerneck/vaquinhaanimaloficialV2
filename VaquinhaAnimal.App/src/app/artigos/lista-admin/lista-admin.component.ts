import { Component, OnInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { ArtigoService } from '../artigo.service';
import { Artigo } from '../model/Artigo';
import { NgxSpinnerService } from 'ngx-spinner';

@Component({
  selector: 'app-lista-admin',
  templateUrl: './lista-admin.component.html'
})
export class ListaAdminComponent implements OnInit {

  artigos!: Artigo[];
  errors!: any[];
  artigoId!: string;
  
  constructor(private artigoService: ArtigoService, private toastr: ToastrService, private spinner: NgxSpinnerService) {}

  ngOnInit(): void {this.ObterTodos();}

  ObterTodos() {
    this.spinner.show();

    this.artigoService.obterTodos().subscribe(
      (_artigos: Artigo[]) => {
      this.artigos = _artigos;
      this.spinner.hide();
    }, error => {
      console.log(error);
    });
  }

   processarSucesso(response: any) {
    this.errors = [];
    this.toastr.success('Ar iniciada!', 'Sucesso!');
    this.ObterTodos();
    this.artigoId = "";
    this.spinner.hide();
  }
  
  processarFalha(fail: any) {
    this.errors = fail.error.errors;
    this.toastr.error('Ocorreu um erro!', 'Opa :(');
    this.artigoId = "";
    this.spinner.hide();
  }

}
