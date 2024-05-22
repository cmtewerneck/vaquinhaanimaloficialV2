import { DOCUMENT } from '@angular/common';
import { Component, Inject, OnInit } from '@angular/core';

@Component({
  selector: 'app-termos',
  templateUrl: './termos.component.html',
  styleUrls: ['./termos.component.scss']
})
export class TermosComponent implements OnInit {

  constructor(@Inject(DOCUMENT) private _document: any) { }

  ngOnInit() {
    var window = this._document.defaultView;
    window.scrollTo(0, 0);
  }

}
