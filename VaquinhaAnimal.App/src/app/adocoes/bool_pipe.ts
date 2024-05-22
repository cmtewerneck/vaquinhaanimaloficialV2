import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';

@Pipe({
  name: 'boolPipe'
})
export class BoolPipe implements PipeTransform {
  
  constructor(private sanitizer: DomSanitizer) { }
  
  transform(castrado: boolean) {
    console.log(castrado);
    if(castrado == true){
      return "Sim";
    } 
    else if(castrado == false){
      return "Não";
    } 
  }
}