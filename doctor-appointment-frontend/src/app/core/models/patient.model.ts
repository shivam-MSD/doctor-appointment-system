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
  country?: string;
  state?: string;
  city?: string;
  area?: string;
  pincode?: string;
  addressline1?: string;
  addressline2?: string;
}

export interface FamilyVerification {
  verificationId: string;
  message: string;
}
