import { DOCUMENT } from '@angular/common';
import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';

@Component({
  selector: 'app-contato',
  templateUrl: './contato.component.html'
})
export class ContatoComponent implements OnInit {

  contactForm!: FormGroup;

  constructor(@Inject(DOCUMENT) private _document: any, public fb: FormBuilder) {}

  ngOnInit() {
    var window = this._document.defaultView;
    window.scrollTo(0, 0);

    this.createForm();
  }

  createForm(){
    this.contactForm = this.fb.group({
      nome: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(64)]],
      sobrenome: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(64)]],
      email: ['', [Validators.required, Validators.minLength(5), Validators.email, Validators.maxLength(64)]],
      celular: [''],
      mensagem: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(500)]],
    });
  }

  sendMessage(){

  }

  resetForm(){
    this.contactForm.reset();
  }

}