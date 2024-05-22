import { Component, Inject, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Dimensions, ImageCroppedEvent, ImageTransform } from 'ngx-image-cropper';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { Campanha, TagCampanhaEnum } from '../model/Campanha';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Bancos } from '../model/Bancos';
import { CampanhaService } from '../campanha.service';
import { DOCUMENT } from '@angular/common';

@Component({
  selector: 'app-criar-campanhas',
  templateUrl: './criar.component.html'
})
export class CriarComponent implements OnInit {
  
  imgSrc: string = "assets/img/causes_1.jpg";
  descricao_curta_card: string = "";
  titulo_card: string = "";
  tag_campanha: any = "";
  private _jsonURL = '../../../assets/bancos.json';
  document_mask: string = "000.000.000-00";
  document_toggle: string = "CPF";
  
  get imagensArray(): FormArray {
    return <FormArray>this.campanhaForm.get('imagens');
  }
  
  // VARIÁVEIS PARA IMAGEM
  imageChangedEvent: any = '';
  croppedImage: any = '';
  canvasRotation = 0;
  rotation = 0;
  scale = 1;
  showCropper = false;
  containWithinAspectRatio = false;
  transform: ImageTransform = {};
  imageUrl!: string;
  imagemNome!: string;
  // FIM DA IMAGEM
  
  errors: any[] = [];
  campanhaForm!: FormGroup;
  beneficiarioForm!: FormGroup;
  campanha!: Campanha;
  campanhaRecorrente: boolean = false;
  bancos: Bancos[] = [];
  
  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private campanhaService: CampanhaService,
    private spinner: NgxSpinnerService, 
    private router: Router,
    private toastr: ToastrService,
    @Inject(DOCUMENT) private _document: any) { 
      this.getJSON().subscribe(data => {
        this.bancos = data;
        console.log(data);
      });
    }
    
    public getJSON(): Observable<any> {
      return this.http.get(this._jsonURL);
    }
    
    ngOnInit(): void {
      this.createForm();

      var window = this._document.defaultView;
      window.scrollTo(0, 0);
    }
    
    createForm(){
      this.campanhaForm = this.fb.group({
        titulo: ['', [Validators.required, Validators.maxLength(100), Validators.minLength(3)]],
        tipo_campanha: ['', Validators.required],
        tag_campanha: ['', Validators.required],
        duracao_dias: [''],
        valor_desejado: ['', Validators.required],
        descricao_curta: ['', [Validators.required, Validators.maxLength(200), Validators.minLength(5)]],
        descricao_longa: ['', [Validators.required, Validators.maxLength(5000), Validators.minLength(500)]],
        video_url: [''],
        data_inicio: [''],
        data_encerramento: [''],
        termos: [false, [Validators.required, Validators.requiredTrue]],
        premium: [false, Validators.required],
        imagens: this.fb.array([]),
        beneficiario: this.beneficiarioForm = this.fb.group({
          nome: ['', [Validators.required, Validators.maxLength(100)]],
          documento: [''],
          tipo: ['', Validators.required],
          codigo_banco: ['', [Validators.required, Validators.maxLength(3)]],
          tipo_conta: ['', Validators.required],
          numero_agencia: ['', [Validators.required, Validators.maxLength(4), Validators.pattern("[0-9]+")]],
          digito_agencia: ['', [Validators.maxLength(1), Validators.pattern("[0-9]+")]],
          numero_conta: ['', [Validators.required, Validators.maxLength(13), Validators.pattern("[0-9]+")]],
          digito_conta: ['', [Validators.required, Validators.maxLength(2), Validators.pattern("[0-9]+")]],
          recebedor_id: ['']
        })
      });
    }
    
    documentSelected(event: any){
      console.log(event.target.value);
      
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
      this.beneficiarioForm.controls['documento'].clearValidators();
      this.beneficiarioForm.controls['documento'].setValue("");
      
      if (this.document_toggle == "CPF"){
        this.beneficiarioForm.controls['documento'].setValidators([Validators.required, Validators.minLength(11), Validators.maxLength(11)]);
      } else if(this.document_toggle == "CNPJ"){
        this.beneficiarioForm.controls['documento'].setValidators([Validators.required, Validators.minLength(14), Validators.maxLength(14)]);
      } 
      
      this.beneficiarioForm.controls['documento'].updateValueAndValidity();
      
      console.log(this.beneficiarioForm);
    }
    
    criaImagem(documento: any): FormGroup {
      return this.fb.group({
        id: [documento.id],
        tipo: [1],
        arquivo: [''],
        arquivo_upload: ['']
      });
    }
    
    adicionarImagem(){
      this.imagensArray.push(this.criaImagem({ id: "00000000-0000-0000-0000-000000000000" }));
    }
    
    removerImagem(id: number){
      this.imagensArray.removeAt(id);
    }
    
    adicionarCampanha() {
      this.spinner.show();
      
      if(!this.campanhaForm.valid){
        this.spinner.hide();
        this.toastr.error("Preencha todos os campos requeridos", "Formulário inválido!");
      }
      
      if (this.campanhaForm.dirty && this.campanhaForm.valid) {
        this.campanha = Object.assign({}, this.campanha, this.campanhaForm.value);
        
        this.campanha.imagens.forEach(imagem => {
          imagem.arquivo_upload = this.croppedImage.split(',')[1];
          imagem.arquivo = this.imagemNome;
        });
        
        // CONVERSÕES
        if (this.campanha.data_inicio) { this.campanha.data_inicio = new Date(this.campanha.data_inicio); } else { this.campanha.data_inicio = null!; }
        if (this.campanha.data_encerramento) { this.campanha.data_encerramento = new Date(this.campanha.data_encerramento); } else { this.campanha.data_encerramento = null!; }
        this.campanha.termos = this.campanha.termos.toString() == "true";
        this.campanha.premium = this.campanha.premium.toString() == "true";
        this.campanha.tipo_campanha = Number(this.campanha.tipo_campanha)
        this.campanha.tag_campanha = Number(this.campanha.tag_campanha)
        // this.campanha.beneficiario.numero_agencia = this.campanha.beneficiario.numero_agencia.replace(/\D/g, '');
        // this.campanha.beneficiario.digito_agencia = this.campanha.beneficiario.numero_agencia.replace(/\D/g, '');
        // this.campanha.beneficiario.numero_conta = this.campanha.beneficiario.numero_agencia.replace(/\D/g, '');
        // this.campanha.beneficiario.digito_conta = this.campanha.beneficiario.numero_agencia.replace(/\D/g, '');
        
        this.campanhaService.novaCampanha(this.campanha)
        .subscribe(
          sucesso => { this.processarSucesso(sucesso) },
          falha => { this.processarFalha(falha) }
          );
        }
    }
      
    processarSucesso(response: any) {
      this.spinner.hide();
      this.campanhaForm.reset();
      this.errors = [];
      
      let toast = this.toastr.success('Campanha cadastrada com sucesso!', 'Sucesso!');
      if (toast) {
        toast.onHidden.subscribe(() => {
          this.router.navigate(['campanhas/minhas-campanhas']);
        });
      }
    }
    
    processarFalha(fail: any) {
      this.spinner.hide();
      this.errors = fail.error.errors;
      this.toastr.error(this.errors[0], "Erro!");
    }
    
    fileChangeEvent(event: any): void {
      this.removerFoto();

      if (event.target.files && event.target.files[0]) {
        this.imgSrc = URL.createObjectURL(event.target.files[0]);
      }

      this.imagemNome = event.currentTarget.files[0].name;
      this.adicionarImagem();

      const reader = new FileReader();
      reader.readAsDataURL(event.target.files[0]);
      reader.onload = () => {
          if(event.target.files[0].size > 2097152){
            this.toastr.error("A imagem deve ter no máximo 2 mb.");
            this.removerFoto();
            return;
          }

          if(event.target.files[0].type != "image/png" && event.target.files[0].type != "image/jpg" && event.target.files[0].type != "image/jpeg"){
            this.toastr.error("Formato inválido. Aceitos (png, jpg e jpeg)");
            this.removerFoto();
            return;
          }

          this.croppedImage = reader.result;
      };
    }
    
    removerFoto(){
      this.imagemNome = "";
      this.removerImagem(0);
      this.imgSrc = "assets/img/causes_1.jpg";
    }
    
    tipoCampanhaSelected(event: any){
      if(event.target.value == 1){
        this.campanhaRecorrente = false;
      } else if(event.target.value == 2){
        this.campanhaRecorrente = true;
        this.campanhaForm.get('duracao_dias')?.setValue(null);
      }

      this.setDuracaoValidation(event.target.value)
    }

    setDuracaoValidation(tipo: number){
      this.campanhaForm.controls['duracao_dias'].clearValidators();
      
      // TIPO == 1 - Campanha única, com duração
      if (tipo == 1){
        this.campanhaForm.controls['duracao_dias'].setValidators(Validators.required);
      } 

      this.campanhaForm.controls['duracao_dias'].updateValueAndValidity();
    }
}    