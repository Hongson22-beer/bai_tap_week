# FE integration test report (24/09/2026)

## Static checks completed
- Parsed all 28 JS/JSX files with the TypeScript JSX parser: 0 syntax errors.
- No page/component imports `src/data/mockData.js`.
- All application HTTP requests are centralized in `src/services/api.js`.
- Customer vehicle list/detail/booking flow uses backend vehicle/rental APIs.
- Booking availability calendar uses `/api/rentals/vehicle/{idXe}/booked-periods` and `/api/rentals/availability`.
- Staff requests/customers/vehicles/contracts/payments/handover/return/extensions/cancellations use backend services.
- Staff approval button only approves a rental. Contract creation is kept in the Contracts section.
- Admin dashboard uses real APIs: reports, users, customers, vehicles, evaluations and audit logs.
- Admin can update user status/roles, customer CCCD verification, and vehicle status through backend APIs.

## Important backend limitation handled
The backend snapshot does not expose a GET list endpoint for all contracts. Staff contract discovery therefore reads individual `/api/contracts/{id}` endpoints and stops after a run of missing IDs. No mock contracts are shown.

## Runtime checks already confirmed by user during integration
- Customer login and JWT bearer authentication.
- Vehicle listing from backend.
- Rental availability endpoint.
- Booked-period endpoint for vehicle #3.
- Calendar shows PENDING/APPROVED booked periods.
- Online rental creation (#9) and tracking.
- Staff rental list and approval (#9 PENDING -> APPROVED).
- Admin reports/users/vehicles/audit data were reachable in the earlier technical view.

## Local build required
The sandbox used for this patch does not have the project's npm dependencies cached and cannot download them, so `vite build` could not be executed here. JS/JSX syntax parsing passed. Run locally:

```powershell
npm install
npm run build
npm run dev
```
