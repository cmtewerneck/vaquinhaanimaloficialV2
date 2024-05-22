import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';

@Pipe({
  name: 'textFormat'
})
export class TextFormatPipe implements PipeTransform {

  constructor(private sanitizer: DomSanitizer) { }
  
  transform(text: string) {
    let textToReturn = text.replace('\n', '<br/>');
    return textToReturn;
  }
}