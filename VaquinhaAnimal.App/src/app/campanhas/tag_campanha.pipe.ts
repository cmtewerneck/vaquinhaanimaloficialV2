import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { TagCampanhaEnum } from './model/Campanha';

@Pipe({
  name: 'tagCampanha'
})
export class TagCampanhaPipe implements PipeTransform {
  
  constructor(private sanitizer: DomSanitizer) { }
  
  transform(tagCampanha: TagCampanhaEnum) {
    if(tagCampanha == TagCampanhaEnum.Acessórios){
      return "Acessórios";
    } else if(tagCampanha == TagCampanhaEnum.Alimentacao){
      return "Alimentação";
    } else if(tagCampanha == TagCampanhaEnum.Cirurgias){
      return "Cirurgias";
    } else if(tagCampanha == TagCampanhaEnum.Medicamentos){
      return "Medicamentos";
    } else if(tagCampanha == TagCampanhaEnum.Outros){
      return "Outros";
    } else if(tagCampanha == TagCampanhaEnum.PuroAmor){
      return "PuroAmor";
    } else if(tagCampanha == TagCampanhaEnum.Tratamentos){
      return "Tratamentos";
    } 
  }
}