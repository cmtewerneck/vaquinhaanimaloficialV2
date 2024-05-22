import { Component, OnInit, TemplateRef } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { SuporteService } from '../suporte.service';
import { Suporte } from '../Model/Suporte';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { NgxSpinnerService } from 'ngx-spinner';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';

@Component({
  selector: 'app-lista-suporte-admin',
  templateUrl: './listaSuporteAdmin.component.html'
})
export class ListaSuporteAdminComponent implements OnInit {

  tickets!: Suporte[];
  ticketResponder!: Suporte;
  errors!: any[];
  campanhaId!: string;
  answerTicketForm!: FormGroup;
  modalRef?: BsModalRef;
  config = {
    backdrop: true,
    ignoreBackdropClick: true
  };
  mensagemEnviada: string = "";
  
  constructor(private suporteService: SuporteService, 
              private toastr: ToastrService, 
              private fb: FormBuilder, 
              private modalService: BsModalService,
              private spinner: NgxSpinnerService) {}

  ngOnInit(): void {this.ObterTodos();}

  ObterTodos() {
    this.spinner.show();
    this.suporteService.obterTodosTickets().subscribe(
      (_tickets: Suporte[]) => {
        this.spinner.hide();
      this.tickets = _tickets;
    }, error => {
      this.spinner.hide();
      console.log(error);
    });
  }  

  verMensagem(mensagem: TemplateRef<any>, mensagemTicket: string) {
    this.mensagemEnviada = mensagemTicket;
    this.modalRef = this.modalService.show(mensagem, this.config);
  }

  responder(resposta: TemplateRef<any>, ticketMensagem: Suporte) {
    this.ticketResponder = ticketMensagem;
    this.modalRef = this.modalService.show(resposta, this.config);
    this.createForm();
  }

  closeModal() {
    this.mensagemEnviada = "";
    // this.answerTicketForm.reset();
    this.modalRef?.hide();
  }

   processarSucesso(response: any) {
    this.spinner.hide();
    this.errors = [];
    this.toastr.success('Campanha iniciada!', 'Sucesso!');
    this.ObterTodos();
    this.campanhaId = "";
  }

  processarSucessoResposta(response: any) {
    this.spinner.hide();
    this.errors = [];
    this.toastr.success('Resposta enviada!', 'Sucesso!');
    this.ObterTodos();
    this.modalRef?.hide();
  }
  
  processarFalha(fail: any) {
    this.spinner.hide();
    this.errors = fail.error.errors;
    this.toastr.error('Ocorreu um erro!', 'Opa :(');
    this.campanhaId = "";
  }

  deletarTicket(id: string) {
    this.spinner.show();

    this.suporteService.excluirTicket(id)
    .subscribe(
      ticket => { 
        this.processarSucesso(ticket) 
      },
      falha => { this.processarFalha(falha) }
      )
   }

   createForm(){
    this.answerTicketForm = this.fb.group({
      resposta: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(1000)]],
      id: [this.ticketResponder.id],
      data: [this.ticketResponder.data],
      mensagem: [this.ticketResponder.mensagem],
      assunto: [this.ticketResponder.assunto],
      respondido: [this.ticketResponder.respondido]
    });
  }

  responderTicket() {
    this.spinner.show();

    if (this.answerTicketForm.dirty && this.answerTicketForm.valid) {
      this.ticketResponder = Object.assign({}, this.ticketResponder, this.answerTicketForm.value);

      this.ticketResponder.respondido = true;

      console.log(this.ticketResponder);
      
      this.suporteService.respostaTicket(this.ticketResponder)
      .subscribe(
        sucesso => { this.processarSucessoResposta(sucesso) },
        falha => { this.processarFalha(falha) }
        );
      }
    }

}
