import { Component, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { CampanhaService } from '../campanha.service';
import { Campanha } from '../model/Campanha';
import { NgxSpinnerService } from 'ngx-spinner';
import { environment } from 'src/environments/environment';
import { Dimensions, ImageCroppedEvent, ImageTransform } from 'ngx-image-cropper';
import { Bancos } from '../model/Bancos';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-edit',
  templateUrl: './edit.component.html'
})
export class EditComponent implements OnInit {
  
  private _jsonURL = '../../../assets/bancos.json';
  
  get imagensArray(): FormArray {
    return <FormArray>this.campanhaForm.get('imagens');
  }
  
  imgSrc: string = "assets/img/causes_1.jpg";
  descricao_curta_card: string = "";
  titulo_card: string = "";
  tag_campanha: any = "";
  imagens: string = environment.imagensUrl;
  errors: any[] = [];
  campanhaForm!: FormGroup;
  beneficiarioForm!: FormGroup;
  campanha!: Campanha;
  document_mask: string = "000.000.000-00";
  document_toggle: string = "individual";
  campanhaRecorrente: boolean = false;
  bancos: Bancos[] = [];
  
  imageBase64: any;
  imagemPreview: any;
  imagemNome!: string;
  imagemOriginalSrc!: string;
  
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
  // FIM DA IMAGEM
  
  constructor(
    private fb: FormBuilder,
    private campanhaService: CampanhaService,
    private router: Router,
    private http: HttpClient,
    private route: ActivatedRoute,
    private spinner: NgxSpinnerService,
    private toastr: ToastrService) { 
      this.campanha = this.route.snapshot.data['campanha']; 
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
      this.patchForms();      
      
      this.imagemOriginalSrc = this.imagens + this.campanha.imagens[0].arquivo;
      this.imgSrc = this.imagens + this.campanha.imagens[0].arquivo;
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
    
    patchForms(){
      this.campanhaForm.patchValue({
        id: this.campanha.id,
        descricao_curta: this.campanha.descricao_curta,
        descricao_longa: this.campanha.descricao_longa,
        premium: this.campanha.premium,
        tag_campanha: this.campanha.tag_campanha,
        termos: this.campanha.termos,
        tipo_campanha: this.campanha.tipo_campanha,
        titulo: this.campanha.titulo,
        valor_desejado: this.campanha.valor_desejado,
        video_url: this.campanha.video_url,
        // duracao_dias: this.campanha.duracao_dias,
        data_inicio: this.campanha.data_inicio,
        data_encerramento: this.campanha.data_encerramento,
      });

      this.patchDuracao();
      
      this.beneficiarioForm.patchValue({
        id: this.campanha.beneficiario.id,
        nome: this.campanha.beneficiario.nome,
        documento: this.campanha.beneficiario.documento,
        tipo: this.campanha.beneficiario.tipo,
        codigo_banco: this.campanha.beneficiario.codigo_banco,
        numero_agencia: this.campanha.beneficiario.numero_agencia,
        digito_agencia: this.campanha.beneficiario.digito_agencia,
        numero_conta: this.campanha.beneficiario.numero_conta,
        digito_conta: this.campanha.beneficiario.digito_conta,
        tipo_conta: this.campanha.beneficiario.tipo_conta
      });

      this.titulo_card = this.campanha.titulo;
      this.descricao_curta_card = this.campanha.descricao_curta;
      this.tag_campanha = this.campanha.tag_campanha;
    }

    patchDuracao(){
      if(this.campanha.tipo_campanha == 1) {
        this.campanhaForm.get('duracao_dias')?.setValue(this.campanha.duracao_dias);
        this.campanhaRecorrente = false;
      } else if(this.campanha.tipo_campanha == 2){
        this.campanhaRecorrente = true;
      }
    }
    
    setDocumentValidation(){
      this.beneficiarioForm.controls['documento'].clearValidators();
      this.beneficiarioForm.controls['documento'].setValue("");
      
      if (this.document_toggle == "CPF"){
        this.beneficiarioForm.controls['documento'].setValidators([Validators.required, Validators.minLength(11), Validators.maxLength(11)]);
      } else if(this.document_toggle == "CNPJ"){
        this.beneficiarioForm.controls['documento'].setValidators([Validators.required, Validators.minLength(14), Validators.maxLength(16)]);
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
    
    editarCampanha() {
      this.spinner.show();
      
      if (this.campanhaForm.dirty && this.campanhaForm.valid) {
        this.campanha = Object.assign({}, this.campanha, this.campanhaForm.value);
        
        this.campanha.imagens.forEach(imagem => {
          imagem.arquivo_upload = this.croppedImage.split(',')[1];
          imagem.arquivo = this.imagemNome;
        });
        
        console.log(this.campanha);
        
        if (this.campanha.data_inicio) { this.campanha.data_inicio = new Date(this.campanha.data_inicio); } else { this.campanha.data_inicio = null!; }
        if (this.campanha.data_encerramento) { this.campanha.data_encerramento = new Date(this.campanha.data_encerramento); } else { this.campanha.data_encerramento = null!; }
        this.campanha.termos = this.campanha.termos.toString() == "true";
        this.campanha.premium = this.campanha.premium.toString() == "true";
        this.campanha.tipo_campanha = Number(this.campanha.tipo_campanha)
        this.campanha.tag_campanha = Number(this.campanha.tag_campanha)
        
        this.campanhaService.atualizarCampanha(this.campanha)
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
      
      let toast = this.toastr.success('Campanha atualizada com sucesso!', 'Sucesso!');
      if (toast) {
        toast.onHidden.subscribe(() => {
          this.router.navigate(['campanhas/minhas-campanhas']);
        });
      }
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
      
    processarFalha(fail: any) {
      this.spinner.hide();
      this.errors = fail.error.errors;
      
      this.toastr.error(this.errors[0], "Erro!");
    }
    
    documentSelected(event: any){
      console.log(event.target.value);
      
      if(event.target.value == "individual"){
        this.document_mask = "000.000.000-00";
        this.document_toggle = "individual";
      } else if(event.target.value == "company"){
        this.document_mask = "00.000.000/0000-00"
        this.document_toggle = "company";
      }
      
      this.setDocumentValidation();
    }
    
    fileChangeEvent(event: any): void {
      this.campanhaForm.markAsDirty();
      this.removerFoto();

      if (event.target.files && event.target.files[0]) {
        this.imgSrc = URL.createObjectURL(event.target.files[0]);
      }

      this.imagemNome = event.currentTarget.files[0].name;
      this.adicionarImagem();

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
}    