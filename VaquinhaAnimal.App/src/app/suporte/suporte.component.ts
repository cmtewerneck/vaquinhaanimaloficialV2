import { Component, Inject, OnInit, TemplateRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { FailedToNegotiateWithServerError } from '@microsoft/signalr/dist/esm/Errors';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { Suporte } from './Model/Suporte';
import { SuporteService } from './suporte.service';
import { DOCUMENT } from '@angular/common';

@Component({
  selector: 'app-suporte',
  templateUrl: './suporte.component.html'
})
export class SuporteComponent implements OnInit { 
  
  newTicketToggle: boolean = false;
  newTicketForm!: FormGroup;
  tickets!: Suporte[];
  ticket!: Suporte;
  errors: any[] = [];
  modalRef?: BsModalRef;
  config = {
    backdrop: true,
    ignoreBackdropClick: true
  };
  mensagemEnviada: string = "";
  
  constructor(private fb: FormBuilder, 
    private suporteService: SuporteService, 
    private spinner: NgxSpinnerService,
    private toastr: ToastrService,
    private modalService: BsModalService,
    @Inject(DOCUMENT) private _document: any,
    private router: Router) {
    }
    
    ngOnInit(): void {
      this.ObterTodos();
      
      var window = this._document.defaultView;
      window.scrollTo(0, 0);
    }
    
    createForm(){
      this.newTicketForm = this.fb.group({
        assunto: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
        mensagem: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(500)]],
        respondido: [false]
      });
    }
    
    changeTicketToggle(){
      if(this.newTicketToggle == false){
        this.newTicketToggle = true;
        this.createForm();
      } 
      else if(this.newTicketToggle == true){
        this.newTicketToggle = false;
        this.newTicketForm.reset();
        window.scrollTo(0, 0);
      }
    }
    
    verMensagem(mensagem: TemplateRef<any>, mensagemTicket: string) {
      this.mensagemEnviada = mensagemTicket;
      this.modalRef = this.modalService.show(mensagem, this.config);
    }
    
    closeModal() {
      this.mensagemEnviada = "";
      this.modalRef?.hide();
    }
    
    criarTicket() {
      this.spinner.show();
      
      if (this.newTicketForm.dirty && this.newTicketForm.valid) {
        this.ticket = Object.assign({}, this.ticket, this.newTicketForm.value);
        
        this.suporteService.addTicket(this.ticket)
        .subscribe(
          sucesso => { this.processarSucesso(sucesso) },
          falha => { this.processarFalha(falha) }
          );
        }
      }
      
      processarSucesso(response: any) {
        this.spinner.hide();
        this.newTicketForm.reset();
        this.newTicketToggle = false;
        this.errors = [];
        this.ObterTodos();
        window.scrollTo(0, 0);
        
        this.toastr.success('Ticket cadastrado com sucesso!', 'Sucesso!');
      }
      
      processarFalha(fail: any) {
        this.spinner.hide();
        this.errors = fail.error.errors;
        this.toastr.error(this.errors[0], "Erro!");
      }
      
      ObterTodos() {
        this.spinner.show();
        
        this.suporteService.obterMeusTickets().subscribe(
          (_suportes: Suporte[]) => {
            this.tickets = _suportes;
            
            this.spinner.hide();
            
          }, error => {
            this.spinner.hide();
            this.toastr.error(`Erro de carregamento: ${error.error.errors}`);
            console.log(error);
          });
        }
        
        deletarTicket(id: string) {
          this.spinner.show();
          
          this.suporteService.excluirTicket(id)
          .subscribe(
            ticket => { 
              this.processarSucessoExclusao(ticket) 
            },
            falha => { this.processarFalha(falha) }
            )
          }
          
          processarSucessoExclusao(response: any) {
            this.spinner.hide();
            this.newTicketToggle = false;
            this.errors = [];
            this.ObterTodos();
            window.scrollTo(0, 0);
            
            this.toastr.success('Ticket excluído com sucesso!', 'Sucesso!');
          }
          
        }