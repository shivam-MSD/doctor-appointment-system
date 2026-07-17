import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MainLayoutComponent } from './shared/components/main-layout/main-layout.component';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { DoctorRegisterComponent } from './features/auth/doctor-register/doctor-register.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { BookComponent } from './features/appointments/book/book.component';
import { ProfileComponent } from './features/profile/profile.component';
import { PatientsComponent } from './features/patients/patients.component';

// Doctor feature pages
import { ClinicsComponent } from './features/doctor/clinics/clinics.component';
import { ClinicAdminsComponent } from './features/doctor/clinic-admins/clinic-admins.component';
import { DoctorAppointmentsComponent } from './features/doctor/appointments/doctor-appointments.component';
import { DoctorRequestsComponent } from './features/doctor/requests/doctor-requests.component';

// Super Admin feature pages
import { SuperAdminDashboardComponent } from './features/superadmin/dashboard/super-admin-dashboard.component';
import { SuperAdminDoctorsComponent } from './features/superadmin/doctors/super-admin-doctors.component';
import { SuperAdminClinicsComponent } from './features/superadmin/clinics/super-admin-clinics.component';
import { SuperAdminAdminsComponent } from './features/superadmin/admins/super-admin-admins.component';
import { PatientDoctorsComponent } from './features/patient/doctors/patient-doctors.component';
import { CareTeamComponent } from './features/patient/care-team/care-team.component';
import { AuthGuard } from './core/guards/auth.guard';

const routes: Routes = [
  // Separate Portal Routes
  { path: 'patient/login', component: LoginComponent, data: { role: 'Patient' } },
  { path: 'patient/register', component: RegisterComponent },
  { path: 'doctor/login', component: LoginComponent, data: { role: 'Doctor' } },
  { path: 'doctor/register', component: DoctorRegisterComponent },
  { path: 'admin/login', component: LoginComponent, data: { role: 'Admin' } },
  { path: 'superadmin/login', component: LoginComponent, data: { role: 'SuperAdmin' } },

  // Default fallbacks to Patient Portal
  { path: 'login', redirectTo: 'patient/login', pathMatch: 'full' },
  { path: 'register', redirectTo: 'patient/register', pathMatch: 'full' },

  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      { path: '', redirectTo: 'login', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent }, // Shared fallback route

      // Patient Routes
      { path: 'patient/dashboard', component: DashboardComponent },
      { path: 'patient/history', component: DashboardComponent, data: { historyOnly: true } },
      { path: 'patient/book-appointment', component: BookComponent },
      { path: 'patient/profile', component: ProfileComponent },
      { path: 'patient/doctors', component: PatientDoctorsComponent },
      { path: 'patient/care-team', component: CareTeamComponent },
      { path: 'patient/audit-logs', loadComponent: () => import('./features/doctor/audit-logs/audit-logs.component').then(m => m.AuditLogsComponent) },

      // Doctor Routes
      { path: 'doctor/dashboard', component: DashboardComponent },
      { path: 'doctor/appointments', component: DoctorAppointmentsComponent },
      { path: 'doctor/requests', component: DoctorRequestsComponent },
      { path: 'doctor/patients', component: PatientsComponent },
      { path: 'doctor/clinics', component: ClinicsComponent },
      { path: 'doctor/admins', component: ClinicAdminsComponent },
      { path: 'doctor/profile', component: ProfileComponent },
      { path: 'doctor/audit-logs', loadComponent: () => import('./features/doctor/audit-logs/audit-logs.component').then(m => m.AuditLogsComponent) },

      // Clinic Admin Routes
      { path: 'admin/dashboard', component: DashboardComponent },
      { path: 'admin/appointments', component: DoctorAppointmentsComponent },
      { path: 'admin/profile', component: ProfileComponent },
      { path: 'admin/audit-logs', loadComponent: () => import('./features/doctor/audit-logs/audit-logs.component').then(m => m.AuditLogsComponent) },

      // Super Admin Routes
      { path: 'superadmin/dashboard', component: SuperAdminDashboardComponent },
      { path: 'superadmin/doctors', component: SuperAdminDoctorsComponent },
      { path: 'superadmin/clinics', component: SuperAdminClinicsComponent },
      { path: 'superadmin/admins', component: SuperAdminAdminsComponent },
      { path: 'superadmin/audit-logs', loadComponent: () => import('./features/doctor/audit-logs/audit-logs.component').then(m => m.AuditLogsComponent) }
    ]
  },
  { path: '**', redirectTo: 'login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
