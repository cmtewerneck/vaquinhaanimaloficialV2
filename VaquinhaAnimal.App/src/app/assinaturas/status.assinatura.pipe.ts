import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';

@Pipe({
  name: 'statusAssinatura'
})
export class StatusAssinaturaPipe implements PipeTransform {
  
  constructor(private sanitizer: DomSanitizer) { }
  
  transform(status: string) {
    if (status == 'active'){
      return "Ativa";
    } else if (status == 'canceled'){
      return "Cancelada";
    } else if (status == 'future'){
      return "Futura";
    }
  }
}