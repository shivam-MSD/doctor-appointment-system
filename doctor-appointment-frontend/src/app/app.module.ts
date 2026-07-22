import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { AuthInterceptor } from './core/interceptors/auth.interceptor';

// Shared Components
import { HeaderComponent } from './shared/components/header/header.component';
import { SidebarComponent } from './shared/components/sidebar/sidebar.component';
import { MainLayoutComponent } from './shared/components/main-layout/main-layout.component';

// Feature Components
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { DoctorRegisterComponent } from './features/auth/doctor-register/doctor-register.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { BookComponent } from './features/appointments/book/book.component';
import { ProfileComponent } from './features/profile/profile.component';
import { PatientsComponent } from './features/patients/patients.component';

// Doctor Feature Sub-components
import { ClinicsComponent } from './features/doctor/clinics/clinics.component';
import { ClinicAdminsComponent } from './features/doctor/clinic-admins/clinic-admins.component';
import { DoctorAppointmentsComponent } from './features/doctor/appointments/doctor-appointments.component';
import { DoctorRequestsComponent } from './features/doctor/requests/doctor-requests.component';

// Super Admin Feature Sub-components
import { SuperAdminDashboardComponent } from './features/superadmin/dashboard/super-admin-dashboard.component';
import { SuperAdminDoctorsComponent } from './features/superadmin/doctors/super-admin-doctors.component';
import { SuperAdminClinicsComponent } from './features/superadmin/clinics/super-admin-clinics.component';
import { SuperAdminAdminsComponent } from './features/superadmin/admins/super-admin-admins.component';
import { PatientDoctorsComponent } from './features/patient/doctors/patient-doctors.component';
import { CareTeamComponent } from './features/patient/care-team/care-team.component';
import { PatientHeaderComponent } from './shared/components/patient-header/patient-header.component';
import { MyDoctorsComponent } from './shared/components/my-doctors/my-doctors.component';
import { DoctorHeaderComponent } from './shared/components/doctor-header/doctor-header.component';
import { AdminHeaderComponent } from './shared/components/admin-header/admin-header.component';
import { SuperadminHeaderComponent } from './shared/components/superadmin-header/superadmin-header.component';
import { ForgotPasswordComponent } from './features/auth/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './features/auth/reset-password/reset-password.component';

@NgModule({
  declarations: [
    AppComponent,
    HeaderComponent,
    SidebarComponent,
    MainLayoutComponent,
    LoginComponent,
    RegisterComponent,
    DoctorRegisterComponent,
    DashboardComponent,
    BookComponent,
    ProfileComponent,
    PatientsComponent,
    ClinicsComponent,
    ClinicAdminsComponent,
    DoctorAppointmentsComponent,
    DoctorRequestsComponent,
    SuperAdminDashboardComponent,
    SuperAdminDoctorsComponent,
    SuperAdminClinicsComponent,
    SuperAdminAdminsComponent,
    PatientDoctorsComponent,
    CareTeamComponent,
    PatientHeaderComponent,
    MyDoctorsComponent,
    DoctorHeaderComponent,
    AdminHeaderComponent,
    SuperadminHeaderComponent,
    ForgotPasswordComponent,
    ResetPasswordComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    FormsModule,
    AppRoutingModule
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
