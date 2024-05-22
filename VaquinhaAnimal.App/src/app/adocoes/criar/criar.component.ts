import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { Adocao } from '../model/Adocao';
import { AdocaoService } from '../adocao.service';
import { DOCUMENT } from '@angular/common';

@Component({
  selector: 'app-criar-adocoes',
  templateUrl: './criar.component.html'
})
export class CriarComponent implements OnInit {
  
  imgSrc: string = "assets/img/causes_1.jpg";
  descricao_curta_card: string = "";
  nome_pet_card: string = "";
  celular_card: string = "";
  tipo_pet_card: any = "";
  tipoAnuncianteToggle: string = "";
  
  // VARIÁVEIS PARA IMAGEM
  imageChangedEvent: any = '';
  croppedImage: any = '';
  imageUrl!: string;
  imagemNome!: string;
  // FIM DA IMAGEM
  
  errors: any[] = [];
  adocaoForm!: FormGroup;
  adocao!: Adocao;
  
  constructor(
    private fb: FormBuilder,
    private adocaoService: AdocaoService,
    private spinner: NgxSpinnerService, 
    private router: Router,
    private toastr: ToastrService,
    @Inject(DOCUMENT) private _document: any) { }
    
    ngOnInit(): void {
      this.createForm();

      var window = this._document.defaultView;
      window.scrollTo(0, 0);
    }
    
    createForm(){
      this.adocaoForm = this.fb.group({
        nome_pet: ['', [Validators.required, Validators.maxLength(50), Validators.minLength(3)]],
        email: ['', [Validators.email, Validators.maxLength(100), Validators.minLength(3)]],
        celular: ['', [Validators.maxLength(20), Validators.minLength(6)]],
        instagram: [''],
        facebook: [''],
        descricao: ['', [Validators.maxLength(1000), Validators.minLength(5)]],
        link_video: [''],
        tipo_pet: ['', Validators.required],
        tipo_anunciante: ['', Validators.required],
        faixa_etaria: ['', Validators.required],
        castrado: ['', Validators.required],
        particular_nome: ['', [Validators.maxLength(50), Validators.minLength(3)]],
        abrigo_nome: ['', [Validators.maxLength(50), Validators.minLength(3)]],
        empresa_nome: ['', [Validators.maxLength(50), Validators.minLength(3)]],
        adotado: [false],
        foto: [''],
        foto_upload: ['']
      });
    }

    anuncianteSelected(event: any){
      if(event.target.value == 1){
        this.tipoAnuncianteToggle = "abrigo";
      } 
      else if(event.target.value == 2){
        this.tipoAnuncianteToggle = "empresa"
      } 
      else if(event.target.value == 3){
        this.tipoAnuncianteToggle = "particular"
      } 
    }
    
    adicionarAdocao() {
      this.spinner.show();
      
      if(!this.adocaoForm.valid){
        this.spinner.hide();
        this.toastr.error("Preencha todos os campos requeridos", "Formulário inválido!");
      }
      
      if (this.adocaoForm.dirty && this.adocaoForm.valid) {
        this.adocao = Object.assign({}, this.adocao, this.adocaoForm.value);

        console.log(this.adocao);

        this.adocao.foto_upload = this.croppedImage.split(',')[1];
        this.adocao.foto = this.imagemNome;
        
        // CONVERSÕES
        this.adocao.tipo_pet = Number(this.adocao.tipo_pet);
        this.adocao.tipo_anunciante = Number(this.adocao.tipo_anunciante);
        this.adocao.faixa_etaria = Number(this.adocao.faixa_etaria);
        this.adocao.castrado = this.adocao.castrado.toString() == "true";
        this.adocao.adotado = this.adocao.adotado.toString() == "true";
        
        this.adocaoService.novaAdocao(this.adocao)
        .subscribe(
          sucesso => { this.processarSucesso(sucesso) },
          falha => { this.processarFalha(falha) }
          );
        }
    }
      
    processarSucesso(response: any) {
      this.spinner.hide();
      this.adocaoForm.reset();
      this.errors = [];
      
      let toast = this.toastr.success('Adoção cadastrada com sucesso!', 'Sucesso!');
      if (toast) {
        toast.onHidden.subscribe(() => {
          this.router.navigate(['adocoes/meus-pets']);
        });
      }
    }
    
    processarFalha(fail: any) {
      this.spinner.hide();
      this.errors = fail.error.errors;
      this.toastr.error(this.errors[0], "Erro!");
    }
    
    fileChangeEvent(event: any): void {

      this.adocaoForm.markAsDirty();

      if (event.target.files && event.target.files[0]) {
        this.imgSrc = URL.createObjectURL(event.target.files[0]);
      }

      this.imagemNome = event.currentTarget.files[0].name;

      const reader = new FileReader();
      reader.readAsDataURL(event.target.files[0]);
      reader.onload = () => {
          if(event.target.files[0].size > 3145728){
            this.toastr.error("A imagem deve ter menos do que 3 mb.");
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
      this.imgSrc = "assets/img/causes_1.jpg";
    }
}    