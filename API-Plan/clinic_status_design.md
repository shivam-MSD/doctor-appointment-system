# Clinic Open/Close Status — Complete Design Analysis

## How It Works Today (Already Correct)

The `isClinicCurrentlyOpen()` function **already auto-calculates** open status. Nothing is hardcoded.
The logic is:

```
isOpen = TRUE  only if ALL of these pass:
  1. isAvailable !== false          ← not manually closed
  2. today's day is in openDays     ← right day of week
  3. current time is within startTime–endTime window  ← right time
```

So the doctor/admin **never needs to manually set "open"** — it happens automatically based on the schedule they configured.

---

## All Situations & Solutions

### 🟢 Situation 1 — Normal Operation (Auto-Open)
**What:** Today is Monday, clinic opens Mon/Wed/Fri 10:00–19:00, current time is 2:00 PM  
**Status:** 🟢 **Open** (auto-calculated)  
**Action needed:** None. Works automatically.

---

### 🔴 Situation 2 — Outside Working Hours (Same Day)
**What:** Today is Monday (a clinic day), but current time is 8:00 AM (before 10:00) or 9:00 PM (after 19:00)  
**Status:** 🔴 **Closed (Outside Working Hours)** (auto-calculated)  
**Action needed:** None. Works automatically.

---

### 🔴 Situation 3 — Non-Clinic Day
**What:** Today is Sunday but clinic only opens Mon/Wed/Fri  
**Status:** 🔴 **Closed (Outside Working Hours)** (auto-calculated)  
**Action needed:** None. Works automatically.

---

### 🔴 Situation 4 — Temporary Closure (Emergency / Holiday)
**What:** Clinic is usually open today but doctor/admin wants to close it for a specific reason  
> e.g., Doctor is sick, Public holiday, Power cut, Renovation, Family emergency  
**Status:** 🔴 **Closed (Temporarily Closed)** + reason shown to patient  
**Action:** Doctor sets `isAvailable = false` + writes `unavailabilityReason`  
**Timings should NOT be changed** — they resume automatically when `isAvailable` is toggled back to `true`  
**Who resets it?** Doctor/admin manually sets `isAvailable = true` again when back to normal.

> [!IMPORTANT]  
> This is the correct pattern. **Never change timings for a temp closure** — just toggle `isAvailable`.

---

### 🟡 Situation 5 — Doctor Personally Absent (Clinic Still Open)
**What:** The clinic building is open, staff/physiotherapists are present, but the primary doctor is not  
> e.g., Doctor at a conference, on leave, but assistants handle patients  
**Status:** 🟢 Open (auto) but shows ⚠️ "Doctor is Personally Unavailable" notice  
**Action:** Doctor sets `isDoctorAvailable = false` + `doctorUnavailabilityReason`  
**Patients can still book** (up to system/admin decision)

---

### 🔴 Situation 6 — Outside Booking Window
**What:** The clinic has a `bookingWindowStartDate` that hasn't arrived yet, OR `bookingWindowEndDate` has passed  
> e.g., New clinic branch opens next month — you can see it but can't book yet  
**Status:** 🟢 Schedule exists, but "Book at this Branch" button should be disabled/hidden  
**Current gap:** `isClinicCurrentlyOpen()` doesn't check the booking window — **this needs to be added**

---

### ⚪ Situation 7 — No Schedule Configured Yet
**What:** Doctor created the clinic record but hasn't set `openDays` or `startTime`/`endTime`  
**Status:** 🔴 Closed (shown as "Outside Working Hours" currently)  
**Better UX:** Show "Schedule not configured yet" instead of "Outside Working Hours"

---

### 🔴 Situation 8 — Permanent Closure / Branch Shutdown
**What:** Clinic branch at Location A is permanently shutting down  
**Action:** Admin/Doctor either deletes the clinic record, or sets `isAvailable = false` with reason "Permanently Closed"  
**Timings:** Can be cleared or left as-is, doesn't matter since `isAvailable = false`

---

## Summary — What "isAvailable" Flag Means

| `isAvailable` | Meaning |
|---|---|
| `true` (or not set) | Schedule is active — auto-open/closed based on days+time |
| `false` | **Manually overridden to closed** regardless of schedule — always shows Temporarily Closed |

**The schedule (days + timings) should NEVER be deleted just to close the clinic.** It's like deleting your work shift from a calendar just because you're taking a day off — you'd have to re-enter everything the next time.

---

## What Needs to Be Fixed/Improved

### ✅ Already Working
- Auto-open based on day of week
- Auto-open based on current time vs. startTime–endTime
- Manual override via `isAvailable = false`
- Doctor personal unavailability notice

### ❌ Missing: Booking Window Check in Status Display
The `isClinicCurrentlyOpen()` does not verify if today's date is within `bookingWindowStartDate` → `bookingWindowEndDate`.

**Fix needed in `isClinicCurrentlyOpen()`:**
```typescript
// After day/time checks pass, also verify booking window
const today = new Date();
today.setHours(0, 0, 0, 0);

if (clinic.bookingWindowStartDate) {
  const windowStart = new Date(clinic.bookingWindowStartDate);
  windowStart.setHours(0, 0, 0, 0);
  if (today < windowStart) return false;  // Booking window not started
}
if (clinic.bookingWindowEndDate) {
  const windowEnd = new Date(clinic.bookingWindowEndDate);
  windowEnd.setHours(23, 59, 59, 999);
  if (today > windowEnd) return false;  // Booking window expired
}
```

### ❌ Missing: Better Status Labels in the Badge
Currently there are only 2 possible closed labels:
- "Closed (Temporarily Closed)" — when `isAvailable === false`
- "Closed (Outside Working Hours)" — for everything else

Should be expanded to:
- 🔴 "Closed (Temporarily Closed)" — `isAvailable === false`
- 🔴 "Closed (Today is not a clinic day)" — correct day not in openDays
- 🔴 "Closed (Outside Working Hours)" — correct day but wrong time
- 🟡 "Booking Window Not Open Yet" — before bookingWindowStartDate
- 🔴 "Booking Window Expired" — after bookingWindowEndDate
- ⚪ "Schedule Not Configured" — no openDays or times set

---

## Recommended Implementation Plan

1. **Update `isClinicCurrentlyOpen()`** to also check booking window dates
2. **Add `getClinicStatusLabel(clinic)`** function that returns a descriptive label for the exact reason the clinic is closed
3. **Update the HTML badge** to use the new label function
4. **No backend changes needed** — all status logic is derived/calculated on the frontend

> [!NOTE]  
> The backend field `isAvailable` already serves as the manual override toggle — it is correct as-is.  
> The "open" status is **never stored** in the database; it is always **computed at render time**.

