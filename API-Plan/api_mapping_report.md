# API Mappings and Usage Report

This document maps all backend API routes defined in the ASP.NET Core project to their corresponding Angular frontend callers, specifying what action/button triggers them and identifying any unused endpoints.

---

## 1. Authentication APIs (`AuthController`)
Base Route: `/api/auth`

| HTTP Method | API Endpoint | Frontend Service & Caller Component | Trigger Action / Purpose | Status |
|---|---|---|---|---|
| **POST** | `/api/auth/register` | `AuthService.register()` <br> ➔ `RegisterComponent` | Fired when a **Patient** fills out the signup form and clicks the **"Sign Up"** button to register. | **Active** |
| **POST** | `/api/auth/register-doctor** | `AuthService.registerDoctor()` <br> ➔ `DoctorRegisterComponent` | Fired when a **Doctor** onboarding registration form is submitted. | **Active** |
| **POST** | `/api/auth/login` | `AuthService.login()` <br> ➔ `LoginComponent` | Fired when clicking the **"Log In"** button to authenticate user credentials. | **Active** |
| **POST** | `/api/auth/verify-email` | `AuthService.verifyEmail()` <br> ➔ `VerifyEmailComponent` | Fired after signup/login to verify the OTP code sent to the email inbox. | **Active** |
| **POST** | `/api/auth/check-email` | `AuthService.checkEmail()` <br> ➔ `LoginComponent` | Triggered as a pre-validation step during login input change or submission to check if the email exists. | **Active** |
| **POST** | `/api/auth/forgot-password` | `AuthService.forgotPassword()` <br> ➔ `ForgotPasswordComponent` | Triggered when a user enters their email and clicks **"Send Reset OTP"**. | **Active** |
| **POST** | `/api/auth/reset-password` | `AuthService.resetPassword()` <br> ➔ `ForgotPasswordComponent` | Fired when entering the received OTP along with a new password. | **Active** |
| **POST** | `/api/auth/initiate-password-update` | `AuthService.initiatePasswordUpdate()` <br> ➔ `ProfileComponent` | Triggered when a logged-in user initiates a password update request inside their profile settings. | **Active** |
| **POST** | `/api/auth/update-password` | `AuthService.updatePassword()` <br> ➔ `ProfileComponent` | Submits the OTP and new password to complete the password update process. | **Active** |

---

## 2. Appointment Booking & Management APIs (`AppointmentsController`)
Base Route: `/api/appointments`

| HTTP Method | API Endpoint | Frontend Service & Caller Component | Trigger Action / Purpose | Status |
|---|---|---|---|---|
| **POST** | `/api/appointments/book` | `AppointmentService.bookAppointment()` <br> ➔ `BookComponent` | Fired when a Patient clicks **"Confirm Booking"** inside the booking form. | **Active** |
| **POST** | `/api/appointments/cancel/{id}` | `AppointmentService.cancelAppointment()` <br> ➔ `DashboardComponent` | Fired when a Patient cancels their pending/upcoming appointment from the dashboard. | **Active** |
| **POST** | `/api/appointments/doctor-cancel/{id}` | `AppointmentService.doctorCancelAppointment()` <br> ➔ `DoctorAppointmentsComponent` | Fired when a Doctor cancels a confirmed appointment with a cancellation reason. | **Active** |
| **GET** | `/api/appointments/admin-doctor-dashboard` | `AppointmentService.getAdminDoctorDashboard()` <br> ➔ `DoctorRequestsComponent` / `DoctorAppointmentsComponent` / `SidebarComponent` | Fetches filtered lists and counts of pending/confirmed appointments for Doctor & Admin views. | **Active** |
| **GET** | `/api/appointments/patient-dashboard` | `AppointmentService.getPatientDashboard()` <br> ➔ `DashboardComponent` | Loads upcoming and historical appointments for the Patient's dashboard view. | **Active** |
| **GET** | `/api/appointments/consulted-doctors` | `AppointmentService.getConsultedDoctors()` <br> ➔ `BookComponent` | Fetches the dropdown of doctors previously consulted by the patient to simplify re-booking. | **Active** |
| **GET** | `/api/appointments/patients-list` | `AppointmentService.getPatientsList()` <br> ➔ `MyPatientsComponent` | Displays list of unique patients treated by the doctor under **"My Patients"** tab. | **Active** |
| **GET** | `/api/appointments/available-doctors` | `AppointmentService.getAvailableDoctors()` | Fetches list of all active/verified doctors in the system. | **Unused (Client-side searches use `/search-doctors` instead)** |
| **GET** | `/api/appointments/booking-details` | `AppointmentService.getBookingDetails()` <br> ➔ `BookComponent` | Retrieves doctor details and clinic window hours when initiating the booking wizard. | **Active** |
| **GET** | `/api/appointments/specializations` | `AppointmentService.getSpecializations()` <br> ➔ Multiple dropdowns | Fetches the full list of medical specializations for search filters and registration dropdowns. | **Active** |
| **GET** | `/api/appointments/search-doctors` | `AppointmentService.searchDoctors()` <br> ➔ `BookComponent` | Performs multi-parameter search/filters for verified doctors in booking screens. | **Active** |
| **GET** | `/api/appointments/doctors/{doctorId}/clinics` | `AppointmentService.getClinicsByDoctorId()` <br> ➔ `BookComponent` | Fetches list of clinics where a specific doctor practices during booking. | **Active** |
| **GET** | `/api/appointments/day-availability` | `AppointmentService.getDayAvailability()` <br> ➔ `BookComponent` | Dynamically checks if the maximum daily appointment limit of the clinic is reached for a given date. | **Active** |
| **POST** | `/api/appointments/approve/{id}` | `AppointmentService.approveAppointment()` <br> ➔ `DoctorRequestsComponent` | Fired when a Doctor clicks **"Approve"** and inputs an assigned time. | **Active** |
| **POST** | `/api/appointments/reject/{id}` | `AppointmentService.rejectAppointment()` <br> ➔ `DoctorRequestsComponent` | Fired when a Doctor clicks **"Reject"** and provides a reason. | **Active** |
| **POST** | `/api/appointments/complete/{id}` | `AppointmentService.completeAppointment()` <br> ➔ `DoctorAppointmentsComponent` | Fired when a Doctor clicks **"Complete Consultation"** and inputs medical notes/report. | **Active** |
| **POST** | `/api/appointments/move-pending/{id}` | `AppointmentService.movePendingAppointment()` <br> ➔ `DoctorAppointmentsComponent` | Fired when a Doctor clicks **"Skip Turn"** (patient late/absent) to reset status to Pending. | **Active** |
| **POST** | `/api/appointments/assign-time/{id}` | `AppointmentService.assignAppointmentTime()` <br> ➔ `DoctorAppointmentsComponent` | Fired when a Doctor updates the scheduled timing of an active appointment. | **Active** |
| **GET** | `/api/appointments/patients/{patientId}` | `AppointmentService.getPatientDetails()` <br> ➔ `DashboardComponent` / `MyPatientsComponent` | Opens the patient profile modal (clinical details, age, blood group) for the doctor. | **Active** |
| **POST** | `/api/appointments/propose-reschedule` | `AppointmentService.proposeReschedule()` <br> ➔ `DoctorAppointmentsComponent` | Fired when a Doctor/Admin proposes a reschedule date and time for a confirmed booking. | **Active** |
| **POST** | `/api/appointments/respond-reschedule` | `AppointmentService.respondToReschedule()` <br> ➔ `DashboardComponent` | Fired when a Patient clicks **"Accept"** or **"Decline"** on a proposed rescheduled time. | **Active** |
| **GET** | `/api/appointments/audit-logs` | `AppointmentService.getAuditLogs()` <br> ➔ `AuditLogsComponent` | Fetches appointment logs (Created, Confirmed, Completed) for the history timeline view. | **Active** |

---

## 3. Clinics & Admins APIs (`ClinicsController` & `AdminController`)
Base Routes: `/api/clinics` and `/api/admin`

| HTTP Method | API Endpoint | Frontend Service & Caller Component | Trigger Action / Purpose | Status |
|---|---|---|---|---|
| **POST** | `/api/clinics/register-only` | `AdminService.registerClinicOnly()` <br> ➔ `ClinicsComponent` | Fired when a Doctor registers a clinic branch location without assigning an administrator. | **Active** |
| **POST** | `/api/clinics/register-admin` | `AdminService.registerClinicAdmin()` <br> ➔ `ClinicAdminsComponent` | Fired when a Doctor registers a new clinic administrator profile. | **Active** |
| **POST** | `/api/clinics/register` | `AdminService.registerClinic()` <br> ➔ `ClinicsComponent` | Fired when a Doctor registers a clinic and assigns a new administrator simultaneously. | **Active** |
| **GET** | `/api/clinics` | `AdminService.getDoctorClinics()` <br> ➔ `ClinicsComponent` | Loads all clinics associated with the currently logged-in Doctor. | **Active** |
| **GET** | `/api/clinics/admins` | `AdminService.getDoctorAdmins()` <br> ➔ `ClinicAdminsComponent` | Loads all admins registered by the currently logged-in Doctor. | **Active** |
| **GET** | `/api/clinics/pending` | `AdminService.getPendingClinics()` <br> ➔ `SuperAdminClinicsComponent` / `SidebarComponent` | Fetches clinics awaiting verification by the Super Admin. | **Active** |
| **GET** | `/api/clinics/pending-admins` | `AdminService.getPendingAdmins()` <br> ➔ `SuperAdminAdminsComponent` / `SidebarComponent` | Fetches admins awaiting verification by the Super Admin. | **Active** |
| **POST** | `/api/clinics/verify-clinic/{id}` | `AdminService.verifyClinic()` <br> ➔ `SuperAdminClinicsComponent` | Fired when the Super Admin clicks **"Approve"** on a pending clinic. | **Active** |
| **POST** | `/api/clinics/verify-clinic/{id}/reject` | `AdminService.rejectClinic()` <br> ➔ `SuperAdminClinicsComponent` | Fired when the Super Admin clicks **"Reject"** on a clinic and types a reason. | **Active** |
| **POST** | `/api/clinics/verify-admin/{id}` | `AdminService.verifyAdmin()` <br> ➔ `SuperAdminAdminsComponent` | Fired when the Super Admin clicks **"Approve"** on a pending administrator. | **Active** |
| **POST** | `/api/clinics/reject-admin/{id}` | `AdminService.rejectAdmin()` <br> ➔ `SuperAdminAdminsComponent` | Fired when the Super Admin clicks **"Reject"** on a pending administrator. | **Active** |
| **PUT** | `/api/clinics/{clinicId}` | `AdminService.updateClinic()` <br> ➔ `ClinicsComponent` | Fired when a Doctor updates the details/availability hours of their clinic. | **Active** |
| **PUT** | `/api/clinics/admin-update` | `AdminService.updateClinicByAdmin()` <br> ➔ `DashboardComponent` | Fired when a Clinic Admin updates their assigned clinic details. | **Active** |
| **GET** | `/api/clinics/my-clinic` | `AdminService.getAdminClinic()` <br> ➔ `DashboardComponent` | Loads the assigned clinic details for the logged-in Clinic Admin. | **Active** |
| **GET** | `/api/clinics/{clinicId}/history` | `AdminService.getClinicHistory()` <br> ➔ `ClinicsComponent` | Fetches verification history log timeline (Approve/Reject entries) for a clinic. | **Active** |
| **GET** | `/api/admin/pending-doctors` | `AdminService.getPendingDoctors()` <br> ➔ `SuperAdminDoctorsComponent` / `SidebarComponent` | Fetches doctors awaiting verification by the Super Admin. | **Active** |
| **POST** | `/api/admin/verify-doctor/{id}` | `AdminService.verifyDoctor()` <br> ➔ `SuperAdminDoctorsComponent` | Fired when the Super Admin clicks **"Approve"** or **"Reject"** on a pending doctor. | **Active** |
| **GET** | `/api/admin/doctors` | `AdminService.getAllDoctors()` <br> ➔ `SuperAdminDoctorsComponent` | Loads and searches all doctors registered in the system. | **Active** |
| **GET** | `/api/admin/clinics` | `AdminService.getAllClinics()` <br> ➔ `SuperAdminClinicsComponent` | Loads and searches all clinics registered in the system. | **Active** |
| **GET** | `/api/admin/admins` | `AdminService.getAllAdmins()` <br> ➔ `SuperAdminAdminsComponent` | Loads and searches all clinic administrators registered in the system. | **Active** |
| **GET** | `/api/admin/system-audit-logs` | HTTP GET call <br> ➔ `SuperAdminAuditLogsComponent` | Loads system-wide audit logs (onboarding histories, clinic/doctor approval states). | **Active** |
| **POST** | `/api/admin/{adminId}/clinics` | `AdminService.assignAdminToClinics()` | Assigns a clinic administrator to a list of clinics. | **Unused (Admin-to-clinic links are configured on clinic registration)** |
| **GET** | `/api/admin/{adminId}/clinics` | `AdminService.getAdminClinics()` | Fetches all clinics assigned to a clinic administrator. | **Unused (Replaced by `my-clinic` calls for Clinic Admins)** |

---

## 4. User Profiles & Family APIs (`UsersController`, `PatientsController`, & `FamilyController`)

| HTTP Method | API Endpoint | Frontend Service & Caller Component | Trigger Action / Purpose | Status |
|---|---|---|---|---|
| **GET** | `/api/users/profile` | `UserService.getProfile()` <br> ➔ `ProfileComponent` | Loads profile information (email, personal details) for the logged-in User. | **Active** |
| **POST** | `/api/users/change-password` | `UserService.changePassword()` <br> ➔ `ProfileComponent` | Fired when updating passwords inside settings. | **Active** |
| **GET** | `/api/users/doctor-profile` | `PatientService.getDoctorProfile()` <br> ➔ `ProfileComponent` | Loads professional profile (licence number, fees, about text) of the logged-in Doctor. | **Active** |
| **PUT** | `/api/users/doctor-profile` | `PatientService.updateDoctorProfile()` <br> ➔ `ProfileComponent` | Fired when a Doctor clicks **"Save Profile"** to update their clinical bio details. | **Active** |
| **GET** | `/api/users/admin-profile` | `PatientService.getAdminProfile()` <br> ➔ `ProfileComponent` | Loads profile of the logged-in Clinic Admin. | **Active** |
| **PUT** | `/api/users/admin-profile` | `PatientService.updateAdminProfile()` <br> ➔ `ProfileComponent` | Fired when a Clinic Admin updates their profile details. | **Active** |
| **GET** | `/api/patients/doctors` | `PatientService.getDoctors()` <br> ➔ `PatientDoctorsComponent` | Loads the list of doctors for patients to browse and consult. | **Active** |
| **GET** | `/api/patients/doctors/{id}` | `PatientService.getDoctorDetails()` <br> ➔ `PatientDoctorsComponent` | Loads specific doctor profile information and schedules. | **Active** |
| **GET** | `/api/patients/{id}` | `PatientService.getPatientProfile()` <br> ➔ `ProfileComponent` | Loads profile information of the logged-in Patient. | **Active** |
| **PUT** | `/api/patients/{id}` | `PatientService.updatePatientProfile()` <br> ➔ `ProfileComponent` | Fired when a Patient clicks **"Save Profile"** to update details. | **Active** |
| **POST** | `/api/family/add` | `FamilyService.addFamilyMember()` <br> ➔ `ProfileComponent` | Fired when clicking **"Add Member"** in the Family tab and entering details. | **Active** |
| **POST** | `/api/family/verify` | `FamilyService.verifyFamilyMember()` <br> ➔ `ProfileComponent` | Fired when entering the OTP to verify and activate the new family member. | **Active** |
| **GET** | `/api/family` | `FamilyService.getFamilyMembers()` <br> ➔ `ProfileComponent` / `BookComponent` | Loads all verified family members associated with the Patient's profile. | **Active** |

---

## 5. Notifications & Real-Time APIs (`NotificationsController` & SignalR)

| HTTP Method / Protocol | API Endpoint / Hub | Frontend Service & Caller Component | Trigger Action / Purpose | Status |
|---|---|---|---|---|
| **GET** | `/api/notifications` | `NotificationService.getNotifications()` <br> ➔ `HeaderComponent` | Fetches historical inbox notifications displayed in the top navbar bell dropdown. | **Active** |
| **POST** | `/api/notifications/mark-read` | `NotificationService.markAllAsRead()` <br> ➔ `HeaderComponent` | Fired when the Patient/Doctor clicks the notification bell to clear unread counts. | **Active** |
| **WS (SignalR)** | `/notificationHub` | `NotificationService.startConnection()` <br> ➔ `AppComponent` | Initiates persistent WebSocket websocket connections to handle real-time notifications and silent UI refreshing. | **Active** |
