# HealSync — Future Enhancements Backlog

This file tracks improvement ideas that are **good to have but not implementable yet**, because the underlying backend/module doesn't exist. Pull items from here into your sprint board once the dependency is built. Add new items to the bottom of the relevant section as they come up — don't lose them in chat history.

---

## 🔴 Blocked on: Ratings & Reviews module (not built yet)

- [ ] Doctor ratings/reviews (stars + review count) shown on doctor cards in search/list
- [ ] "Rate your experience" prompt after a completed appointment (star rating + optional comment)
- [ ] Sort doctor list by "Rating (high to low)"
- [ ] Doctor-side: show average rating + review count on doctor's own profile/dashboard
- [ ] Admin-side: moderate/flag inappropriate reviews

## 🔴 Blocked on: Payments module (not built yet)

- [ ] Integrated payment gateway (Razorpay/PayU) with "Pay Now" / "Pay at Clinic" option
- [ ] Show estimated consultation fee + full payment breakdown (fee + platform fee + tax) before confirming booking
- [ ] Doctor dashboard: revenue/earnings chart (weekly/monthly)
- [ ] Doctor dashboard: "This week's revenue" stat card
- [ ] Clinic-level performance stats including revenue per clinic
- [ ] Doctor profile: bank account/payout details section (masked, RBI-compliant)
- [ ] Refund handling on cancellation

## 🟡 Blocked on: Other backend modules not yet implemented

*(Update this list based on your actual backend status — add/remove as needed)*

- [ ] Family member management (book appointments for dependents under one account)
- [ ] Medical history section (allergies, chronic conditions, medications) with doctor-consent visibility toggle
- [ ] Document vault (upload/store past prescriptions, lab reports, insurance card)
- [ ] Aadhaar/ABHA ID linking (ABDM integration)
- [ ] Structured prescription generator (medicine name/dosage/duration → auto-generate PDF)
- [ ] Multi-channel notifications (SMS + Push, currently email-only) + per-event channel preference
- [ ] Notification retry/failover (email fails → fallback to SMS)
- [ ] Export audit logs to PDF/Excel
- [ ] Multi-language support (Hindi + regional languages)
- [ ] Offline PWA fallback + cached last-viewed appointment list
- [ ] Doctor "Mark as Available/Unavailable" on-duty toggle
- [ ] Bulk-accept pending appointment requests
- [ ] Patient no-show tracking analytics

---

## ✅ How to use this file
1. Before starting a new sprint, scan this file for anything now unblocked (e.g., payments module is done → move payment items into your active task board).
2. When Claude or you generate new "nice to have but backend-dependent" ideas in future sessions, append them here instead of losing them.
3. Keep completed items struck through or move them to a `CHANGELOG.md` instead of deleting, so you retain a history of what's been built.
