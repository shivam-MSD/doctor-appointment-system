# 🚀 Future Enhancements Backlog (HealSync Roadmap)

This document tracks deferred features and backend integrations to be implemented in future phases.

---

## 💳 1. Payment Gateway & Financial Enhancements
- **Integrated Online Payment Gateway**: Razorpay / PayU SDK integration with "Pay Online Now" vs. "Pay at Clinic" toggle.
- **Detailed Fee Breakdown**: Breakdown calculation (Consultation Fee + Platform Convenience Fee + GST/Tax).
- **Payment Transaction Receipts**: Automated PDF invoice/receipt generation upon successful digital payment.

---

## ⭐ 2. Doctor Ratings & Patient Reviews System
- **Post-Consultation Rating Prompt**: Interactive star rating (1 to 5 stars) + optional text review prompt triggered after appointment status becomes `Completed`.
- **Doctor Profile Review Analytics**: Aggregate star ratings, total review count, and verified patient reviews rendered on Doctor Search cards and profile modals.

---

## 📁 3. Medical Attachments & Prescription System
- **Patient Lab Report / Prescription Upload**: Multi-file attachment (PDF/images) during appointment booking to share past medical records with the practitioner.
- **Digital Prescription & Report Download**: In-row "View Prescription" and "Download Lab Summary" buttons for completed consultation records.
- **Calendar (.ics) Sync**: Automated "Add to Google/Outlook Calendar" download link on booking confirmation screens.

---

## 📊 4. Advanced Analytics & Cancellation Feedback
- **Cancellation Reason Dropdown**: Structured cancellation reason categories (e.g. "Doctor Unavailable", "Schedule Conflict", "Fees Issue") for patient & clinic analytics.

---

## 🆔 5. India Digital Health Stack (ABDM) & Security
- **ABHA ID (Ayushman Bharat Health Account) Linking**: Integration with ABDM M1/M2 APIs for digital health card verification.
- **Family Dependents Schema**: Multi-profile management allowing primary accounts to manage appointments for parents/children.
- **Medical History & Document Vault API**: Encryption and Cloud Storage (S3/Azure Blob) for patient prescription and lab report vault.
- **Two-Factor Authentication (2FA)**: SMS/WhatsApp OTP verification gate for sensitive profile changes.

---

## ⚙️ 6. Advanced Settings & Compliance Engine
- **DPDPA Data Export & Account Deletion**: Self-service "Download My Health Data" and account deletion API with data retention policies.
- **Doctor Auto-Approval & Leave Mode Engine**: Automated rule engine for doctor leave schedules, auto-accepting booking requests, and clinic default fee overrides.
- **Multi-Language Localization Engine**: Translation backend for Hindi, Gujarati, Marathi, and Tamil.

---

## 📈 7. Doctor Financial Analytics & Digital Rx PDF Generator
- **Doctor Weekly/Monthly Revenue Charts**: Financial earnings breakdown charts tracking consultation income per clinic.
- **Structured Digital Prescription PDF Engine**: Rx generator with medicine name, dosage, frequency, and duration inputs auto-rendering official PDF reports.

---

## 🏥 8. Advanced Patient Tagging & Multi-Clinic Analytics
- **Patient Tagging & Categorization System**: Custom labels ("Diabetic", "Hypertension", "VIP", "Follow-up Needed") attached to patient profiles for doctor organization.
- **Clinic Photos Cloud Storage**: Multi-image upload for clinic storefront & facility preview.
- **Clinic Performance Stats**: Comparative analytics (Appointments per clinic, Patient volume) for doctors practicing across multiple locations.

---

## 🔒 9. Doctor NMC Verification, Payouts & Admin Access Control
- **RBI-Compliant Bank Account & Payout Details**: Masked bank account (IFSC, Account Number) section for doctor payout disbursements.
- **Granular Clinic Admin Access Control**: Permission matrix selector (e.g. "Appointments Only" vs "Clinic Details & Pricing").
- **Education, Awards & Publications Timeline**: Structured timeline schema for medical degrees, colleges, awards, and journal publications.

---

## 📡 10. Multi-Channel Push Infrastructure, PDF Export & PWA Offline Caching
- **Audit Logs PDF & Excel Export**: Client/Server PDF and Excel export engine for doctor tax compliance & auditing.
- **Multi-Channel Notification Gateway & Fallback**: Email failure detection with automatic SMS/WhatsApp failover engine.
- **Email Action Deep-Links & Digest Engine**: Interactive email templates with direct action buttons and daily summary digest options.
- **PWA Offline Service Worker & Session Timeout Warning Modal**: Offline service worker cache for last-viewed appointment lists and inactivity session timeout warning modal.
