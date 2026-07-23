export interface Appointment {
  appointmentId: string;
  patientId: string;
  patientName: string;
  patientAge: number;
  patientGender: string;
  doctorId: string;
  doctorName: string;
  doctorSpecialization: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  status: 'Pending' | 'Confirmed' | 'Cancelled' | 'Completed' | 'Rejected' | 'RescheduleProposed' | 'FollowUpProposed';
  reason: string;
  consultationType: EConsultationType | string;
  clinicId?: string;
  clinicName?: string;
  comment?: string;
  report?: string;
  rejectionReason?: string;
  queueNumber?: number;
  doctorAssignedTime?: string;
  rescheduleProposedDate?: string;
  rescheduleProposedTime?: string;
  rescheduleReason?: string;
  confirmedDate?: string;
  rescheduleProposedAt?: string;
  cancelledDate?: string;
  cancelledBy?: string;
}

export enum EConsultationType {
  InPerson = 'InPerson',
  VideoConsultation = 'VideoConsultation'
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}
