import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { TipoPetEnum } from './model/Adocao';

@Pipe({
  name: 'tipoPet'
})
export class TipoPetPipe implements PipeTransform {
  
  constructor(private sanitizer: DomSanitizer) { }
  
  transform(tipoPet: TipoPetEnum) {
    if(tipoPet == TipoPetEnum.Cachorro){
      return "Cachorro";
    } else if(tipoPet == TipoPetEnum.Coelho){
      return "Coelho";
    } else if(tipoPet == TipoPetEnum.Gato){
      return "Gato";
    } else if(tipoPet == TipoPetEnum.Outros){
      return "Outros";
    } else if(tipoPet == TipoPetEnum.Passaro){
      return "Pássaro";
    } 
  }
}