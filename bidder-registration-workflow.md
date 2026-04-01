# Gavel - Bidder Registration & KYC Workflow

This document defines the step-by-step lifecycle of a user from initial sign-up to becoming a qualified bidder. This workflow ensures compliance with **Decree No. 21,981/1932** regarding the legal capacity of participants.

## 🔄 Registration States

| State | Description |
| --- | --- |
| `PendingBasicInfo` | User created an account but hasn't completed their profile. |
| `PendingDocuments` | Profile data provided; awaiting mandatory document upload. |
| `UnderReview` | Documents and data are being analyzed by the Auctioneer's team. |
| `ActionRequired` | Discrepancies found (e.g., blurred ID); user must re-submit data. |
| `Approved` | User is fully qualified and authorized to place bids. |
| `Rejected` | User failed credit or legal background checks; bidding is blocked. |

---

## 🛠 Workflow Steps

### Step 1: Initial Registration & Identity Validation
* **Action:** Collection of name, email, phone, and **CPF/CNPJ**.
* **Technical Rule:** Use `FluentValidation` with specialized providers to check CPF/CNPJ status against Brazilian Federal Revenue rules.
* **Native AOT Note:** Ensure validation logic does not rely on heavy reflection.

### Step 2: Document Management
* **Mandatory Files:** Official ID (RG/CNH), Proof of Residence, and Corporate Bylaws (for CNPJ).
* **Storage:** Files must be stored securely with metadata stored in **EF Core 10 JSON columns** for high-performance retrieval.
* **UI/UX:** The Angular frontend must provide real-time upload progress.

### Step 3: Terms and Conditions Acceptance
* **Legal Binding:** User must explicitly accept the "Terms of Use" for each specific auction.
* **Audit Trail:** Every acceptance must be recorded in an **Append-Only** log with a high-precision UTC timestamp and source IP.

### Step 4: Credit & Guarantee Analysis
* **Requirement:** In high-value auctions (e.g., real estate), the system checks for a guarantee deposit or proof of funds.
* **Automation:** Integrated with the state machine; the user cannot move to `Approved` if the guarantee is missing for a "Reserved" auction.

### Step 5: Final Approval & Activation
* **Action:** Manual review by the Auctioneer or Admin.
* **Real-time Notification:** Upon approval, a **SignalR** message is sent to the Angular client to instantly enable the "Place Bid" button without a page refresh.

---

## 🏗 Technical Implementation Details

### State Machine Constraints
* **Strict Transitions:** A user cannot move from `PendingBasicInfo` to `Approved` without passing through `UnderReview`.
* **Concurrency:** Use **EF Core Row Versioning** to prevent two admins from approving/rejecting the same user simultaneously.

### Auditability
* Any change in the user status (e.g., `Approved` -> `Rejected`) must be logged in the **Audit Trail** including the Admin ID and the reason.

### Financial Integrity
* Ensure all data related to credit limits or guarantees uses `decimal` for absolute precision.