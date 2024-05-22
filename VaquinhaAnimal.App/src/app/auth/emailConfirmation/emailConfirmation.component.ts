import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { NgxSpinnerService } from 'ngx-spinner';
import { AuthService } from '../auth.service';
import { ConfirmEmail } from '../User';

@Component({
  selector: 'app-email-confirmation',
  templateUrl: './emailConfirmation.component.html'
})
export class EmailConfirmationComponent implements OnInit {
  
  errors: any[] = [];
  user!: ConfirmEmail;
  token!: string;
  username!: string;
  emailConfirmed: boolean = false;
  
  constructor(
    private authService: AuthService, 
    private activatedRoute: ActivatedRoute,
    private spinner: NgxSpinnerService) { 
      this.username = activatedRoute.snapshot.url[1].path; 
      this.token = activatedRoute.snapshot.url[2].path; 
    }
    
    ngOnInit() {
      this.confirmEmail();
    }
    
    confirmEmail() {
      this.spinner.show();
      
      this.authService.confirmEmail(this.username, this.token).subscribe(
        success => { 
          this.processarSucesso(success); 
        },
        error => {
          this.processarFalha(error);
        },
      );
    }
      
    processarSucesso(response: any) {
      this.emailConfirmed = true;
      this.spinner.hide();
      this.errors = [];
    }
    
    processarFalha(fail: any) {
      this.spinner.hide();
    }   
      
}    