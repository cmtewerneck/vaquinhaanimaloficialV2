import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';

@Pipe({
  name: 'formaPagamento'
})
export class FormaPagamentoPipe implements PipeTransform {
  
  constructor(private sanitizer: DomSanitizer) { }
  
  transform(status: string) {
    if (status == 'credit_card'){
      return "Cartão de crédito";
    } else if (status == 'boleto'){
      return "Boleto";
    } else if (status == 'pix'){
      return "PIX";
    }
  }
}