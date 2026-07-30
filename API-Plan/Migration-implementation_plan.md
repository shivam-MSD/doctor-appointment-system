# Implementation Plan: Creating DTOs & Migration Guide

This plan outlines the design and files for creating Data Transfer Objects (DTOs) under the `Application/DTOs` folder, and details step-by-step how to configure and execute EF Core migrations.

## Proposed Changes

### Application Layer (DTOs)
We will create structured DTOs grouped by feature folder inside `Application/DTOs` to handle client requests and responses safely.

#### [NEW] [LoginDto.cs & RegisterDto.cs](file:///d:/shivam/doctor-appointment-system/DoctorAppointmentSystem/DoctorAppointmentSystem/Application/DTOs/AuthDTOs.cs)
- `LoginDto`: For user login (Email, Password).
- `RegisterDto`: For user registration (Email, Password, FirstName, LastName, MobileNo, Role).
- `AuthResponseDto`: Returns JWT Token, Refresh Token, and basic user info upon successful authentication.

#### [NEW] [UserDto.cs](file:///d:/shivam/doctor-appointment-system/DoctorAppointmentSystem/DoctorAppointmentSystem/Application/DTOs/UserDto.cs)
- `UserDto`: Basic user details returned to client (UserId, Email, IsActive).

#### [NEW] [DoctorDTOs.cs](file:///d:/shivam/doctor-appointment-system/DoctorAppointmentSystem/DoctorAppointmentSystem/Application/DTOs/DoctorDTOs.cs)
- `DoctorDto`: Detailed doctor profile including specialization name and qualification.
- `DoctorRegisterDto`: Input DTO when registering as a Doctor.
- `DoctorUpdateDto`: Input DTO when a doctor updates their profile details.

#### [NEW] [PatientDTOs.cs](file:///d:/shivam/doctor-appointment-system/DoctorAppointmentSystem/DoctorAppointmentSystem/Application/DTOs/PatientDTOs.cs)
- `PatientDto`: Detailed patient profile with user account details.
- `PatientRegisterDto`: Input DTO when registering as a Patient.

#### [NEW] [AppointmentDTOs.cs](file:///d:/shivam/doctor-appointment-system/DoctorAppointmentSystem/DoctorAppointmentSystem/Application/DTOs/AppointmentDTOs.cs)
- `CreateAppointmentDto`: Inputs for booking a slot (DoctorId, PatientId, AppointmentDate, StartTime, EndTime, Reason, ConsultationType).
- `AppointmentDto`: Detailed representation of an appointment returned to the client (includes Doctor/Patient basic info and status).
- `UpdateAppointmentStatusDto`: DTO for approving, cancelling, or completing an appointment.

#### [NEW] [DoctorScheduleDTOs.cs](file:///d:/shivam/doctor-appointment-system/DoctorAppointmentSystem/DoctorAppointmentSystem/Application/DTOs/DoctorScheduleDTOs.cs)
- `CreateScheduleDto`: For creating weekly schedule slots.
- `DoctorScheduleDto`: Output representation of a schedule slot.

#### [NEW] [SpecializationDto.cs](file:///d:/shivam/doctor-appointment-system/DoctorAppointmentSystem/DoctorAppointmentSystem/Application/DTOs/SpecializationDto.cs)
- `SpecializationDto`: Represents specializations like Cardiologist, Dentist, etc.

---

### Migration Implementation Guide

We will compile and present a comprehensive, step-by-step guide explaining how migrations are created, configured, and run on this project.

## Verification Plan
We will verify that:
1. DTO classes compile successfully without syntax errors.
2. The project compiles.
