import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { TipoAnuncianteEnum, TipoPetEnum } from './model/Adocao';

@Pipe({
  name: 'tipoAnunciante'
})
export class TipoAnunciantePipe implements PipeTransform {
  
  constructor(private sanitizer: DomSanitizer) { }
  
  transform(tipoAnunciante: TipoAnuncianteEnum) {
    if(tipoAnunciante == TipoAnuncianteEnum.Abrigo){
      return "Abrigo";
    } else if(tipoAnunciante == TipoAnuncianteEnum.Empresa){
      return "Empresa";
    } else if(tipoAnunciante == TipoAnuncianteEnum.Particular){
      return "Particular";
    } 
  }
}