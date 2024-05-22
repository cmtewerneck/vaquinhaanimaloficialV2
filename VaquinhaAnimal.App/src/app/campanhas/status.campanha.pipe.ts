import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';

@Pipe({
  name: 'statusCampanha'
})
export class StatusCampanhaPipe implements PipeTransform {

  constructor(private sanitizer: DomSanitizer) { }
  
  transform(status?: number) {
   if (status == 1){
    return "Em edição";
   } else if (status == 2){
    return "Em análise";
   } else if (status == 3){
    return "Em andamento";
   } else if (status == 4){
    return "Finalizada";
   } else if (status == 5){
    return "Rejeitada";
   }

   return "";
  }
}