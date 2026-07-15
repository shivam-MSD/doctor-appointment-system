export interface Appointment {
  appointmentId: string;
  patientId: string;
  patientName: string;
  doctorId: string;
  doctorName: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  status: 'Pending' | 'Confirmed' | 'Cancelled' | 'Completed' | 'Rejected' | 'RescheduleProposed';
  reason: string;
  consultationType: EConsultationType | string;
  clinicId?: string;
  clinicName?: string;
  comment?: string;
  report?: string;
  rejectionReason?: string;
  queueNumber?: number;
  doctorAssignedTime?: string;
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
