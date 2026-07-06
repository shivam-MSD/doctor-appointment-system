export interface Patient {
  patientId: string;
  firstName: string;
  lastName: string;
  mobileNo: string;
  gender: string;
  dob: string;
  bloodGroup?: string;
  emergencyContactName: string;
  emergencyContactNumber: string;
}

export interface FamilyVerification {
  verificationId: string;
  message: string;
}
