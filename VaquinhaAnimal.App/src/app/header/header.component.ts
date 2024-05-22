import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Subscription } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { User } from '../auth/User';
import { LocalStorageUtils } from '../_utils/localStorage';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html'
})
export class HeaderComponent implements OnInit, OnDestroy {
  LocalStorage = new LocalStorageUtils();
  token!: string;
  nomeUsuario!: string;
  emailUsuario!: string;
  userId!: string;
  userLogado: boolean = false;
  userSubscription!: Subscription;
  habilitarMenu: boolean = false;

  constructor(private toastr: ToastrService, private router: Router, private authService: AuthService) {}

  ngOnInit() {
    this.userSubscription = this.authService.userChanged.subscribe(success => {
      if(success != null){
        this.emailUsuario = success.userToken.email;
        this.userId = success.userToken.id;
        this.nomeUsuario = success.userToken.nome;
        this.userLogado = true;
        this.habilitarMenu = true;
      }
      else {
        this.emailUsuario = "";
        this.userId = "";
        this.nomeUsuario = "";
        this.userLogado = false;
      }
    });
  }

  ngOnDestroy() {
    this.userSubscription.unsubscribe();
  }

  reloadPage(){
    window.location.reload();
  }

  usuarioLogado() {
    this.token = this.LocalStorage.obterTokenUsuarioSession();
    let usuario = this.LocalStorage.obterUsuarioSession();
    this.userId = usuario.id;

    if (usuario) { 
      this.nomeUsuario = usuario.nome; 
      this.emailUsuario = usuario.email;
    }

    if(this.token != null){
      this.userLogado = true;
    } else { this.userLogado = false;}
  }

  logout() {
    this.authService.logout();
    
    this.toastr.info('Você foi desconectado', 'LOGOUT', {
      closeButton: true,
      progressBar: true,
      timeOut: 2000
    });

    this.router.navigate(['']);
  }

}