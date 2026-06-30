# Software Requirement Specification (SRS)

# Project Name
Doctor Appointment System

# Technology Stack

Backend:
- .NET 10
- C#
- ASP.NET Core Web API
- Entity Framework Core 10
- LINQ
- SignalR

Database:
- SQL Server / PostgreSQL

Authentication:
- ASP.NET Core Identity
- JWT Authentication
- Refresh Token

Frontend:
- Angular / React / Blazor

Cloud:
- Azure / AWS

Deployment:
- Docker
- Kubernetes

Architecture:
- Clean Architecture
- Domain Driven Design (DDD)

---

# 1. Introduction

## 1.1 Purpose

The purpose of this system is to provide an online platform where patients can:

- Search doctors
- View doctor profiles
- Check availability
- Book appointments
- Make payments
- Receive reminders
- Manage medical history

Doctors can:

- Manage schedules
- Accept/reject appointments
- Maintain patient records
- Generate prescriptions


---

# 2. Scope

The system will support three major users:

1. Patient

2. Doctor

3. Admin


---

# 3. User Roles


## 3.1 Patient

Responsibilities:

- Register account
- Login
- Search doctors
- View doctor details
- Book appointment
- Cancel appointment
- View appointment history
- Upload medical documents
- Pay consultation fees
- Provide reviews


---

## 3.2 Doctor

Responsibilities:

- Register doctor profile
- Upload certificates
- Manage availability
- Accept appointments
- Reject appointments
- View patient details
- Create prescriptions
- Maintain consultation history


---

## 3.3 Admin

Responsibilities:

- Manage users
- Verify doctors
- Manage specialties
- Monitor appointments
- Generate reports


---

# 4. Functional Requirements


# Authentication Module


## Features

User Registration

Fields:
First Name
Last Name
Email
Mobile Number
Password
Date Of Birth
Gender
Address

Login:


Email
Password


Security:

- Password hashing
- JWT token
- Refresh token
- Account lockout
- Email verification


---

# User Management


Entities:


## User


UserId
FirstName
LastName
Email
Mobile
PasswordHash
Role
CreatedDate
UpdatedDate
IsActive



---

# Doctor Management


Doctor Entity:



DoctorId

UserId

Specialization

Qualification

Experience

LicenseNumber

ConsultationFee

HospitalName

Address

Rating

ProfileImage

Status

CreatedDate



Doctor can manage:


- Profile
- Availability
- Fees
- Consultation type


---

# Doctor Specialization


Example:


Cardiologist

Dermatologist

Neurologist

Dentist

Orthopedic

Pediatrician



Table:


SpecializationId

Name

Description



---

# Appointment Management


Core module.


Patient can:


Search doctor:

Filters:


Specialization

Location

Experience

Rating

Availability

Fees



Book appointment:


Input:



DoctorId

PatientId

AppointmentDate

TimeSlot

Reason

ConsultationType



Appointment Status:



Pending

Confirmed

Completed

Cancelled

Rejected

NoShow



Appointment Entity:



AppointmentId

PatientId

DoctorId

AppointmentDate

StartTime

EndTime

Status

Reason

CreatedDate



---

# Schedule Management


Doctor availability:


Example:


Monday


09:00 AM - 01:00 PM

05:00 PM - 09:00 PM



Entity:



ScheduleId

DoctorId

Day

StartTime

EndTime

IsAvailable



---

# Prescription Management


Doctor creates prescription:


Details:



Medicine Name

Dosage

Frequency

Duration

Instructions



Prescription Entity:



PrescriptionId

AppointmentId

DoctorId

PatientId

CreatedDate



Prescription Medicine:



MedicineId

PrescriptionId

Name

Dosage

Frequency

Duration



---

# Medical Records


Patient history:


Store:


- Previous diseases
- Reports
- Documents
- Allergies


Entity:



MedicalRecordId

PatientId

Title

Description

FilePath

CreatedDate



---

# File Upload


Supported:


- PDF
- JPG
- PNG


Storage:

Option 1:

Local storage


Option 2:

Azure Blob Storage


---

# Payment Module


Integration:

Example:

- Razorpay
- Stripe


Payment Entity:



PaymentId

AppointmentId

Amount

TransactionId

PaymentStatus

PaymentDate



Payment Status:



Pending

Success

Failed

Refunded



---

# Notification System


Notifications:


Email:

- Appointment confirmation
- Cancellation
- Reminder


SMS:

- Appointment reminder


Push:

- Mobile notification


Implementation:


Background Worker:


IHostedService

Hangfire

Quartz.NET



---

# Review and Rating


Patient can review doctor:


Entity:



ReviewId

DoctorId

PatientId

Rating

Comment

CreatedDate



---

# Admin Dashboard


Features:


User statistics

Doctor statistics

Appointment statistics

Revenue reports


Charts:



Total Patients

Total Doctors

Daily Appointments

Monthly Revenue



---

# Non Functional Requirements


## Performance

System should support:

10000+ users


API response:

< 500ms


---

# Security


Implementation:


- JWT Authentication
- Role Based Authorization
- HTTPS
- SQL Injection prevention
- Input validation
- Password encryption
- Audit logs


---

# Logging


Use:


- Serilog


Store:



Exception

Request

Response

User Activity



---

# Audit Trail


Track:


Who changed what?


Example:



Admin approved doctor

Patient cancelled appointment

Doctor updated schedule



---

# Database Design


Main Tables:



Users

Roles

Doctors

Patients

Specializations

Appointments

Schedules

Prescriptions

PrescriptionMedicine

Payments

Reviews

MedicalRecords

Notifications

AuditLogs



---

# API Design


## Authentication


POST


/api/auth/register



POST


/api/auth/login



POST


/api/auth/refresh-token



---

# Doctor APIs


GET


/api/doctors



GET


/api/doctors/{id}



POST


/api/doctors/profile



PUT


/api/doctors/schedule



---

# Appointment APIs


POST


/api/appointments



GET


/api/appointments/my



PUT


/api/appointments/{id}/cancel



---

# Prescription APIs


POST



/api/prescription



GET



/api/prescription/{appointmentId}



---

# Architecture


Recommended:



DoctorAppointmentSystem

│

├── API

│

├── Application

│

├── Domain

│

├── Infrastructure

│

├── Persistence

│

└── Tests



---

# Domain Layer


Contains:


Entities

Value Objects

Domain Events


Example:



Appointment.cs

Doctor.cs

Patient.cs

Prescription.cs



---

# Application Layer


Contains:


Services

DTO

Interfaces

Validators


Example:



AppointmentService

DoctorService

PaymentService



---

# Infrastructure Layer


Contains:


External integrations:



Email

SMS

Payment Gateway

File Storage



---

# Background Jobs


Jobs:


Appointment reminder


Example:



Every 15 minutes

Check upcoming appointments

Send reminder



---

# Testing Strategy


Unit Testing:


Framework:

xUnit


Mocking:

Moq


Integration:

WebApplicationFactory


Coverage target:

80%


---

# Docker Support


Containers:



API Container

SQL Server Container

Redis Container

RabbitMQ Container



---

# Advanced Features


## Video Consultation


Integration:

- WebRTC


---

## Chat Between Doctor Patient


Using:

SignalR


---

## AI Features


Possible:


- Disease symptom checker
- Appointment recommendation
- Medical report summary


---

# Microservice Future Design


Services:



Auth Service

User Service

Doctor Service

Appointment Service

Payment Service

Notification Service



Communication:


RabbitMQ


---

# Deployment Architecture



Client

|

API Gateway

|

Microservices

|

Database

|

Cloud Storage



---

# Future Enhancements


- Mobile application
- Health insurance integration
- Lab test booking
- Pharmacy integration
- Online medicine ordering

---

# End of Document


For a strong .NET 10 portfolio, I would suggest implementing it in this order:

Clean Architecture + JWT Authentication
User/Role Management
Doctor Module
Appointment Booking
Schedule Management
Email Notification
Payment Integration
Docker
Redis Cache
RabbitMQ + Microservices

This project can realistically demonstrate the skills expected from a mid-level .NET backend developer (3–5 years experience).

i dont know mobile development but can i give this apis to mobile team for the integration or to use backend? or i have to prepare separate backend for mobile app?

Yes, you can absolutely give the same .NET 10 Web APIs to a mobile development team. You do not need to create a separate backend for mobile applications.

This is how real-world systems are usually designed.

Your architecture will look like this:

                 Mobile App
              (Android / iOS)
                    |
                    |
                    |
              REST API (HTTP)
                    |
                    |
          .NET 10 Backend API
                    |
        -------------------------
        |                       |
    SQL Database          External Services
                            |
                    Email / SMS / Payment

The mobile team consumes your APIs exactly like your web frontend would.

Example

You create:

Login API
POST /api/auth/login

Request:

{
    "email": "doctor@gmail.com",
    "password": "123456"
}

Response:

{
    "accessToken": "eyJhbGciOiJIUzI1...",
    "refreshToken": "abc123",
    "userId": 10,
    "role": "Doctor"
}

Mobile developer uses this:

Android:

Retrofit API call

iOS:

Flutter:

http.post()

They don't care whether your backend is .NET, Java, Node, etc.

Do you need a separate backend for Mobile?

No.

Avoid this:

Web Backend
      |
      |
Mobile Backend
      |
      |
Database

This creates unnecessary duplication:

Example:

Web Backend:

CreateAppointment()

Mobile Backend:

CreateAppointmentForMobile()

Both do the same thing.

Problems:

Duplicate code
More maintenance
More bugs
More servers
Real Company Architecture

Companies normally build:

              Client Applications

       Web              Mobile
        |                 |
        |                 |
        -------------------
                |
                |
          Backend APIs
                |
        ----------------
        |
    Business Logic
        |
    Database

One backend, many clients.

Examples:

Banking apps
Amazon
Uber
Healthcare apps
For your Doctor Appointment System

You can create:

Backend

.NET 10 Web API

DoctorAppointment.API

Responsibilities:

Authentication
Appointment booking
Doctor management
Payment
Notifications
Frontend options
Web Application

Angular / React

doctor-appointment-web

Uses:

https://api.yourdomain.com/api/doctors
Mobile Application

Someone else creates:

doctor-appointment-mobile

Uses:

https://api.yourdomain.com/api/doctors

Same API.

Important Things You Should Add for Mobile Support
1. API Versioning

Do not create:

/api/doctors

Instead:

/api/v1/doctors

Later:

/api/v2/doctors

Example:

GET

https://api.xyz.com/api/v1/doctors

Why?

Mobile apps stay installed for years.

Suppose:

Version 1 app is installed by 1 million users.

You release changes.

Old app should still work.

2. Pagination

Mobile internet can be slow.

Don't return:

100000 doctors

Instead:

Request:

GET /api/v1/doctors?page=1&pageSize=20

Response:

{
 "page":1,
 "pageSize":20,
 "totalRecords":5000,
 "data":[]
}
3. Mobile-friendly Response

Bad:

{
 "doctorName":"Dr Patel",
 "doctorAddress":"Ahmedabad Gujarat India..."
}

Better:

{
 "id":101,
 "name":"Dr Patel",
 "specialization":"Cardiologist",
 "experience":10,
 "rating":4.8,
 "imageUrl":"https://..."
}
4. File Upload Support

Mobile user uploads:

Medical reports
Prescription images

API:

POST

/api/v1/files/upload
5. Push Notification API

Example:

Doctor accepts appointment:

Backend:

Appointment Status = Confirmed

        |
        |
Send Notification

        |
        |
Mobile receives notification

Using:

Firebase Cloud Messaging (FCM)
Your Project Structure can be:
DoctorAppointmentSystem


src

 |
 |-- DoctorAppointment.API

 |
 |-- DoctorAppointment.Application

 |
 |-- DoctorAppointment.Domain

 |
 |-- DoctorAppointment.Infrastructure

 |
 |-- DoctorAppointment.Persistence


tests

 |
 |-- UnitTests

 |
 |-- IntegrationTests

If later you create Mobile App

You only add:

Android App

or

Flutter App

or

React Native App

No backend change.

For your learning path, I would build this as:

Phase 1

.NET 10 Web API
+
Clean Architecture
+
SQL Server
+
JWT

Phase 2

Angular Web App

Phase 3

Expose APIs to Mobile Team

Phase 4

Docker + Azure Deployment