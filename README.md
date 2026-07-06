# HealSync - Doctor Appointment & Clinic Management System

HealSync is a modern, decoupled healthcare management application built to streamline doctor registrations, clinic scheduling, administrative delegation, and patient appointment bookings across multiple centers.

---

## 💻 Tech Stack
* **Backend:** ASP.NET Core Web API (C# .NET 10), Entity Framework Core (Code-First), SQL Server (LocalDB / Express).
* **Frontend:** Angular, Reactive Forms & HTML5/Vanilla CSS (custom glassmorphic theme).
* **Communication:** RESTful JSON APIs, Mock JWT Authentication.

---

## 🔑 Dedicated Role Portals & Credentials

The web application simulates **four separate applications** with dedicated URLs. Access them at:

### 👤 Patient Application
* **Login URL:** [http://localhost:4200/patient/login](http://localhost:4200/patient/login)
* **Register URL:** [http://localhost:4200/patient/register](http://localhost:4200/patient/register)

### 🩺 Doctor Application
* **Login URL:** [http://localhost:4200/doctor/login](http://localhost:4200/doctor/login)
* **Register URL:** [http://localhost:4200/doctor/register](http://localhost:4200/doctor/register)

### 🏢 Clinic Admin Application
* **Login URL:** [http://localhost:4200/admin/login](http://localhost:4200/admin/login)
* *Note: Clinic Admins are registered directly from the Doctor's Portal for their specific clinic branches and approved by the Super Admin before login.*

### 👑 Super Admin Console
* **Login URL:** [http://localhost:4200/superadmin/login](http://localhost:4200/superadmin/login)

---

### Seeded Credentials (Developer Testing):
1. **Super Admin Account:**
   * **Email:** `superadmin@doctorapp.com`
   * **Password:** `SuperAdmin@123`

---

## 👥 Role Capabilities & Feature Breakdown

### 👑 1. Super Admin Portal
The Super Admin governs credential verification and activation gates across the platform.
* **Verify Doctors:** View details of newly registered practitioners, review clinical parameters (e.g. licence numbers), and verify status from `Pending` to `Verified`.
* **Verify Clinics:** Approve new physiotherapy clinics, centers, or hospitals registered by verified doctors.
* **Verify Clinic Admins:** Validate administrative accounts requesting access to manage specific clinic locations.
* **Dashboard Overview:** Displays pending verification queues in real-time.

### 🩺 2. Doctor Portal
Designed for practitioners practicing privately or across multiple clinical branches.
* **Profile Setup:** Complete license details, qualification lists, biography info (`AboutDoctor`), and average consultation fees.
* **Clinic Registration:** Request activation of multiple clinic branches, centers, or hospitals under their name (once the Doctor's profile is verified).
* **Admin Delegation:** Add/assign Clinic Admins to their verified clinic branches.
* **Conflict-Free Schedule:** View unified schedules across all clinical locations. Appointments are verified globally against `DoctorId` to prevent time-slot overlaps and double bookings.

### 🏢 3. Clinic Admin Portal
Maintains center-specific queues and handles patient schedules.
* **Clinic Focus:** Filtered dashboard viewing only appointments booked for their assigned clinic location.
* **Schedule Control:** Edit schedules, check patient check-in statuses, and cancel or postpone appointments.
* **Profile Management:** Manage contact credentials and details.

### 👤 4. Patient Portal
Allows patients to discover practitioners and manage family bookings seamlessly.
* **Dynamic Search & Filtering (Mandatory):** Patients can search and filter verified doctors by **State**, **City**, and **Specialization** (e.g., Dentist, ENT, General Physician). Optional custom keyword filters (name or address details) are supported.
* **Profile Completion Tracker (INDmoney-style):**
  * Displays circular progress indicators in the sidebar and header.
  * Details the exact missing properties (Gender, Blood group, Emergency contact, DOB) to reach `100%` completion.
  * Hides the setup guide card once completion status reaches 100%.
  * Offers unified **Save Settings** and **Discard Changes** actions.
* **Family Members Management:** Add and manage family profiles under a single master account (supporting relationship definitions: Spouse, Parent, Child, etc.).
* **Slot Bookings:** Select active clinic branches for verified doctors and secure time-slots without schedule conflicts.

---

## 🛠️ Project Setup & Installation

### 1. Database Setup
1. Configure your database connection string in [appsettings.json](file:///d:/shivam/doctor-appointment-system/DoctorAppointmentSystem/DoctorAppointmentSystem/appsettings.json):
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=DoctorAppointmentDb;User Id=your_user;Password=your_password;TrustServerCertificate=True"
   }
   ```
2. Recreate databases or run migrations using dotnet tools:
   ```bash
   dotnet ef database update
   ```

### 2. Start the Backend API
```bash
cd DoctorAppointmentSystem/DoctorAppointmentSystem
dotnet run
```
The server will boot and begin listening on `http://localhost:5222`. Seed data will populate automatically.

### 3. Start the Frontend App
```bash
cd doctor-appointment-frontend
npm install
npm run start
```
Open your browser and navigate to `http://localhost:4200` to start using HealSync!
