import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { PatientService } from '../../../core/services/patient.service';
import { AppointmentService } from '../../../core/services/appointment.service';
import { NotificationService } from '../../../core/services/notification.service';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-patient-doctors',
  templateUrl: './patient-doctors.component.html',
  styleUrls: ['./patient-doctors.component.css']
})
export class PatientDoctorsComponent implements OnInit, OnDestroy {
  doctors: any[] = [];
  specializations: any[] = [];
  private signalrSub?: Subscription;

  // Search & Filter Parameters
  searchQuery = '';
  selectedSpecializationId = '';
  stateFilter = '';
  cityFilter = '';

  // Pagination parameters
  page = 1;
  size = 5;
  totalCount = 0;
  totalPages = 1;

  errorMessage = '';
  isDoctorsLoading = true;

  // Tracks which doctor card is expanded for biography
  expandedDoctorIds: { [id: string]: boolean } = {};

  constructor(
    private patientService: PatientService,
    private appointmentService: AppointmentService,
    private notificationService: NotificationService,
    private sanitizer: DomSanitizer,
    private router: Router
  ) { }

  toggleExpand(doctorId: string): void {
    this.expandedDoctorIds[doctorId] = !this.expandedDoctorIds[doctorId];
  }

  isExpanded(doctorId: string): boolean {
    return !!this.expandedDoctorIds[doctorId];
  }

  ngOnInit(): void {
    this.loadSpecializations();
    this.loadDoctors();

    // Auto-reload the doctors list in real-time when notifications or refresh signals are received
    this.signalrSub = this.notificationService.refreshData$.subscribe({
      next: (area) => {
        if (area === 'Doctors') {
          this.loadDoctors();
        }
        if (area === 'Clinics') {
          this.loadDoctors();
          // Real-time update: If doctor modal popup is open, reload their clinics/timings listing
          if (this.selectedDoctorForModal) {
            this.openDoctorModal(this.selectedDoctorForModal);
          }
        }
      }
    });
  }

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }

  loadSpecializations(): void {
    this.appointmentService.getSpecializations().subscribe({
      next: (res) => {
        this.specializations = res;
      },
      error: () => {
        this.errorMessage = 'Failed to load doctor specializations.';
      }
    });
  }

  loadDoctors(): void {
    this.errorMessage = '';
    const params: any = {
      page: this.page,
      size: this.size
    };

    if (this.searchQuery.trim()) {
      params.search = this.searchQuery.trim();
    }
    if (this.selectedSpecializationId) {
      params.specializationId = this.selectedSpecializationId;
    }
    if (this.stateFilter.trim()) {
      params.state = this.stateFilter.trim();
    }
    if (this.cityFilter.trim()) {
      params.city = this.cityFilter.trim();
    }

    this.isDoctorsLoading = true;
    this.patientService.getDoctorsDirectory(params).subscribe({
      next: (res) => {
        this.doctors = res.items;
        this.totalCount = res.totalCount;
        this.totalPages = Math.ceil(this.totalCount / this.size);
        this.isDoctorsLoading = false;
      },
      error: () => {
        this.errorMessage = 'Failed to load doctors directory.';
        this.isDoctorsLoading = false;
      }
    });
  }

  onFilterChange(): void {
    this.page = 1;
    this.loadDoctors();
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.selectedSpecializationId = '';
    this.stateFilter = '';
    this.cityFilter = '';
    this.page = 1;
    this.loadDoctors();
  }

  onPageChange(newPage: number): void {
    if (newPage >= 1 && newPage <= this.totalPages) {
      this.page = newPage;
      this.loadDoctors();
    }
  }

  getSpecialtySvg(specName: string): SafeHtml {
    if (!specName) return this.sanitizer.bypassSecurityTrustHtml(this.getDefaultStethoscopeSvg());
    const name = specName.toLowerCase();
    let rawSvg = '';
    if (name.includes('cardio')) rawSvg = this.getHeartPulseSvg();
    else if (name.includes('derm')) rawSvg = this.getSkinSvg();
    else if (name.includes('dent') || name.includes('orthodont') || name.includes('periodont') || name.includes('endodont') || name.includes('oral')) rawSvg = this.getToothSvg();
    else if (name.includes('physio') || name.includes('rehab')) rawSvg = this.getPhysioSvg();
    else if (name.includes('neuro') || name.includes('psychiat') || name.includes('psychol')) rawSvg = this.getBrainSvg();
    else if (name.includes('ophthal') || name.includes('eye')) rawSvg = this.getEyeSvg();
    else if (name.includes('ent') || name.includes('audio')) rawSvg = this.getEarSvg();
    else if (name.includes('pulmono') || name.includes('lung')) rawSvg = this.getLungsSvg();
    else if (name.includes('diet') || name.includes('nutri')) rawSvg = this.getLeafSvg();
    else if (name.includes('pediat') || name.includes('neonat')) rawSvg = this.getChildSvg();
    else if (name.includes('gyneco') || name.includes('obstet')) rawSvg = this.getPregnantSvg();
    else if (name.includes('ortho') || name.includes('rheum') || name.includes('chiro')) rawSvg = this.getBoneSvg();
    else if (name.includes('gastro')) rawSvg = this.getGastroSvg();
    else if (name.includes('nephro')) rawSvg = this.getKidneySvg();
    else if (name.includes('endo')) rawSvg = this.getEndoSvg();
    else if (name.includes('oncol') || name.includes('cancer')) rawSvg = this.getOncologySvg();
    else if (name.includes('urol')) rawSvg = this.getUrologySvg();
    else if (name.includes('plastic')) rawSvg = this.getPlasticSvg();
    else if (name.includes('hemato')) rawSvg = this.getHematologySvg();
    else if (name.includes('radio')) rawSvg = this.getRadiologySvg();
    else if (name.includes('patho')) rawSvg = this.getPathologySvg();
    else if (name.includes('geriat')) rawSvg = this.getGeriatricSvg();
    else if (name.includes('sports')) rawSvg = this.getSportsSvg();
    else if (name.includes('podiat')) rawSvg = this.getFootSvg();
    else if (name.includes('speech')) rawSvg = this.getSpeechSvg();
    else if (name.includes('allerg') || name.includes('immun')) rawSvg = this.getAllergySvg();
    else if (name.includes('anest')) rawSvg = this.getAnesthesiaSvg();
    else if (name.includes('infect')) rawSvg = this.getInfectiousSvg();
    else if (name.includes('occup')) rawSvg = this.getOccupationalSvg();
    else if (name.includes('pain')) rawSvg = this.getPainSvg();
    else if (name.includes('homeo') || name.includes('ayurv')) rawSvg = this.getHerbalSvg();
    else rawSvg = this.getDefaultStethoscopeSvg();

    return this.sanitizer.bypassSecurityTrustHtml(rawSvg);
  }

  private svgWrap(paths: string): string {
    return `<svg xmlns="http://www.w3.org/2000/svg" width="35" height="35" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" class="svg-spec-icon">${paths}</svg>`;
  }

  private getHeartPulseSvg(): string {
    return this.svgWrap(`<path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z"/><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/>`);
  }

  private getSkinSvg(): string {
    return this.svgWrap(`<path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"/><circle cx="12" cy="11" r="1"/><circle cx="9" cy="14" r="1"/><circle cx="15" cy="14" r="1"/>`);
  }

  private getToothSvg(): string {
    return this.svgWrap(`<path d="M12 2C9 2 7 3.5 7 6.5c0 2 1.5 3 2.5 4.5C8 12.5 6 15 6 18c0 2 1.5 3.5 3 3.5 1 0 1.5-.5 2-1 .5.5 1 1 2 1 1.5 0 3-1.5 3-3.5 0-3-2-5.5-3.5-7C15.5 9.5 17 8.5 17 6.5 17 3.5 15 2 12 2z"/>`);
  }

  private getPhysioSvg(): string {
    return this.svgWrap(`<circle cx="12" cy="4" r="2"/><path d="M7 22l2-8 3 3 3-3 2 8"/><path d="M7 10c0 0 1 2 5 2s5-2 5-2"/><line x1="9" y1="10" x2="8" y2="14"/><line x1="15" y1="10" x2="16" y2="14"/>`);
  }

  private getBrainSvg(): string {
    return this.svgWrap(`<path d="M9.5 2A2.5 2.5 0 0 1 12 4.5v15a2.5 2.5 0 0 1-4.96-.44 2.5 2.5 0 0 1 0-3.12 3 3 0 0 1 0-4.88 2.5 2.5 0 0 1 0-3.12A2.5 2.5 0 0 1 9.5 2z"/><path d="M14.5 2A2.5 2.5 0 0 0 12 4.5v15a2.5 2.5 0 0 0 4.96-.44 2.5 2.5 0 0 0 0-3.12 3 3 0 0 0 0-4.88 2.5 2.5 0 0 0 0-3.12A2.5 2.5 0 0 0 14.5 2z"/>`);
  }

  private getEyeSvg(): string {
    return this.svgWrap(`<path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7z"/><circle cx="12" cy="12" r="3"/><circle cx="12" cy="12" r="1" fill="currentColor" stroke="none"/>`);
  }

  private getEarSvg(): string {
    return this.svgWrap(`<path d="M6 8.5a6.5 6.5 0 1 1 13 0c0 3-1.5 5.5-3.5 7.5l-2.5 2.5H10v-3a3 3 0 0 1 3-3"/><path d="M10 18.5a2.5 2.5 0 1 1-5 0"/>`);
  }

  private getLungsSvg(): string {
    return this.svgWrap(`<path d="M12 2v7"/><path d="M12 9a4 4 0 0 1 4 4v4a2 2 0 0 1-2 2h-1a1 1 0 0 1-1-1v-4a1 1 0 0 0-1-1h-1a1 1 0 0 0-1 1v4a1 1 0 0 1-1 1H8a2 2 0 0 1-2-2v-4a4 4 0 0 1 4-4Z"/>`);
  }

  private getLeafSvg(): string {
    return this.svgWrap(`<path d="M11 20A7 7 0 0 1 9.8 6.1C15.5 5 17 4.48 19 2c1 2.52 0 5-.5 7.5A7 7 0 0 1 11 20Z"/><path d="M19 2c-2.26 4.33-5.27 7.14-8 9"/>`);
  }

  private getChildSvg(): string {
    return this.svgWrap(`<circle cx="12" cy="5" r="3"/><path d="M6.5 20h11"/><path d="M8 20V12a4 4 0 0 1 8 0v8"/><path d="M9 16h6"/>`);
  }

  private getPregnantSvg(): string {
    return this.svgWrap(`<circle cx="12" cy="4" r="2.5"/><path d="M9 8.5c0 0-3 1.5-3 5.5v2a2 2 0 0 0 2 2h1l1 3h4l1-3h1a2 2 0 0 0 2-2v-2c0-4-3-5.5-3-5.5"/><circle cx="12" cy="13" r="1.5"/>`);
  }

  private getBoneSvg(): string {
    return this.svgWrap(`<path d="M18 3a3 3 0 0 0-3 3v12a3 3 0 0 0 3 3 3 3 0 0 0 3-3 3 3 0 0 0-3-3H6a3 3 0 0 0-3 3 3 3 0 0 0 3 3 3 3 0 0 0 3-3V6a3 3 0 0 0-3-3 3 3 0 0 0-3 3 3 3 0 0 0 3 3h12a3 3 0 0 0 3-3 3 3 0 0 0-3-3z"/>`);
  }

  private getGastroSvg(): string {
    return this.svgWrap(`<path d="M8 2a4 4 0 0 0-4 4c0 3 2 5 3 6 .5.5.5 1 0 2a3 3 0 0 0 3 4c1 0 2-.5 2.5-1.5"/><path d="M16 2a4 4 0 0 1 4 4c0 3-2 5-3 6-.5.5-.5 1 0 2a3 3 0 0 1-3 4c-1 0-2-.5-2.5-1.5"/><line x1="12" y1="7" x2="12" y2="13"/>`);
  }

  private getKidneySvg(): string {
    return this.svgWrap(`<path d="M12 2C9.5 2 7 4 7 7.5c0 4 2.5 6.5 5 9 2.5-2.5 5-5 5-9C17 4 14.5 2 12 2z"/><path d="M9 7c0 2 1 4 3 4s3-2 3-4"/>`);
  }

  private getEndoSvg(): string {
    return this.svgWrap(`<circle cx="12" cy="12" r="3"/><line x1="12" y1="2" x2="12" y2="6"/><line x1="12" y1="18" x2="12" y2="22"/><line x1="2" y1="12" x2="6" y2="12"/><line x1="18" y1="12" x2="22" y2="12"/><line x1="4.93" y1="4.93" x2="7.76" y2="7.76"/><line x1="16.24" y1="16.24" x2="19.07" y2="19.07"/><line x1="4.93" y1="19.07" x2="7.76" y2="16.24"/><line x1="16.24" y1="7.76" x2="19.07" y2="4.93"/>`);
  }

  private getOncologySvg(): string {
    return this.svgWrap(`<circle cx="12" cy="12" r="4"/><path d="M12 2v3M12 19v3M4.22 4.22l2.12 2.12M17.66 17.66l2.12 2.12M2 12h3M19 12h3M4.22 19.78l2.12-2.12M17.66 6.34l2.12-2.12"/>`);
  }

  private getUrologySvg(): string {
    return this.svgWrap(`<path d="M10 3a7 7 0 0 0-7 7c0 4 2 7 5 8.5V21h8v-2.5c3-1.5 5-4.5 5-8.5a7 7 0 0 0-7-7z"/><path d="M9 10h6M12 10v6"/>`);
  }

  private getPlasticSvg(): string {
    return this.svgWrap(`<circle cx="12" cy="8" r="4"/><path d="M20 21a8 8 0 1 0-16 0"/><path d="M9 13.5c0 3 1.5 5.5 3 5.5s3-2.5 3-5.5"/>`);
  }

  private getHematologySvg(): string {
    return this.svgWrap(`<path d="M12 2L8 8H4l4 4-2 6 6-4 6 4-2-6 4-4h-4z"/>`);
  }

  private getRadiologySvg(): string {
    return this.svgWrap(`<rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="12" cy="12" r="4"/><line x1="4" y1="4" x2="9" y2="9"/><line x1="20" y1="4" x2="15" y2="9"/><line x1="4" y1="20" x2="9" y2="15"/><line x1="20" y1="20" x2="15" y2="15"/>`);
  }

  private getPathologySvg(): string {
    return this.svgWrap(`<circle cx="12" cy="12" r="3"/><path d="M5 3a2 2 0 0 0-2 2"/><path d="M19 3a2 2 0 0 1 2 2"/><path d="M21 19a2 2 0 0 1-2 2"/><path d="M5 21a2 2 0 0 1-2-2"/><path d="M9 3h1"/><path d="M9 21h1"/><path d="M14 3h1"/><path d="M14 21h1"/><path d="M3 9v1"/><path d="M21 9v1"/><path d="M3 14v1"/><path d="M21 14v1"/>`);
  }

  private getGeriatricSvg(): string {
    return this.svgWrap(`<circle cx="12" cy="6" r="3"/><path d="M12 9c-4 0-6 2.5-6 5v1h12v-1c0-2.5-2-5-6-5z"/><path d="M8 21v-4"/><path d="M16 21v-4"/><path d="M6 17h12"/><line x1="12" y1="17" x2="12" y2="21"/>`);
  }

  private getSportsSvg(): string {
    return this.svgWrap(`<circle cx="12" cy="4" r="2"/><path d="M15 8l-3 4-3-4"/><path d="M9 12l-4 6h14l-4-6"/><path d="M7 22v-3l5-3 5 3v3"/>`);
  }

  private getFootSvg(): string {
    return this.svgWrap(`<path d="M8 3c0 0-2 2-2 6s1 7 3 8h6c2-1 3-4 3-8s-2-6-2-6"/><path d="M6 9h12"/><circle cx="9" cy="6" r="1"/><circle cx="12" cy="5" r="1"/><circle cx="15" cy="6" r="1"/>`);
  }

  private getSpeechSvg(): string {
    return this.svgWrap(`<path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/><line x1="9" y1="10" x2="15" y2="10"/><line x1="9" y1="14" x2="12" y2="14"/>`);
  }

  private getAllergySvg(): string {
    return this.svgWrap(`<path d="M12 22V12"/><path d="m5 12 7-10 7 10"/><path d="M5 12H2l3 5h14l3-5h-3"/>`);
  }

  private getAnesthesiaSvg(): string {
    return this.svgWrap(`<path d="M9 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-4"/><path d="M9 3v18"/><path d="M13 7l5-5 3 3-5 5"/><path d="m15 5 3 3"/>`);
  }

  private getInfectiousSvg(): string {
    return this.svgWrap(`<circle cx="12" cy="12" r="4"/><path d="M12 2v3M12 19v3M4.22 4.22l2.12 2.12M17.66 17.66l2.12 2.12M2 12h3M19 12h3M4.22 19.78l2.12-2.12M17.66 6.34l2.12-2.12"/><circle cx="12" cy="12" r="1.5" fill="currentColor" stroke="none"/>`);
  }

  private getOccupationalSvg(): string {
    return this.svgWrap(`<path d="M14.5 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7.5L14.5 2z"/><polyline points="14 2 14 8 20 8"/><path d="M9 15l2 2 4-4"/>`);
  }

  private getPainSvg(): string {
    return this.svgWrap(`<path d="M22 12h-4l-3 9L9 3l-3 9H2"/>`);
  }

  private getHerbalSvg(): string {
    return this.svgWrap(`<path d="M11 20A7 7 0 0 1 9.8 6.1C15.5 5 17 4.48 19 2c1 2.52 0 5-.5 7.5A7 7 0 0 1 11 20Z"/><path d="M19 2c-2.26 4.33-5.27 7.14-8 9"/><circle cx="8" cy="16" r="1.5" fill="currentColor" stroke="none"/>`);
  }

  private getDefaultStethoscopeSvg(): string {
    return this.svgWrap(`<path d="M4.5 16.5C3.67 16.5 3 15.83 3 15V9c0-3.87 3.13-7 7-7s7 3.13 7 7v6c0 .83-.67 1.5-1.5 1.5h-11Z"/><path d="M12 2v10a3 3 0 0 0 3 3"/><path d="M12 12a3 3 0 0 1-3 3"/><circle cx="19" cy="14.5" r="2.5"/>`);
  }

  getSpecialtyClass(specName: string): string {
    if (!specName) return 'spec-default';
    const name = specName.toLowerCase();
    if (name.includes('cardio')) return 'spec-cardio';
    if (name.includes('dermato')) return 'spec-dermato';
    if (name.includes('pediat')) return 'spec-pediat';
    if (name.includes('gyneco') || name.includes('obstet')) return 'spec-gyneco';
    if (name.includes('ortho') || name.includes('bone') || name.includes('chiro') || name.includes('rheuma')) return 'spec-ortho';
    if (name.includes('neuro') || name.includes('psychiat') || name.includes('psychol')) return 'spec-neuro';
    if (name.includes('ophthal') || name.includes('eye')) return 'spec-ophthal';
    if (name.includes('ent') || name.includes('audio')) return 'spec-ent';
    if (name.includes('pulmono') || name.includes('lung')) return 'spec-pulmono';
    if (name.includes('dent') || name.includes('orthodont') || name.includes('periodont') || name.includes('endodont')) return 'spec-dent';
    if (name.includes('physio') || name.includes('rehab')) return 'spec-physio';
    if (name.includes('diet') || name.includes('nutri') || name.includes('food')) return 'spec-diet';
    if (name.includes('podiat') || name.includes('foot')) return 'spec-podiat';
    if (name.includes('speech')) return 'spec-speech';
    if (name.includes('oncolog') || name.includes('cancer')) return 'spec-onco';
    if (name.includes('allergy') || name.includes('immun')) return 'spec-allergy';
    if (name.includes('homeo') || name.includes('ayurv') || name.includes('herb')) return 'spec-herbal';
    return 'spec-default';
  }

  selectedDoctorForModal: any = null;
  modalClinics: any[] = [];
  modalClinicsLoading = false;
  aboutExpanded = false;

  selectedDoctorForBooking: any = null;
  bookingClinics: any[] = [];
  bookingClinicsLoading = false;

  weekDays = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

  getSortedDays(openDaysStr: string): string[] {
    if (!openDaysStr) return [];
    const days = openDaysStr.split(',').map(d => d.trim());
    return days.sort((a, b) => this.weekDays.indexOf(a) - this.weekDays.indexOf(b));
  }

  formatClinicTimings(startTime: string, endTime: string): string {
    if (!startTime || !endTime) return '';
    const starts = startTime.split(',').map(t => t.trim());
    const ends = endTime.split(',').map(t => t.trim());
    const shifts: string[] = [];
    for (let i = 0; i < starts.length; i++) {
      if (starts[i] && ends[i]) {
        shifts.push(`${starts[i]} - ${ends[i]}`);
      }
    }
    return shifts.join(' & ');
  }

  isClinicCurrentlyOpen(clinic: any): boolean {
    if (!clinic || clinic.isAvailable === false) {
      return false;
    }

    if (!clinic.openDays) return false;
    const days = clinic.openDays.split(',').map((d: string) => d.trim().toLowerCase());
    const todayName = new Date().toLocaleDateString('en-US', { weekday: 'long' }).toLowerCase();
    if (!days.includes(todayName)) {
      return false;
    }

    if (!clinic.startTime || !clinic.endTime) return false;
    const starts = clinic.startTime.split(',').map((t: string) => t.trim());
    const ends = clinic.endTime.split(',').map((t: string) => t.trim());

    const parseTimeToMinutes = (timeStr: string): number => {
      if (!timeStr) return 0;
      const ampmMatch = timeStr.match(/(\d+):(\d+)\s*(AM|PM)/i);
      if (ampmMatch) {
        let hours = parseInt(ampmMatch[1], 10);
        const minutes = parseInt(ampmMatch[2], 10);
        const ampm = ampmMatch[3].toUpperCase();
        if (ampm === 'PM' && hours < 12) hours += 12;
        if (ampm === 'AM' && hours === 12) hours = 0;
        return hours * 60 + minutes;
      }
      const parts = timeStr.split(':');
      if (parts.length >= 2) {
        const hours = parseInt(parts[0], 10);
        const minutes = parseInt(parts[1], 10);
        return hours * 60 + minutes;
      }
      return 0;
    };

    const now = new Date();
    const currentMinutes = now.getHours() * 60 + now.getMinutes();

    for (let i = 0; i < starts.length; i++) {
      if (!starts[i] || !ends[i]) continue;
      const startMin = parseTimeToMinutes(starts[i]);
      const endMin = parseTimeToMinutes(ends[i]);
      
      if (startMin <= endMin) {
        if (currentMinutes >= startMin && currentMinutes <= endMin) {
          return true;
        }
      } else {
        if (currentMinutes >= startMin || currentMinutes <= endMin) {
          return true;
        }
      }
    }

    return false;
  }

  /**
   * Returns true if the clinic has at least one bookable date remaining:
   * - Not manually closed (isAvailable !== false)
   * - Has openDays configured
   * - Booking window end date (if set) has not passed
   * - At least one day within the effective booking window falls on an open day
   */
  isClinicBookable(clinic: any): boolean {
    if (!clinic || clinic.isAvailable === false) return false;
    if (!clinic.openDays || clinic.openDays.trim() === '') return false;

    const openDayNames = clinic.openDays.split(',').map((d: string) => d.trim().toLowerCase());
    const weekDays = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday'];

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    // Effective range start = max(today, bookingWindowStart)
    let rangeStart = new Date(today);
    if (clinic.bookingWindowStartDate) {
      const winStart = new Date(clinic.bookingWindowStartDate);
      winStart.setHours(0, 0, 0, 0);
      if (winStart > rangeStart) rangeStart = winStart;
    }

    // Effective range end = bookingWindowEnd (if set), else open-ended (search 180 days)
    let rangeEnd: Date | null = null;
    if (clinic.bookingWindowEndDate) {
      rangeEnd = new Date(clinic.bookingWindowEndDate);
      rangeEnd.setHours(23, 59, 59, 999);
      // Booking window already expired
      if (rangeEnd < today) return false;
    }

    // If rangeStart > rangeEnd → impossible window
    if (rangeEnd && rangeStart > rangeEnd) return false;

    // Scan up to 7 days from rangeStart (a full week covers all day-of-week possibilities)
    const scanLimit = rangeEnd
      ? Math.min(7, Math.ceil((rangeEnd.getTime() - rangeStart.getTime()) / 86400000) + 1)
      : 7;

    for (let i = 0; i < scanLimit; i++) {
      const d = new Date(rangeStart);
      d.setDate(d.getDate() + i);
      if (rangeEnd && d > rangeEnd) break;
      if (openDayNames.includes(weekDays[d.getDay()])) return true;
    }

    return false;
  }

  /** Human-readable reason why a clinic is not bookable */
  getClinicNotBookableReason(clinic: any): string {
    if (!clinic) return 'Clinic unavailable.';
    if (clinic.isAvailable === false) return clinic.unavailabilityReason || 'This branch is temporarily closed.';
    if (!clinic.openDays || clinic.openDays.trim() === '') return 'No schedule has been configured for this branch yet.';

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    if (clinic.bookingWindowEndDate) {
      const rangeEnd = new Date(clinic.bookingWindowEndDate);
      rangeEnd.setHours(23, 59, 59, 999);
      if (rangeEnd < today) return `Booking window closed on ${new Date(clinic.bookingWindowEndDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })}.`;
    }

    return 'No remaining bookable dates within the current booking window.';
  }

  toggleAboutExpand(): void {
    this.aboutExpanded = !this.aboutExpanded;
  }

  openDoctorModal(doctor: any): void {
    this.selectedDoctorForModal = doctor;
    this.modalClinics = [];
    this.modalClinicsLoading = true;
    this.aboutExpanded = false; // Collapsed by default

    this.patientService.getClinicsByDoctorId(doctor.doctorId).subscribe({
      next: (res) => {
        this.modalClinics = res;
        this.modalClinicsLoading = false;
      },
      error: () => {
        this.modalClinicsLoading = false;
      }
    });
  }

  closeDoctorModal(): void {
    this.selectedDoctorForModal = null;
    this.modalClinics = [];
    this.aboutExpanded = false;
  }

  onBookAppointmentClick(doctor: any): void {
    this.selectedDoctorForBooking = doctor;
    this.bookingClinics = [];
    this.bookingClinicsLoading = true;

    this.patientService.getClinicsByDoctorId(doctor.doctorId).subscribe({
      next: (res) => {
        this.bookingClinics = res;
        this.bookingClinicsLoading = false;

        // If doctor only has 1 clinic, navigate immediately to prevent any modal fatigue
        if (res.length === 1) {
          const singleClinic = res[0];
          this.router.navigate(['/patient/book-appointment'], {
            queryParams: { doctorId: doctor.doctorId, clinicId: singleClinic.clinicId }
          });
          this.selectedDoctorForBooking = null; // reset
        } else if (res.length === 0) {
          // If no clinics registered yet, navigate to book screen with doctorId only
          this.router.navigate(['/patient/book-appointment'], {
            queryParams: { doctorId: doctor.doctorId }
          });
          this.selectedDoctorForBooking = null; // reset
        }
      },
      error: () => {
        this.bookingClinicsLoading = false;
        // Default fallback to direct booking screen
        this.router.navigate(['/patient/book-appointment'], {
          queryParams: { doctorId: doctor.doctorId }
        });
        this.selectedDoctorForBooking = null; // reset
      }
    });
  }

  closeBookingModal(): void {
    this.selectedDoctorForBooking = null;
    this.bookingClinics = [];
  }
}
