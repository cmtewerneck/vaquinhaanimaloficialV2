import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { ArtigoService } from '../artigo.service';
import { Artigo } from '../model/Artigo';

@Component({
  selector: 'app-add',
  templateUrl: './add.component.html'
})
export class AddComponent implements OnInit {

  errors: any[] = [];
  artigoForm!: FormGroup;
  artigo!: Artigo;

  // VARIÁVEIS PARA IMAGEM
  imageUrl!: string;
  imagemNome!: string;
  croppedImage: any = '';
  // FIM DA IMAGEM

  constructor(
    private fb: FormBuilder,
    private artigoService: ArtigoService,
    private spinner: NgxSpinnerService, 
    private router: Router,
    private toastr: ToastrService) { }

    ngOnInit(): void {
      this.createForm();
    }

    createForm(){
      this.artigoForm = this.fb.group({
        titulo: [''],
        resumo: [''],
        escrito_por: [''],
        html: [''],
        foto_capa: [''],
        foto_capa_upload: ['']
      });
    }

    adicionarArtigo() {
      this.spinner.show();

      if (this.artigoForm.dirty && this.artigoForm.valid) {
        this.artigo = Object.assign({}, this.artigo, this.artigoForm.value);

        this.artigo.foto_capa_upload = this.croppedImage.split(',')[1];
        this.artigo.foto_capa = this.imagemNome;
        
        this.artigoService.novoArtigo(this.artigo)
        .subscribe(
          sucesso => { this.processarSucesso(sucesso) },
          falha => { this.processarFalha(falha) }
          );
        }
      }
      
      processarSucesso(response: any) {
        this.spinner.hide();
        this.artigoForm.reset();
        this.errors = [];
        
        this.toastr.success('Artigo cadastrado com sucesso!', 'Sucesso!');
      }
      
      processarFalha(fail: any) {
        this.spinner.hide();
        this.errors = fail.error.errors;
        this.toastr.error(this.errors[0], "Erro!");
      }

      fileChangeEvent(event: any): void {

        this.artigoForm.markAsDirty();
  
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
      }

}