import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

// Core layout & auth pages
import { MainLayoutComponent } from './shared/components/main-layout/main-layout.component';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { DoctorRegisterComponent } from './features/auth/doctor-register/doctor-register.component';
import { ForgotPasswordComponent } from './features/auth/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './features/auth/reset-password/reset-password.component';

// Dashboard & feature pages
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { BookComponent } from './features/appointments/book/book.component';
import { DoctorAppointmentsComponent } from './features/doctor/appointments/doctor-appointments.component';
import { DoctorRequestsComponent } from './features/doctor/requests/doctor-requests.component';
import { PatientsComponent } from './features/patients/patients.component';
import { ProfileComponent } from './features/profile/profile.component';

// Doctor feature pages
import { ClinicsComponent } from './features/doctor/clinics/clinics.component';
import { ClinicAdminsComponent } from './features/doctor/clinic-admins/clinic-admins.component';

// Super Admin feature pages
import { SuperAdminDashboardComponent } from './features/superadmin/dashboard/super-admin-dashboard.component';
import { SuperAdminDoctorsComponent } from './features/superadmin/doctors/super-admin-doctors.component';
import { SuperAdminClinicsComponent } from './features/superadmin/clinics/super-admin-clinics.component';
import { SuperAdminAdminsComponent } from './features/superadmin/admins/super-admin-admins.component';
import { PatientDoctorsComponent } from './features/patient/doctors/patient-doctors.component';
import { AccountSecurityComponent } from './shared/components/account-security/account-security.component';
import { AuthGuard } from './core/guards/auth.guard';

const routes: Routes = [
  // Top-Level Default Redirects (Unauthenticated)
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'reset-password', component: ResetPasswordComponent },
  { path: 'account/security', component: AccountSecurityComponent },

  // Dedicated Portal Routes
  { path: 'patient/login', component: LoginComponent, data: { role: 'Patient' } },
  { path: 'patient/register', component: RegisterComponent },
  { path: 'doctor/login', component: LoginComponent, data: { role: 'Doctor' } },
  { path: 'doctor/register', component: DoctorRegisterComponent },
  { path: 'admin/login', component: LoginComponent, data: { role: 'Admin' } },
  { path: 'superadmin/login', component: LoginComponent, data: { role: 'SuperAdmin' } },

  // Authenticated Main Layout Routes
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      { path: 'dashboard', component: DashboardComponent }, // Shared fallback route

      // Patient Routes
      { path: 'patient/dashboard', component: DashboardComponent, data: { expectedRole: 'Patient', loginRoute: '/login' } },
      { path: 'patient/history', component: DashboardComponent, data: { expectedRole: 'Patient', loginRoute: '/login', historyOnly: true } },
      { path: 'patient/book-appointment', component: BookComponent, data: { expectedRole: 'Patient', loginRoute: '/login' } },
      { path: 'patient/profile', component: ProfileComponent, data: { expectedRole: 'Patient', loginRoute: '/login' } },
      { path: 'patient/doctors', component: PatientDoctorsComponent, data: { expectedRole: 'Patient', loginRoute: '/login' } },
      { path: 'patient/audit-logs', loadComponent: () => import('./features/doctor/audit-logs/audit-logs.component').then(m => m.AuditLogsComponent), data: { expectedRole: 'Patient', loginRoute: '/login' } },

      // Doctor Routes
      { path: 'doctor/dashboard', component: DashboardComponent, data: { expectedRole: 'Doctor', loginRoute: '/login' } },
      { path: 'doctor/appointments', component: DoctorAppointmentsComponent, data: { expectedRole: 'Doctor', loginRoute: '/login' } },
      { path: 'doctor/completed-appointments', component: DoctorAppointmentsComponent, data: { expectedRole: 'Doctor', loginRoute: '/login', completedOnly: true } },
      { path: 'doctor/requests', component: DoctorRequestsComponent, data: { expectedRole: 'Doctor', loginRoute: '/login' } },
      { path: 'doctor/patients', component: PatientsComponent, data: { expectedRole: 'Doctor', loginRoute: '/login' } },
      { path: 'doctor/clinics', component: ClinicsComponent, data: { expectedRole: 'Doctor', loginRoute: '/login' } },
      { path: 'doctor/admins', component: ClinicAdminsComponent, data: { expectedRole: 'Doctor', loginRoute: '/login' } },
      { path: 'doctor/profile', component: ProfileComponent, data: { expectedRole: 'Doctor', loginRoute: '/login' } },
      { path: 'doctor/audit-logs', loadComponent: () => import('./features/doctor/audit-logs/audit-logs.component').then(m => m.AuditLogsComponent), data: { expectedRole: 'Doctor', loginRoute: '/login' } },

      // Clinic Admin Routes
      { path: 'admin/dashboard', component: DashboardComponent, data: { expectedRole: 'Admin', loginRoute: '/admin/login' } },
      { path: 'admin/appointments', component: DoctorAppointmentsComponent, data: { expectedRole: 'Admin', loginRoute: '/admin/login' } },
      { path: 'admin/completed-appointments', component: DoctorAppointmentsComponent, data: { expectedRole: 'Admin', loginRoute: '/admin/login', completedOnly: true } },
      { path: 'admin/profile', component: ProfileComponent, data: { expectedRole: 'Admin', loginRoute: '/admin/login' } },
      { path: 'admin/audit-logs', loadComponent: () => import('./features/doctor/audit-logs/audit-logs.component').then(m => m.AuditLogsComponent), data: { expectedRole: 'Admin', loginRoute: '/admin/login' } },

      // Super Admin Routes
      { path: 'superadmin/dashboard', component: SuperAdminDashboardComponent, data: { expectedRole: 'SuperAdmin', loginRoute: '/superadmin/login' } },
      { path: 'superadmin/doctors', component: SuperAdminDoctorsComponent, data: { expectedRole: 'SuperAdmin', loginRoute: '/superadmin/login' } },
      { path: 'superadmin/clinics', component: SuperAdminClinicsComponent, data: { expectedRole: 'SuperAdmin', loginRoute: '/superadmin/login' } },
      { path: 'superadmin/admins', component: SuperAdminAdminsComponent, data: { expectedRole: 'SuperAdmin', loginRoute: '/superadmin/login' } },
      { path: 'superadmin/audit-logs', loadComponent: () => import('./features/superadmin/audit-logs/superadmin-audit-logs.component').then(m => m.SuperadminAuditLogsComponent), data: { expectedRole: 'SuperAdmin', loginRoute: '/superadmin/login' } }
    ]
  },
  { path: '**', redirectTo: 'login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
