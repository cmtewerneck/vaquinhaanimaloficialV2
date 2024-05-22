import { DOCUMENT } from '@angular/common';
import { Component, Inject, OnInit } from '@angular/core';
import { Campanha } from '../campanhas/model/Campanha';
import { ToastrService } from 'ngx-toastr';
import { AppService } from '../app.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-homepage',
  templateUrl: './homepage.component.html',
  styleUrls: ['./homepage.component.scss']
})
export class HomepageComponent implements OnInit {

  campanhas!: Campanha[];
  imagens: string = environment.imagensUrl;
  
  constructor(@Inject(DOCUMENT) private _document: any, private appService: AppService, private toastr: ToastrService) { }

  ngOnInit() {
    var window = this._document.defaultView;
    window.ajustarPromoSlider();
    window.ajustarDonorsSlider();
    this.ObterCampanhasPremium();
    window.scrollTo(0, 0);
  }

  ObterCampanhasPremium() {
    this.appService.obterCampanhasPremium().subscribe(
      (_campanhas: Campanha[]) => {
        this.campanhas = _campanhas;

        this.campanhas.forEach(campanha => {
          let percentual = (campanha.total_arrecadado! / campanha.valor_desejado) * 100;
          campanha.percentual_arrecadado = Math.trunc(percentual);
        });

        setTimeout(() => this._document.defaultView.ajustarSliderCampanhasDestaque());
      }, error => {
        this.toastr.error("Erro de carregamento!");
      });
  }

  getWidth(percentual: number): any {
    var x = percentual + '%';

    if (percentual > 100) {
      return '100%';
    }

    return x;
  }


}
