import { Component, Inject, OnInit, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { LocalStorageUtils } from 'src/app/_utils/localStorage';
import { Artigo } from '../model/Artigo';
import { environment } from 'src/environments/environment';
import { DOCUMENT } from '@angular/common';

@Component({
  selector: 'app-detail',
  templateUrl: './detail.component.html',
  encapsulation: ViewEncapsulation.None,
})
export class DetailComponent implements OnInit {
  
  artigo: Artigo;
  errors: any[] = [];
  userLogado: boolean = false;
  localStorage = new LocalStorageUtils;
  imagens: string = environment.imagensUrl;
  
  constructor(
    private route: ActivatedRoute, private router: Router,@Inject(DOCUMENT) private _document: any) {this.artigo = this.route.snapshot.data['artigo'];}
    
    ngOnInit(): void {
      this.usuarioLogado();

      var window = this._document.defaultView;
      window.scrollTo(0, 0);
    }

    usuarioLogado(){
      let userToken = this.localStorage.obterTokenUsuarioSession();
      console.log("USUARIO LOGADO: " + userToken);

      if(userToken != null){
        this.userLogado = true;
      } else { this.userLogado = false }
    }

}