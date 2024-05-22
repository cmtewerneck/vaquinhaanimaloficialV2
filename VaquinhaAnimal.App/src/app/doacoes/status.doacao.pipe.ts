import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';

@Pipe({
  name: 'statusDoacao'
})
export class StatusDoacaoPipe implements PipeTransform {

  constructor(private sanitizer: DomSanitizer) { }
  
  transform(status: string) {
   if (status == 'pending'){
    return "Pendente";
   } else if (status == 'failed'){
    return "Falha";
   } else if (status == 'processing'){
    return "Processando";
   } else if (status == 'paid'){
    return "Pago";
   } else if (status == 'canceled'){
    return "Cancelado";
   }
  }
}