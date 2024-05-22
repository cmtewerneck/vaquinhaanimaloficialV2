import { Component, Inject, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { environment } from 'src/environments/environment';
import { AdocaoService } from '../adocao.service';
import { Adocao } from '../model/Adocao';
import { NgxSpinnerService } from 'ngx-spinner';
import { LocalStorageUtils } from 'src/app/_utils/localStorage';
import { DOCUMENT } from '@angular/common';

@Component({
  selector: 'app-detail',
  templateUrl: './detail.component.html'
})
export class DetailComponent implements OnInit {
  
  
  // VARIAVEIS GERAIS
  userLogado: boolean = false; 
  localStorage = new LocalStorageUtils; 
  adocao!: Adocao; 
  imagens: string = environment.imagensUrl; 
  errors: any[] = []; 
  
  constructor(
    private route: ActivatedRoute,
    private adocaoService: AdocaoService,
    private toastr: ToastrService,
    private spinner: NgxSpinnerService, 
    @Inject(DOCUMENT) private _document: any) {this.adocao = this.route.snapshot.data['adocao'];}
    
    ngOnInit(): void {
      var window = this._document.defaultView;
      window.scrollTo(0, 0);

      this.usuarioLogado();
    }

    usuarioLogado(){
      let userToken = this.localStorage.obterTokenUsuarioSession();
      if(userToken != null){
        this.userLogado = true;
      } else { this.userLogado = false }
    }

    
}