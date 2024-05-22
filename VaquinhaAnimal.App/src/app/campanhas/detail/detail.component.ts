import { Component, ElementRef, Inject, NgZone, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { PagarmeCardResponse, PagarmeResponse } from 'src/app/auth/User';
import { environment } from 'src/environments/environment';
import { CampanhaService } from '../campanha.service';
import { Campanha } from '../model/Campanha';
import { OrderCartaoNovo } from '../model/OrderCartaoNovo';
import { Order } from '../model/Order';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import * as signalR from "@microsoft/signalr"
import { NgxSpinnerService } from 'ngx-spinner';
import { LocalStorageUtils } from 'src/app/_utils/localStorage';
import { DOCUMENT } from '@angular/common';

@Component({
  selector: 'app-detail',
  templateUrl: './detail.component.html'
})
export class DetailComponent implements OnInit {

  @ViewChild('divRecaptcha')
      divRecaptcha!: ElementRef<HTMLDivElement>;

  get grecaptcha(): any {
    const w = window as any;
    return w['grecaptcha'];
  }
  
  //#region ARRAYS
  get itemsArray(): FormArray {
    return <FormArray>this.doacaoForm.get('items');
  }
  
  get paymentsArray(): FormArray {
    return <FormArray>this.doacaoForm.get('payments');
  }

  get itemsArrayCartaoNovo(): FormArray {
    return <FormArray>this.cartaoNovoForm.get('items');
  }

  get itemsArrayAssinatura(): FormArray {
    return <FormArray>this.assinaturaUserLogadoForm.get('items');
  }
  
  get paymentsArrayCartaoNovo(): FormArray {
    return <FormArray>this.cartaoNovoForm.get('payments');
  }

  get itemsBoletoArray(): FormArray {
    return <FormArray>this.boletoForm.get('items');
  }
  
  get paymentsBoletoArray(): FormArray {
    return <FormArray>this.boletoForm.get('payments');
  }

  get itemsCartaoArray(): FormArray {
    return <FormArray>this.cartaoForm.get('items');
  }
  
  get paymentsCartaoArray(): FormArray {
    return <FormArray>this.cartaoForm.get('payments');
  }

  get itemsCartaoUserLogadoArray(): FormArray {
    return <FormArray>this.cartaoUserLogadoForm.get('items');
  }
  
  get paymentsCartaoUserLogadoArray(): FormArray {
    return <FormArray>this.cartaoUserLogadoForm.get('payments');
  }

  get itemsCartaoCadastradoUserLogadoArray(): FormArray {
    return <FormArray>this.cartaoCadastradoUserLogadoForm.get('items');
  }

  get paymentsCartaoCadastradoUserLogadoArray(): FormArray {
    return <FormArray>this.cartaoCadastradoUserLogadoForm.get('payments');
  }

  get itemsPixArray(): FormArray {
    return <FormArray>this.pixForm.get('items');
  }
  
  get paymentsPixArray(): FormArray {
    return <FormArray>this.pixForm.get('payments');
  }

  get itemsPixRapidoArray(): FormArray {
    return <FormArray>this.pixRapidoForm.get('items');
  }
  
  get paymentsPixRapidoArray(): FormArray {
    return <FormArray>this.pixRapidoForm.get('payments');
  }

  get itemsPixRapidoUserLogadoArray(): FormArray {
    return <FormArray>this.pixRapidoUserLogadoForm.get('items');
  }
  
  get paymentsPixRapidoUserLogadoArray(): FormArray {
    return <FormArray>this.pixRapidoUserLogadoForm.get('payments');
  }

  get itemsBoletoUserLogadoArray(): FormArray {
    return <FormArray>this.boletoUserLogadoForm.get('items');
  }
  
  get paymentsBoletoUserLogadoArray(): FormArray {
    return <FormArray>this.boletoUserLogadoForm.get('payments');
  }

  get itemsPixUserLogadoArray(): FormArray {
    return <FormArray>this.pixUserLogadoForm.get('items');
  }
  
  get paymentsPixUserLogadoArray(): FormArray {
    return <FormArray>this.pixUserLogadoForm.get('payments');
  }
  //#endregion

  // VARIAVEIS GERAIS
  key: string = "6LcbyVwnAAAAAEXUaEsI9VXbxJkFZeDmvcwoNhF5";
  userLogado: boolean = false; 
  localStorage = new LocalStorageUtils; 
  campanha: Campanha; 
  outrasCampanhas!: Campanha[]; 
  imagens: string = environment.imagensUrl; 
  document_mask: string = "000.000.000-00"; 
  document_toggle: string = "CPF"; 
  errors: any[] = []; 
  qrCode!: TemplateRef<any>;  
  modalRef?: BsModalRef; 
  config = {
    backdrop: true,
    ignoreBackdropClick: true
  };
  qrCodeLink!: string; 
  qrCodeCopiaCola!: string; 
  hubConnection!: signalR.HubConnection; 
  
  // Variáveis de doação
  queroDoarToggle: boolean = false;
  queroAssinarToggle: boolean = false;
  queroDoarPixRapidoToggle: boolean = false; 
  queroDoarPixRapidoUserLogadoToggle: boolean = false; 
  queroDoarBoletoToggle: boolean = false; 
  doarToggle: boolean = false; 
  doarSemCadastroToggle: boolean = false;
  cartaoToggle: string = ""; 
  payment_method_toggle: string = ""; 
  queroDoarCartaoCadastradoToggle: boolean = false;
  queroDoarCartaoNovoToggle: boolean = false;

  // FORMGROUPS
  pixRapidoForm!: FormGroup; 
  customerForm!: FormGroup; 
  pixRapidoUserLogadoForm!: FormGroup; 
  customerUserLogadoForm!: FormGroup; 
  assinaturaUserLogadoForm!: FormGroup; 
  boletoForm!: FormGroup; 
  cartaoForm!: FormGroup; 
  cartaoUserLogadoForm!: FormGroup; 
  boletoUserLogadoForm!: FormGroup; 
  pixForm!: FormGroup; 
  pixUserLogadoForm!: FormGroup; 
  cartaoNovoForm!: FormGroup; 
  doacaoForm!: FormGroup; 
  cartaoCadastradoUserLogadoForm!: FormGroup; 

  // VARIAVEIS DE CLASSES
  cartoes!: PagarmeResponse<PagarmeCardResponse>; 
  doacao!: Order; 
  doacaoCartaoNovo!: OrderCartaoNovo; 
  
  constructor(
    private route: ActivatedRoute,
    private fb: FormBuilder,
    private campanhaService: CampanhaService,
    private toastr: ToastrService,
    private ngZone: NgZone,
    private spinner: NgxSpinnerService, 
    private modalService: BsModalService,
    @Inject(DOCUMENT) private _document: any,
    private router: Router) {this.campanha = this.route.snapshot.data['campanha'];}
    
    ngOnInit(): void {
      var window = this._document.defaultView;
      window.scrollTo(0, 0);

      this.ObterOutrasCampanhas();
      this.calcularPercentualArrecadado();

      this.usuarioLogado();
    }

    alterarCampanha2(campanhaId: string) {
      this.campanhaService.obterPorId(campanhaId).subscribe(
        (_campanha: Campanha) => {
          this.campanha = _campanha;
          this.spinner.hide();
          this.calcularPercentualArrecadado();
          window.scrollTo(0, 0);
          this.router.navigate(['campanhas/detalhes/' + this.campanha.id]);
        }, error => {
          this.spinner.hide();
          this.toastr.error("Erro de carregamento!");
          console.log(error);
        });
    }

    alterarCampanha(urlCampanha: string) {
      this.campanhaService.obterUrl(urlCampanha).subscribe(
        (_campanha: Campanha) => {
          this.campanha = _campanha;
          this.spinner.hide();
          this.calcularPercentualArrecadado();
          window.scrollTo(0, 0);
          this.router.navigate(['campanhas/detalhes/' + this.campanha.url_campanha]);
        }, error => {
          this.spinner.hide();
          this.toastr.error("Erro de carregamento!");
          console.log(error);
        });
    }

    usuarioLogado(){
      let userToken = this.localStorage.obterTokenUsuarioSession();
      if(userToken != null){
        this.userLogado = true;
      } else { this.userLogado = false }
    }

    renderizarReCaptchaBoleto() {
      this.ngZone.runOutsideAngular(() => {
        if (!this.grecaptcha || !this.divRecaptcha) {
          setTimeout(() => {
            this.renderizarReCaptchaBoleto();
          }, 500);
  
          return;
        }

        const idElemento =
          this.divRecaptcha.nativeElement.getAttribute('id');
  
        this.grecaptcha.render(idElemento, {
          sitekey: this.key,
          callback: (response: string) => {
            this.ngZone.run(() => {
              console.log("CAPTCHA: "+response);
              this.boletoForm.get('recaptcha')?.setValue(response);
            });
          },
        });
      });
    }

    renderizarReCaptchaBoletoUserLogado() {
      this.ngZone.runOutsideAngular(() => {
        if (!this.grecaptcha || !this.divRecaptcha) {
          setTimeout(() => {
            this.renderizarReCaptchaBoletoUserLogado();
          }, 500);
  
          return;
        }

        const idElemento =
          this.divRecaptcha.nativeElement.getAttribute('id');
  
        this.grecaptcha.render(idElemento, {
          sitekey: this.key,
          callback: (response: string) => {
            this.ngZone.run(() => {
              console.log("CAPTCHA: "+response);
              this.boletoUserLogadoForm.get('recaptcha')?.setValue(response);
            });
          },
        });
      });
    }

    renderizarReCaptchaCartao() {
      this.ngZone.runOutsideAngular(() => {
        if (!this.grecaptcha || !this.divRecaptcha) {
          setTimeout(() => {
            this.renderizarReCaptchaCartao();
          }, 500);
  
          return;
        }

        const idElemento =
          this.divRecaptcha.nativeElement.getAttribute('id');
  
        this.grecaptcha.render(idElemento, {
          sitekey: this.key,
          callback: (response: string) => {
            this.ngZone.run(() => {
              console.log("CAPTCHA: "+response);
              this.cartaoForm.get('recaptcha')?.setValue(response);
            });
          },
        });
      });
    }

    renderizarReCaptchaPixRapidoUserLogado() {
      this.ngZone.runOutsideAngular(() => {
        if (!this.grecaptcha || !this.divRecaptcha) {
          setTimeout(() => {
            this.renderizarReCaptchaPixRapidoUserLogado();
          }, 500);
  
          return;
        }

        const idElemento =
          this.divRecaptcha.nativeElement.getAttribute('id');
  
        this.grecaptcha.render(idElemento, {
          sitekey: this.key,
          callback: (response: string) => {
            this.ngZone.run(() => {
              console.log("CAPTCHA: "+response);
              this.pixRapidoUserLogadoForm.get('recaptcha')?.setValue(response);
            });
          },
        });
      });
    }

    renderizarReCaptchaPix() {
      this.ngZone.runOutsideAngular(() => {
        if (!this.grecaptcha || !this.divRecaptcha) {
          setTimeout(() => {
            this.renderizarReCaptchaPix();
          }, 500);
  
          return;
        }

        const idElemento =
          this.divRecaptcha.nativeElement.getAttribute('id');
  
        this.grecaptcha.render(idElemento, {
          sitekey: this.key,
          callback: (response: string) => {
            this.ngZone.run(() => {
              console.log("CAPTCHA: "+response);
              this.pixForm.get('recaptcha')?.setValue(response);
            });
          },
        });
      });
    }

    renderizarReCaptchaCartaoNovoUserLogado() {
      this.ngZone.runOutsideAngular(() => {
        if (!this.grecaptcha || !this.divRecaptcha) {
          setTimeout(() => {
            this.renderizarReCaptchaCartaoNovoUserLogado();
          }, 500);
  
          return;
        }

        const idElemento =
          this.divRecaptcha.nativeElement.getAttribute('id');
  
        this.grecaptcha.render(idElemento, {
          sitekey: this.key,
          callback: (response: string) => {
            this.ngZone.run(() => {
              console.log("CAPTCHA: "+response);
              this.cartaoUserLogadoForm.get('recaptcha')?.setValue(response);
            });
          },
        });
      });
    }

    renderizarReCaptchaCartaoCadastradoUserLogado() {
      this.ngZone.runOutsideAngular(() => {
        if (!this.grecaptcha || !this.divRecaptcha) {
          setTimeout(() => {
            this.renderizarReCaptchaCartaoCadastradoUserLogado();
          }, 500);
  
          return;
        }

        const idElemento =
          this.divRecaptcha.nativeElement.getAttribute('id');
  
        this.grecaptcha.render(idElemento, {
          sitekey: this.key,
          callback: (response: string) => {
            this.ngZone.run(() => {
              console.log("CAPTCHA: "+response);
              this.cartaoCadastradoUserLogadoForm.get('recaptcha')?.setValue(response);
            });
          },
        });
      });
    }

    renderizarReCaptchaPixRapido() {
      this.ngZone.runOutsideAngular(() => {
        if (!this.grecaptcha || !this.divRecaptcha) {
          setTimeout(() => {
            this.renderizarReCaptchaPixRapido();
          }, 500);
  
          return;
        }

        const idElemento =
          this.divRecaptcha.nativeElement.getAttribute('id');
  
        this.grecaptcha.render(idElemento, {
          sitekey: this.key,
          callback: (response: string) => {
            this.ngZone.run(() => {
              console.log("CAPTCHA: "+response);
              this.pixRapidoForm.get('recaptcha')?.setValue(response);
            });
          },
        });
      });
    }

    renderizarReCaptchaAssinaturaUserLogado() {
      this.ngZone.runOutsideAngular(() => {
        if (!this.grecaptcha || !this.divRecaptcha) {
          setTimeout(() => {
            this.renderizarReCaptchaAssinaturaUserLogado();
          }, 500);
  
          return;
        }

        const idElemento =
          this.divRecaptcha.nativeElement.getAttribute('id');
  
        this.grecaptcha.render(idElemento, {
          sitekey: this.key,
          callback: (response: string) => {
            this.ngZone.run(() => {
              console.log("CAPTCHA: "+response);
              this.assinaturaUserLogadoForm.get('recaptcha')?.setValue(response);
            });
          },
        });
      });
    }

    renderizarReCaptchaPixUserLogado() {
      this.ngZone.runOutsideAngular(() => {
        if (!this.grecaptcha || !this.divRecaptcha) {
          setTimeout(() => {
            this.renderizarReCaptchaPixUserLogado();
          }, 500);
  
          return;
        }

        const idElemento =
          this.divRecaptcha.nativeElement.getAttribute('id');
  
        this.grecaptcha.render(idElemento, {
          sitekey: this.key,
          callback: (response: string) => {
            this.ngZone.run(() => {
              console.log("CAPTCHA: "+response);
              this.pixUserLogadoForm.get('recaptcha')?.setValue(response);
            });
          },
        });
      });
    }

    ObterOutrasCampanhas() {
      this.spinner.show();
      this.campanhaService.obterOutrasCampanhas().subscribe(
        (_outrasCampanhas: Campanha[]) => {
          this.outrasCampanhas = _outrasCampanhas;

          this.outrasCampanhas.forEach(campanha => {
            let percentual = (campanha.total_arrecadado! / campanha.valor_desejado) * 100;
            campanha.percentual_arrecadado = Math.trunc(percentual);
          });

          console.log(this.outrasCampanhas);
          this.spinner.hide();
        }, error => {
          this.spinner.hide();
          this.toastr.error("Erro de carregamento!");
          console.log(error);
        });
    }

    getWidth(percentual: number): any {
      var x = percentual + '%';
  
      if (percentual > 100) {
        return '100%';
      }
  
      return x;
    }

    calcularPercentualArrecadado(){
      let percentual = (this.campanha.total_arrecadado! / this.campanha.valor_desejado) * 100;
      this.campanha.percentual_arrecadado = Math.trunc(percentual);
    }
    
    queroDoar(){
      this.queroDoarToggle = true;
    }

    naoQueroDoar(){
      this.queroDoarToggle = false;
      this.cartaoToggle = "";
      this.payment_method_toggle = "";
    }  

    voltarFormulario(){
      this.payment_method_toggle = "";
    }  

    queroDoarPixRapido(){
      if(this.userLogado){
        this.queroDoarPixRapidoUserLogadoToggle = true;
        this.renderizarReCaptchaPixRapidoUserLogado();
        this.createPixRapidoUserLogadoForm();
      } else {
        this.queroDoarPixRapidoToggle = true;
        this.renderizarReCaptchaPixRapido();
        this.createPixRapidoForm();
      }
    }

    createPixRapidoForm(){
      this.pixRapidoForm = this.fb.group({
        recaptcha: [null, Validators.required],
        valorPlataforma: [''],
        customer: this.customerForm = this.fb.group({
          name: ['', [Validators.required, Validators.maxLength(64)]],
          email: ['', [Validators.required, Validators.email, Validators.maxLength(64)]],
          type: ['', [Validators.required, Validators.minLength(7), Validators.maxLength(10)]],
          document: ['', Validators.required]
        }),
        items: this.fb.array([this.criaItemPixRapido()]),
        payments: this.fb.array([this.criaPaymentPixRapido()])
      });
    }

    createPixRapidoUserLogadoForm(){
      this.pixRapidoUserLogadoForm = this.fb.group({
        recaptcha: [null, Validators.required],
        valorPlataforma: [''],
        customer: this.customerUserLogadoForm = this.fb.group({
          name: [''],
          email: [''],
          type: [''],
          document: ['']
        }),
        items: this.fb.array([this.criaItemPixRapido()]),
        payments: this.fb.array([this.criaPaymentPixRapido()])
      });
    }

    createPixUserLogadoForm(){
      this.pixUserLogadoForm = this.fb.group({
        recaptcha: [null, Validators.required],
        valorPlataforma: [''],
        customer: this.customerUserLogadoForm = this.fb.group({
          name: [''],
          email: [''],
          type: [''],
          document: ['']
        }),
        items: this.fb.array([this.criaItem()]),
        payments: this.fb.array([this.criaPaymentPix()])
      });
    }

    criaItemPixRapido(): FormGroup {
      return this.fb.group({
        amount: ['', Validators.required],
        description: ['Vaquinha Animal'],
        quantity: [1],
        code: ['Vaquinha - PIX']
      });
    }

    criaPaymentPixRapido(): FormGroup {
      return this.fb.group({
        payment_method: ['pix'],
        pix: this.fb.group({
          expires_in: [300],
          additional_informations: [null]
        })
      });
    }

    valueBoxSelected(event: any, tipo: string){
      if (tipo == 'pixRapido'){
        this.itemsPixRapidoArray.at(0).get("amount")?.setValue(event.target.value);
      } 
      else if (tipo == 'pixRapidoPlataforma'){
        this.pixRapidoForm.get("valorPlataforma")?.setValue(event.target.value);
      }
      else if (tipo == 'pixRapidoLogado'){
        this.itemsPixRapidoUserLogadoArray.at(0).get("amount")?.setValue(event.target.value);
      }
      else if (tipo == 'pixRapidoPlataformaLogado'){
        this.pixRapidoUserLogadoForm.get("valorPlataforma")?.setValue(event.target.value);
      }
      else if (tipo == 'boletoPlataformaLogado'){
        this.boletoUserLogadoForm.get("valorPlataforma")?.setValue(event.target.value);
      }
      else if (tipo == 'boletoPlataforma'){
        this.boletoForm.get("valorPlataforma")?.setValue(event.target.value);
      }
      else if (tipo == 'boleto'){
        this.itemsBoletoArray.at(0).get("amount")?.setValue(event.target.value);
      }
      else if (tipo == 'boletoLogado'){
        this.itemsBoletoUserLogadoArray.at(0).get("amount")?.setValue(event.target.value);
      }
      else if (tipo == 'pix'){
        this.itemsPixArray.at(0).get("amount")?.setValue(event.target.value);
      }
      else if (tipo == 'pixPlataforma'){
        this.pixForm.get("valorPlataforma")?.setValue(event.target.value);
      }
      else if (tipo == 'pixLogado'){
        this.itemsPixUserLogadoArray.at(0).get("amount")?.setValue(event.target.value);
      }
      else if (tipo == 'pixLogadoPlataforma'){
        this.pixUserLogadoForm.get("valorPlataforma")?.setValue(event.target.value);
      }
      else if (tipo == 'cartao'){
        this.itemsCartaoArray.at(0).get("amount")?.setValue(event.target.value);
      }
      else if (tipo == 'cartaoPlataforma'){
        this.cartaoForm.get("valorPlataforma")?.setValue(event.target.value);
      }
      else if (tipo == 'cartaoLogado'){
        this.itemsCartaoUserLogadoArray.at(0).get("amount")?.setValue(event.target.value);
      }
      else if (tipo == 'cartaoNovoUserLogadoPlataforma'){
        this.cartaoUserLogadoForm.get("valorPlataforma")?.setValue(event.target.value);
      }
      else if (tipo == 'cartaoLogadoCadastrado'){
        this.itemsCartaoCadastradoUserLogadoArray.at(0).get("amount")?.setValue(event.target.value);
      }  
      else if (tipo == 'cartaoCadastradoUserLogadoPlataforma'){
        this.cartaoCadastradoUserLogadoForm.get("valorPlataforma")?.setValue(event.target.value);
      } 
      else if (tipo == 'assinatura'){
        this.itemsArrayAssinatura.at(0).get("pricing_scheme.price")?.setValue(event.target.value);
      }     
    }

    naoQueroDoarPixRapido(){
      if(this.userLogado){
        this.queroDoarPixRapidoUserLogadoToggle = false;
        this.customerUserLogadoForm.reset();
        this.pixRapidoUserLogadoForm.reset();
      } else {
        this.queroDoarPixRapidoToggle = false;
        this.customerForm.reset();
        this.pixRapidoForm.reset();
      }
    }

    naoQueroDoarPix(){
      if(this.userLogado){
        this.pixUserLogadoForm.reset();
        this.customerUserLogadoForm.reset();
      } else {
        this.pixForm.reset();
        this.customerForm.reset();
      }

      this.payment_method_toggle = "";
    }

    naoQueroDoarBoleto(){
      if(this.userLogado){
        this.boletoUserLogadoForm.reset();
        this.customerUserLogadoForm.reset();
      } else {
        this.boletoForm.reset();
        this.customerForm.reset();
      }

      this.payment_method_toggle = "";
    }

    naoQueroDoarCartao(){
      this.cartaoForm.reset();
      this.customerForm.reset();
      this.queroDoarCartaoCadastradoToggle = false;
      this.queroDoarCartaoNovoToggle = false;
      this.queroAssinarToggle = false;
      this.payment_method_toggle = "";
    }

    naoQueroDoarCartaoUserLogado(){
      this.cartaoUserLogadoForm.reset();
      this.customerUserLogadoForm.reset();

      this.queroDoarCartaoCadastradoToggle = false;
      this.queroDoarCartaoNovoToggle = false;
      this.queroAssinarToggle = false;
      this.payment_method_toggle = "credit_card";
    }

    naoQueroAssinarUserLogado(){
      this.assinaturaUserLogadoForm.reset();
      this.customerUserLogadoForm.reset();

      this.queroDoarCartaoCadastradoToggle = false;
      this.queroDoarCartaoNovoToggle = false;
      this.queroAssinarToggle = false;
      this.payment_method_toggle = "credit_card";
    }

    naoQueroDoarCartaoCadastradoUserLogado(){
      this.cartaoCadastradoUserLogadoForm.reset();
      this.customerUserLogadoForm.reset();

      this.queroDoarCartaoCadastradoToggle = false;
      this.queroDoarCartaoNovoToggle = false;
      this.payment_method_toggle = "credit_card";
    }

    realizarDoacaoPixRapido(qrCode: TemplateRef<any>){
      this.spinner.show();

      let doacaoPixEntity: any;

      if (this.pixRapidoForm.dirty && this.pixRapidoForm.valid) 
      {
        doacaoPixEntity = Object.assign({}, doacaoPixEntity, this.pixRapidoForm.value);
      } 
      else 
      { 
        this.toastr.error("Erro no formulário.", 'Erro!'); 
      }

      if(doacaoPixEntity.valorPlataforma == ""){
        doacaoPixEntity.valorPlataforma = 0
      };
      
      this.campanhaService.realizarDoacaoPixRapido(doacaoPixEntity, this.campanha.id)
      .subscribe(
        sucesso => { this.processarSucessoPixRapido(sucesso, qrCode) },
        falha => { this.processarFalhaPixRapido(falha) }
        );
    }

    realizarDoacaoPixRapidoUserLogado(qrCodeUserLogado: TemplateRef<any>){
      this.spinner.show();

      let doacaoPixEntity: any;

      if (this.pixRapidoUserLogadoForm.valid) 
      {
        doacaoPixEntity = Object.assign({}, doacaoPixEntity, this.pixRapidoUserLogadoForm.value);
      } 
      else 
      { 
        this.toastr.error("Erro no formulário.", 'Erro!'); 
      }

      if(doacaoPixEntity.valorPlataforma == ""){
        doacaoPixEntity.valorPlataforma = 0
      };
      
      this.campanhaService.realizarDoacaoPixRapido(doacaoPixEntity, this.campanha.id)
      .subscribe(
        sucesso => { this.processarSucessoPixUserLogadoRapido(sucesso, qrCodeUserLogado) },
        falha => { this.processarFalhaPixRapido(falha) }
        );
    }

    realizarDoacaoPixUserLogado(qrCodeUserLogado: TemplateRef<any>){
      this.spinner.show();

      let doacaoPixEntity: any;

      if (this.pixUserLogadoForm.valid) 
      {
        doacaoPixEntity = Object.assign({}, doacaoPixEntity, this.pixUserLogadoForm.value);
        console.log(doacaoPixEntity);
      } 
      else 
      { 
        this.toastr.error("Erro no formulário.", 'Erro!'); 
      }

      if(doacaoPixEntity.valorPlataforma == ""){
        doacaoPixEntity.valorPlataforma = 0
      };
      
      this.campanhaService.realizarDoacaoPix(doacaoPixEntity, this.campanha.id)
      .subscribe(
        sucesso => { this.processarSucessoPixUserLogado(sucesso, qrCodeUserLogado) },
        falha => { this.processarFalhaPix(falha) }
        );
    }

    realizarDoacaoBoletoUserLogado(){
      this.spinner.show();

      let doacaoBoletoEntity: any;

      if (this.boletoUserLogadoForm.valid) 
      {
        doacaoBoletoEntity = Object.assign({}, doacaoBoletoEntity, this.boletoUserLogadoForm.value);
      }

      if(doacaoBoletoEntity.valorPlataforma == ""){
        doacaoBoletoEntity.valorPlataforma = 0
      };
      
      this.campanhaService.realizarDoacaoBoleto(doacaoBoletoEntity, this.campanha.id)
      .subscribe(
        sucesso => { this.processarSucessoBoleto(sucesso) },
        falha => { this.processarFalhaBoleto(falha) }
        );
    }

    queroDoarSemCadastro(){
      this.doarSemCadastroToggle = true;
    }

    openModal(qrCode: TemplateRef<any>) {
      this.modalRef = this.modalService.show(qrCode, this.config);

      this.createConnection(); // Cria a conexão
      this.pixListener(); // Escuta o método
      this.startConnection(); // Inicia a conexão
    }

    closeModal() {
      this.modalRef?.hide();
      this.queroDoarPixRapidoToggle = false;
      this.customerForm.reset();
      this.pixRapidoForm.reset();
    }

    closeModalUserLogado() {
      this.modalRef?.hide();
      this.queroDoarPixRapidoUserLogadoToggle = false;
      this.customerUserLogadoForm.reset();
      this.pixRapidoUserLogadoForm.reset();
    }

    closeModalSignalR() {
      this.modalRef?.hide();
    }

    closeModalPixInterno() {
      this.modalRef?.hide();
      this.queroDoarToggle = false;
      this.customerForm.reset();
      this.pixForm.reset();
      this.payment_method_toggle = "";
    }
    
    paymentMethodSelected(event: any){
      this.payment_method_toggle = event.target.value;
      this.cartaoToggle = "";

      if(this.payment_method_toggle == "billing"){
        if(this.userLogado){
          this.renderizarReCaptchaBoletoUserLogado();
          this.createBoletoUserLogadoForm();
        } else {
          this.renderizarReCaptchaBoleto();
          this.createBoletoForm();
        }
      }

      if(this.payment_method_toggle == "pix"){
        if(this.userLogado){
          this.renderizarReCaptchaPixUserLogado();
          this.createPixUserLogadoForm();
        } else {
          this.renderizarReCaptchaPix();
          this.createPixForm();
        }
      }

      if(this.payment_method_toggle == "credit_card"){
        if(!this.userLogado){
          this.renderizarReCaptchaCartao();
          this.createCartaoForm();
        } 
      }

      console.log(this.payment_method_toggle);
    }

    cardToBeUsedSelected(event: any){
      let card_to_be_used_toggle = event.target.value;

      if(card_to_be_used_toggle == "cartaoNovo"){
        this.queroDoarCartaoNovoToggle = true;
        this.renderizarReCaptchaCartaoNovoUserLogado();
        this.createCartaoUserLogadoForm();
      } 
      else if (card_to_be_used_toggle == "cartaoCadastrado"){
        this.queroDoarCartaoCadastradoToggle = true;

        this.campanhaService.obterMeusCartoes()
        .subscribe(cartoes => this.cartoes = cartoes);

        this.renderizarReCaptchaCartaoCadastradoUserLogado();
        this.createCartaoCadastradoUserLogadoForm();
      }
      else if (card_to_be_used_toggle == "assinatura"){
        this.queroAssinarToggle = true;

        this.renderizarReCaptchaAssinaturaUserLogado();
        this.createAssinaturaUserLogadoForm();
      }
    }

    createAssinaturaUserLogadoForm(){
      this.assinaturaUserLogadoForm = this.fb.group({
        recaptcha: [null, Validators.required],
        card_id: [''],
        customer: this.fb.group({
          name: [''],
          email: [''],
          type: [''],
          document: ['']
        }),
        card: this.fb.group({
          number: [''],
          exp_month: [''],
          exp_year: [''],
          cvv: [''],
          holder_name: [''],
          holder_document: [''],
          billing_address: this.fb.group({
            line_1: [''],
            zip_code: [''],
            city: [''],
            state: [''],
            country: ['']
          })
        }),
        items: this.fb.array([this.criaAssinaturaItem()])
      });
    }

    criaAssinaturaItem(): FormGroup {
      return this.fb.group({
        description: ['Assinatura'],
        quantity: [1],
        pricing_scheme: this.fb.group({
          scheme_type: ['unit'],
          price: ['']
        })
      });
    }

    createPixForm(){
      this.pixForm = this.fb.group({
        recaptcha: [null, Validators.required],
        valorPlataforma: [''],
        customer: this.customerForm = this.fb.group({
          name: ['', [Validators.required, Validators.maxLength(64)]],
          email: ['', [Validators.required, Validators.email, Validators.maxLength(64)]],
          type: ['', [Validators.required, Validators.minLength(7), Validators.maxLength(10)]],
          document: ['', Validators.required]
        }),
        items: this.fb.array([this.criaItem()]),
        payments: this.fb.array([this.criaPaymentPix()])
      });
    }

    documentSelected(event: any){
      if(event.target.value == "individual"){
        this.document_mask = "000.000.000-00";
        this.document_toggle = "CPF";
      } else if(event.target.value == "company"){
        this.document_mask = "00.000.000/0000-00"
        this.document_toggle = "CNPJ";
      }

      this.setDocumentValidation();
    }

    setDocumentValidation(){
      this.customerForm.controls['document'].clearValidators();
      this.customerForm.controls['document'].setValue("");

      if (this.document_toggle == "CPF"){
        this.customerForm.controls['document'].setValidators([Validators.required, Validators.minLength(11), Validators.maxLength(11)]);
      } else if(this.document_toggle == "CNPJ"){
        this.customerForm.controls['document'].setValidators([Validators.required, Validators.minLength(14), Validators.maxLength(14)]);
      }

      this.customerForm.controls['document'].updateValueAndValidity();
    }

    criaItem(): FormGroup {
      return this.fb.group({
        amount: ['', Validators.required],
        description: ['Vaquinha Animal'],
        quantity: [1],
        code: ['Vaquinha'],
      });
    }

    criaPaymentCartao(): FormGroup {
      return this.fb.group({
        payment_method: ['credit_card'],
        credit_card: this.fb.group({
          recurrence: [false],
          installments: [1],
          statement_descriptor: ['Vaquinha'],
          card_id: [''],
          card: this.fb.group({
            number: ['', Validators.required],
            exp_month: ['', Validators.required],
            exp_year: ['', Validators.required],
            cvv: ['', Validators.required],
            holder_name: ['', Validators.required],
            holder_document: ['', Validators.required],
            billing_address: this.fb.group({
              line_1: ['', Validators.required],
              zip_code: ['', Validators.required],
              city: ['', Validators.required],
              state: ['', Validators.required],
              country: ['', Validators.required],
            })
          })
        })
      });
    }

    criaPaymentCartaoCadastrado(): FormGroup {
      return this.fb.group({
        payment_method: ['credit_card'],
        credit_card: this.fb.group({
          recurrence: [false],
          installments: [1],
          statement_descriptor: ['Vaquinha'],
          card_id: ['', Validators.required],
          card: this.fb.group({
            number: [''],
            exp_month: [null],
            exp_year: [null],
            cvv: [''],
            holder_name: [''],
            holder_document: [''],
            billing_address: this.fb.group({
              line_1: ['', Validators.required],
              zip_code: ['', Validators.required],
              city: ['', Validators.required],
              state: ['', Validators.required],
              country: ['', Validators.required],
            })
          })
        })
      });
    }

    criaPaymentPix(): FormGroup {
      return this.fb.group({
        payment_method: ['pix'],
        pix: this.fb.group({
          expires_in: [300],
          additional_informations: [null]
        })
      });
    }

    createBoletoForm(){
      this.boletoForm = this.fb.group({
        recaptcha: [null, Validators.required],
        valorPlataforma: [''],
        customer: this.customerForm = this.fb.group({
          name: ['', [Validators.required, Validators.maxLength(64)]],
          email: ['', [Validators.required, Validators.email, Validators.maxLength(64)]],
          type: ['', [Validators.required, Validators.minLength(7), Validators.maxLength(10)]],
          document: ['', Validators.required]
        }),
        items: this.fb.array([this.criaItem()]),
        payments: this.fb.array([this.criaPaymentBoleto()])
      });
    }

    createCartaoForm(){
      this.cartaoForm = this.fb.group({
        recaptcha: [null, Validators.required],
        valorPlataforma: [''],
        customer: this.customerForm = this.fb.group({
          name: ['', [Validators.required, Validators.maxLength(64)]],
          email: ['', [Validators.required, Validators.email, Validators.maxLength(64)]],
          type: ['', [Validators.required, Validators.minLength(7), Validators.maxLength(10)]],
          document: ['', Validators.required]
        }),
        items: this.fb.array([this.criaItem()]),
        payments: this.fb.array([this.criaPaymentCartao()])
      });
    }

    createCartaoUserLogadoForm(){
      this.cartaoUserLogadoForm = this.fb.group({
        recaptcha: [null, Validators.required],
        valorPlataforma: [''],
        customer: this.customerUserLogadoForm = this.fb.group({
          name: [''],
          email: [''],
          type: [''],
          document: ['']
        }),
        items: this.fb.array([this.criaItem()]),
        payments: this.fb.array([this.criaPaymentCartao()])
      });
    }

    createCartaoCadastradoUserLogadoForm(){
      this.cartaoCadastradoUserLogadoForm = this.fb.group({
        recaptcha: [null, Validators.required],
        valorPlataforma: [''],
        customer: this.customerUserLogadoForm = this.fb.group({
          name: [''],
          email: [''],
          type: [''],
          document: ['']
        }),
        items: this.fb.array([this.criaItem()]),
        payments: this.fb.array([this.criaPaymentCartaoCadastrado()])
      });
    }

    createBoletoUserLogadoForm(){
      this.boletoUserLogadoForm = this.fb.group({
        recaptcha: [null, Validators.required],
        valorPlataforma: [''],
        customer: this.customerUserLogadoForm = this.fb.group({
          name: [''],
          email: [''],
          type: [''],
          document: ['']
        }),
        items: this.fb.array([this.criaItem()]),
        payments: this.fb.array([this.criaPaymentBoleto()])
      });
    }

    cardToBeUsed(event: any){
      this.cartaoToggle = event.target.value;

      if(this.cartaoToggle == "cartaoCadastrado"){
        this.campanhaService.obterMeusCartoes()
          .subscribe(cartoes => this.cartoes = cartoes);
          
          this.doacaoForm = this.fb.group({
            items: this.fb.array([this.criaItem()]),
            payments: this.fb.array([this.criaPayment()])
          });
      }

      if(this.cartaoToggle == "cartaoNovo"){
        this.cartaoNovoForm = this.fb.group({
          salvarCartao: [false, Validators.required],
          items: this.fb.array([this.criaItem()]),
          payments: this.fb.array([this.criaPaymentCartaoNovo()])
        });
      }
    }
    
    criaPayment(): FormGroup {
      return this.fb.group({
        payment_method: ['credit_card'],
        credit_card: this.fb.group({
          recurrence: [false],
          installments: [1],
          statement_descriptor: ['Doadores'],
          card_id: ['', Validators.required],
          card: this.fb.group({
            cvv: ['', Validators.required]
          })
        })
      });
    }

    criaPaymentCartaoNovo(): FormGroup {
      return this.fb.group({
        payment_method: ['credit_card'],
        credit_card: this.fb.group({
          recurrence: [false],
          installments: [1],
          statement_descriptor: ['Doadores'],
          card: this.fb.group({
            number: ['', Validators.required],
            holder_name: ['', Validators.required],
            holder_document: ['', Validators.required],
            brand: ['', Validators.required],
            exp_month: ['', Validators.required],
            exp_year: ['', Validators.required],
            cvv: ['', Validators.required],
            billing_address: this.fb.group({
              line_1: ['', Validators.required],
              zip_code: ['', Validators.required],
              city: ['', Validators.required],
              state: ['', Validators.required],
              country: ['', Validators.required]
            })
          }),
        })
      });
    }

    criaPaymentBoleto(): FormGroup {
      return this.fb.group({
        payment_method: ['boleto'],
        boleto: this.fb.group({
          instructions: ['Não deixe de doar. O mundo precisa da sua ajuda!'],
          due_at: [null]
        })
      });
    }
    
    realizarDoacao(){
      this.spinner.show();

      if (this.doacaoForm.dirty && this.doacaoForm.valid) 
      {
        this.doacao = Object.assign({}, this.doacao, this.doacaoForm.value);
      }
      
      console.log(this.doacao);
      
      this.campanhaService.realizarDoacao(this.doacao, this.campanha.id)
      .subscribe(
        sucesso => { this.processarSucesso(sucesso) },
        falha => { this.processarFalha(falha) }
        );
    }

    realizarDoacaoCartaoNovo(){
      this.spinner.show();

      if (this.cartaoNovoForm.dirty && this.cartaoNovoForm.valid) 
      {
        this.doacaoCartaoNovo = Object.assign({}, this.doacaoCartaoNovo, this.cartaoNovoForm.value);
      }
      
      console.log(this.doacaoCartaoNovo);
      
      this.campanhaService.realizarDoacaoCartaoNovo(this.doacaoCartaoNovo, this.campanha.id)
      .subscribe(
        sucesso => { this.processarSucessoCartaoNovo(sucesso) },
        falha => { this.processarFalha(falha) }
        );
    }

    realizarAssinaturaUserLogado(){
      this.spinner.show();

      let assinaturaEntity: any;

      if (this.assinaturaUserLogadoForm.dirty && this.assinaturaUserLogadoForm.valid) 
      {
        assinaturaEntity = Object.assign({}, assinaturaEntity, this.assinaturaUserLogadoForm.value);
      }
      
      this.campanhaService.realizarAssinatura(assinaturaEntity, this.campanha.id)
      .subscribe(
        sucesso => { this.processarSucessoAssinatura(sucesso) },
        falha => { this.processarFalhaBoleto(falha) }
        );
    }

    realizarDoacaoBoleto(){
      this.spinner.show();

      let doacaoBoletoEntity: any;

      if (this.boletoForm.dirty && this.boletoForm.valid) 
      {
        doacaoBoletoEntity = Object.assign({}, doacaoBoletoEntity, this.boletoForm.value);
      }

      if(doacaoBoletoEntity.valorPlataforma == ""){
        doacaoBoletoEntity.valorPlataforma = 0
      };
      
      this.campanhaService.realizarDoacaoBoleto(doacaoBoletoEntity, this.campanha.id)
      .subscribe(
        sucesso => { this.processarSucessoBoleto(sucesso) },
        falha => { this.processarFalhaBoleto(falha) }
        );
    }

    realizarDoacaoPix(qrCode: TemplateRef<any>){
      this.spinner.show();

      let doacaoPixEntity: any;

      if (this.pixForm.dirty && this.pixForm.valid) 
      {
        doacaoPixEntity = Object.assign({}, doacaoPixEntity, this.pixForm.value);
      }

      if(doacaoPixEntity.valorPlataforma == ""){
        doacaoPixEntity.valorPlataforma = 0
      };
      
      this.campanhaService.realizarDoacaoPix(doacaoPixEntity, this.campanha.id)
      .subscribe(
        sucesso => { this.processarSucessoPix(sucesso, qrCode) },
        falha => { this.processarFalha(falha) }
        );
    }

    realizarDoacaoCartao(){
      this.spinner.show();

      let doacaoCartaoEntity: any;

      if (this.cartaoForm.dirty && this.cartaoForm.valid) 
      {
        doacaoCartaoEntity = Object.assign({}, doacaoCartaoEntity, this.cartaoForm.value);
      }
      
      if(doacaoCartaoEntity.valorPlataforma == ""){
        doacaoCartaoEntity.valorPlataforma = 0
      };
      
      this.campanhaService.realizarDoacaoCartao(doacaoCartaoEntity, this.campanha.id)
      .subscribe(
        sucesso => { this.processarSucessoCartao(sucesso) },
        falha => { this.processarFalhaCartao(falha) }
        );
    }

    realizarDoacaoCartaoUserLogado(){
      this.spinner.show();

      let doacaoCartaoUserLogadoEntity: any;

      if (this.cartaoUserLogadoForm.dirty && this.cartaoUserLogadoForm.valid) 
      {
        doacaoCartaoUserLogadoEntity = Object.assign({}, doacaoCartaoUserLogadoEntity, this.cartaoUserLogadoForm.value);
      }
      else 
      { 
        this.toastr.error("Erro no formulário.", 'Erro!'); 
      }

      if(doacaoCartaoUserLogadoEntity.valorPlataforma == ""){
        doacaoCartaoUserLogadoEntity.valorPlataforma = 0
      };
      
      this.campanhaService.realizarDoacaoCartao(doacaoCartaoUserLogadoEntity, this.campanha.id)
      .subscribe(
        sucesso => { this.processarSucessoCartao(sucesso) },
        falha => { this.processarFalhaCartao(falha) }
        );
    }

    realizarDoacaoCartaoCadastradoUserLogado(){
      this.spinner.show();

      let doacaoCartaoCadastradoUserLogadoEntity: any;

      if (this.cartaoCadastradoUserLogadoForm.dirty && this.cartaoCadastradoUserLogadoForm.valid) 
      {
        doacaoCartaoCadastradoUserLogadoEntity = Object.assign({}, doacaoCartaoCadastradoUserLogadoEntity, this.cartaoCadastradoUserLogadoForm.value);
      }
      else 
      { 
        this.toastr.error("Erro no formulário.", 'Erro!'); 
      }

      if(doacaoCartaoCadastradoUserLogadoEntity.valorPlataforma == ""){
        doacaoCartaoCadastradoUserLogadoEntity.valorPlataforma = 0
      };
      
      this.campanhaService.realizarDoacaoCartao(doacaoCartaoCadastradoUserLogadoEntity, this.campanha.id)
      .subscribe(
        sucesso => { this.processarSucessoCartaoCadastrado(sucesso) },
        falha => { this.processarFalhaCartao(falha) }
        );
    }
      
    processarSucesso(response: any) {
      this.spinner.hide();
      this.doacaoForm.reset();
      this.errors = [];
      
      let toast = this.toastr.success('Doação em andamento. Verifique o status em MINHAS DOAÇÕES!', 'Processando!');
      if (toast) {
        toast.onHidden.subscribe(() => {
          this.router.navigate(['doacoes/minhas-doacoes']);
        });
      }
    }

    processarSucessoCartaoNovo(response: any) {
      this.spinner.hide();
      this.cartaoNovoForm.reset();
      this.errors = [];
      
      let toast = this.toastr.success('Doação em andamento. Verifique o status em MINHAS DOAÇÕES!', 'Processando!');
      if (toast) {
        toast.onHidden.subscribe(() => {
          this.router.navigate(['doacoes/minhas-doacoes']);
        });
      }
    }

    processarSucessoBoleto(response: any) {
      this.spinner.hide();
      this.errors = [];
      this.payment_method_toggle = "";
      this.queroDoarToggle = false;
      
      if(this.userLogado){
        this.boletoUserLogadoForm.reset();
        let toast = this.toastr.success('Você será redirecionado para impressão do boleto.', 'Boleto Emitido!');
        if (toast) {
          toast.onHidden.subscribe(() => {
            this.router.navigate(['doacoes/minhas-doacoes']);
          });
        }
      } else {
        this.boletoForm.reset();
        window.scrollTo(0, 0);
        this.toastr.success('Verifique seu e-mail para instruções de pagamento.', 'Boleto Emitido!');
      }
    }

    processarSucessoAssinatura(response: any) {
      this.spinner.hide();
      this.errors = [];
      this.payment_method_toggle = "";
      this.queroDoarToggle = false;
      this.queroAssinarToggle = false;
      this.queroDoarCartaoNovoToggle = false;
      this.queroDoarCartaoCadastradoToggle = false;
      
      this.assinaturaUserLogadoForm.reset();
      let toast = this.toastr.success('Assinatura realizada.', 'Sucesso!');
      if (toast) {
        toast.onHidden.subscribe(() => {
          this.router.navigate(['assinaturas/minhas-assinaturas']);
        });
      }
    }
      
    processarSucessoPix(response: any, qrCode: TemplateRef<any>) {
      this.spinner.hide();
      console.log("Link do QR CODE: " + response);
      this.pixForm.reset();
      this.errors = [];
      this.qrCodeLink = response.url;
      this.qrCodeCopiaCola = response.copiaCola;

      this.openModal(qrCode);
    }

    processarSucessoCartao(response: any) {
      this.spinner.hide();
      this.errors = [];
      this.payment_method_toggle = "";
      this.queroDoarToggle = false;
      
      if(this.userLogado){
        this.cartaoUserLogadoForm.reset();
        let toast = this.toastr.success('Doaçao realizada.', 'Sucesso!');
        if (toast) {
          toast.onHidden.subscribe(() => {
            this.router.navigate(['doacoes/minhas-doacoes']);
          });
        }
      } else {
        this.cartaoForm.reset();
        window.scrollTo(0, 0);
        this.toastr.success('Verifique seu e-mail para instruções de acesso.', 'Pagamento Realizado!');
      }
    }

    copyText(){
      let selBox = document.createElement('textarea');
      selBox.style.position = 'fixed';
      selBox.style.left = '0';
      selBox.style.top = '0';
      selBox.style.opacity = '0';
      selBox.value = this.qrCodeCopiaCola;
      document.body.appendChild(selBox);
      selBox.focus();
      selBox.select();
      document.execCommand('copy');
      document.body.removeChild(selBox);
    }

    processarSucessoCartaoCadastrado(response: any) {
      this.spinner.hide();
      this.errors = [];
      this.payment_method_toggle = "";
      this.queroDoarToggle = false;
      
      this.cartaoCadastradoUserLogadoForm.reset();
      let toast = this.toastr.success('Doaçao realizada.', 'Sucesso!');
      if (toast) {
        toast.onHidden.subscribe(() => {
          this.router.navigate(['doacoes/minhas-doacoes']);
        });
      }
    }

    processarSucessoPixRapido(response: any, qrCode: TemplateRef<any>) {
      this.spinner.hide();
      this.customerForm.reset();
      this.pixRapidoForm.reset();
      this.errors = [];
      console.log("RESPONSE: " + response)
      this.qrCodeLink = response.url;
      this.qrCodeCopiaCola = response.copiaCola;

      this.openModal(qrCode);
    }

    processarSucessoPixUserLogadoRapido(response: any, qrCodeUserLogado: TemplateRef<any>) {
      this.spinner.hide();
      this.customerUserLogadoForm.reset();
      this.pixRapidoUserLogadoForm.reset();
      this.errors = [];
      console.log("RESPONSE: " + response);
      this.qrCodeLink = response.url;
      this.qrCodeCopiaCola = response.copiaCola;

      this.openModal(qrCodeUserLogado);
    }

    processarSucessoPixUserLogado(response: any, qrCodeUserLogado: TemplateRef<any>) {
      this.spinner.hide();
      this.customerUserLogadoForm.reset();
      this.pixUserLogadoForm.reset();
      this.errors = [];
      console.log("RESPONSE: " + response);
      this.qrCodeLink = response.url;
      this.qrCodeCopiaCola = response.copiaCola;

      this.openModal(qrCodeUserLogado);
    }

    processarFalha(fail: any) {
      this.spinner.hide();
      this.customerForm.reset();
      this.pixRapidoForm.reset();
      this.errors = fail.error;
      this.toastr.error(fail.error.errors[0], 'Erro!');
    }

    processarFalhaBoleto(fail: any) {
      this.spinner.hide();
      this.errors = fail.error;
      this.toastr.error(fail.error.errors[0], 'Erro!');
    }

    processarFalhaCartao(fail: any) {
      this.spinner.hide();
      this.errors = fail.error;
      this.toastr.error(fail.error.errors[0], 'Erro!');
    }

    processarFalhaPixRapido(fail: any) {
      this.spinner.hide();
      this.errors = fail.error;
      this.toastr.error(fail.error.errors[0], 'Erro!');
    }

    processarFalhaPix(fail: any) {
      this.spinner.hide();
      this.errors = fail.error;
      this.toastr.error(fail.error.errors[0], 'Erro!');
    }

    naoQueroDoarSemCadastro(){
      this.doarSemCadastroToggle = false;
      // this.cartaoToggle = "";
      // this.payment_method_toggle = "";
    }  
    
    private createConnection = () => {
      this.hubConnection = new signalR.HubConnectionBuilder()
                              //.withUrl('https://localhost:44302/pix-response')
                              .withUrl('https://vaquinhaanimal.azurewebsites.net/pix-response')
                              .build();
    }
  
    private pixListener = () => {
      this.hubConnection.on('pixIsPaid', (data: any) => {
        console.log("Status do pagamento: " + data);

        this.closeModalSignalR();

        if(data == true){
          let toast = this.toastr.success('Recebemos o seu PIX', 'Sucesso!');
            if (toast) {
              toast.onHidden.subscribe(() => {
                this.router.navigate(['doacoes/minhas-doacoes']);
              });
            }
        } else if(data == false){
          let toast = this.toastr.error('Pix não reconhecido, tente em outro momento.', 'Erro!');
          if (toast) {
            toast.onHidden.subscribe(() => {
              this.router.navigate(['doacoes/minhas-doacoes']);
            });
          }
        }
      });

      //this.stopConnection(); 
    }
  
    private startConnection = () => {
      this.hubConnection
        .start()
        .then(() => console.log('Connection started'))
        .catch(err => console.log('Error while starting connection: ' + err))
    }

    private stopConnection = () => {
      this.hubConnection
        .stop()
        .then(() => console.log('Connection stoped'))
        .catch(err => console.log('Error while stoping connection: ' + err))
    }
}