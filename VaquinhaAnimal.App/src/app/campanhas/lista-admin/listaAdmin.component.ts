import { Component, OnInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { CampanhaService } from '../campanha.service';
import { Campanha } from '../model/Campanha';
import { NgxSpinnerService } from 'ngx-spinner';

@Component({
  selector: 'app-lista-admin',
  templateUrl: './listaAdmin.component.html'
})
export class ListaAdminComponent implements OnInit {

  campanhas!: Campanha[];
  errors!: any[];
  campanhaId!: string;
  motivoReprovacao: string = "";
  
  constructor(private campanhaService: CampanhaService, private toastr: ToastrService, private spinner: NgxSpinnerService) {}

  ngOnInit(): void {this.ObterTodos();}

  ObterTodos() {
    this.spinner.show();

    this.campanhaService.obterTodos().subscribe(
      (_campanhas: Campanha[]) => {
      this.campanhas = _campanhas;
      this.spinner.hide();
    }, error => {
      console.log(error);
    });
  }

  iniciarCampanha() {
    this.spinner.show();
    
    this.campanhaService.iniciarCampanha(this.campanhaId)
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
        this.processarSucesso(campanha) 
      },
      falha => { this.processarFalha(falha) }
      )
   }

   rejeitarCampanha() {
    this.spinner.show();

    this.campanhaService.rejeitarCampanha(this.campanhaId, this.motivoReprovacao)
    .subscribe(
      campanha => { 
        this.processarSucesso(campanha) 
      },
      falha => { this.processarFalha(falha) }
      )
   }

   retornarCampanha() {
    this.spinner.show();

    this.campanhaService.retornarCampanha(this.campanhaId)
    .subscribe(
      campanha => { 
        this.processarSucesso(campanha) 
      },
      falha => { this.processarFalha(falha) }
      )
   }

   processarSucesso(response: any) {
    this.errors = [];
    this.toastr.success('Campanha iniciada!', 'Sucesso!');
    this.ObterTodos();
    this.campanhaId = "";
    this.spinner.hide();
  }
  
  processarFalha(fail: any) {
    this.errors = fail.error.errors;
    this.toastr.error('Ocorreu um erro!', 'Opa :(');
    this.campanhaId = "";
    this.spinner.hide();
  }

  salvarId(id: string){
    this.campanhaId = id;
  }

  limparId(){
    this.campanhaId = '';
  }

}
