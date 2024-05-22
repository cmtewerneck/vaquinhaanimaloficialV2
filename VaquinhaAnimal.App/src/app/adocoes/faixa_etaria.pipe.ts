import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { FaixaEtariaEnum } from './model/Adocao';

@Pipe({
  name: 'faixaEtaria'
})
export class FaixaEtariaPipe implements PipeTransform {

  constructor(private sanitizer: DomSanitizer) { }
  
  transform(faixaEtaria: FaixaEtariaEnum) {
    if(faixaEtaria == FaixaEtariaEnum.Faixa01){
      return "De 0 a 6 meses";
    } else if(faixaEtaria == FaixaEtariaEnum.Faixa02){
      return "De 7 a 12 meses";
    } else if(faixaEtaria == FaixaEtariaEnum.Faixa03){
      return "De 1 a 3 anos";
    } else if(faixaEtaria == FaixaEtariaEnum.Faixa04){
      return "De 4 a 6 anos";
    } else if(faixaEtaria == FaixaEtariaEnum.Faixa05){
      return "Acima de 6 anos";
    } 
  }
}