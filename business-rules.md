# Gavel Business Rules

This document present every business rules from the Gavel, nothing can be do if isn't here. The auction is based in Brazilian legislate.

## Operational Classification and Taxonomy of Auction Modalities

The system logic definitions starts by the sell modality. Each auction type imposes specifications in database and user interface, influencing since the way a lot is created until the winner processing.

### Difference between judicial auction and extrajuticial

The fundamental difference is in the authority origin's that promotes the selling. The judicial auction is from judicial proccess in municipal, state, or federal levels, generally involving the seizure of assets to pay debts or court judgments. In these cases, the system must have fields to Process Number (CNJ), identification of the court and judicial district, In addition to supporting the issuance of specific documents such as the Auction Certificate signed electronically by judges and auctioneers.

On the other hand, the extrajudicial auction ocurs when the item has no involvement with the judicial branch. This method is frequently used by financial institutions to repossess real estate or vehicles due to default on secured loan agreements, or by private companies wishing to renew their assets. In an extrajudicial auction, the negotiation process is more flexible in terms of documentation and release of the auctioned lot, but it still requires the presence of an official auctioneer to guarantee the public trust in the transaction.

| Characteristic | Judicial Auction | Extrajudicial Auction |
| --- | --- | --- |
| Origin of Authority | Court Order (Judicial Authorization) | Owner or Bank Authorization |
| Final Documentation | Auction Adjudication Letter | Auction Sale Note / Deed |
| Asset Release Deadlines | Depends on judge approval | Defined in the public notice (usually faster) |
| Legal Basis | Brazilian Code of Civil Procedure (CPC) | Law 9.514/97 and Private Contracts |
| Exposure to Appeals | High (possibility of legal challenges) | Low (direct contractual rules) |

### Competition Formats: Online, In-Person, and Simultaneous

The system architecture must support differents dispute interfaces. Our focus is in online auction, where the system controls all the security and transparency, validating each bid in real-time. The most complex model is the simultaneous one, which requires total integration between the physical auditorium and the virtual platform; bids made in person must be instantly entered into the system so that remote participants can react, and vice versa. This synchronization demands extremely low latency and a robust two-way communication mechanism, such as WebSockets, to ensure fairness among bidders.

## The Brazilian Regulatory Framework and Decree No. 21,981/1932

In Brazil the operation is governed by Federal Decree No. 21,981/1932, from october 19th 1932, For a system built from scratch, compliance with this decree is not merely a legal detail, but a core functional requirement. The official auctioneer acts as a public official endowed with public faith, and the digital platform must be treated as an extension of their office.

### Bookkeeping Requirements and Public Faith

The system must automate the management of the auctioneer's mandatory records, ensuring that every transaction is recorded in an indelible manner. The Entry Log must include a detailed description of each asset submitted for sale, while the Exit Log (or sales ledger) must record the final sale price, the winning bidder's identification, and the payment terms. The Decree requires that sales statements be delivered to consignors (sellers) with clear information on gross prices and deductions for commissions and expenses. Failure to maintain these records may lead to removal from the auctioneer position, making the integrity of the system database an absolute priority.

### Auctioneer Commission and Service Fees

An immutable business rule in the Brazilian market is the auctioneer's commission, set at 5% of the winning bid amount and mandatorily paid by the buyer. The system must calculate this amount automatically when each lot closes. In addition to the commission, it is common to charge administrative fees to cover operational costs (advertising, yard storage, photography), and these amounts must be clearly defined in the public notice and reflected in the invoice generated for the winning bidder. On certain platforms, such as Superbid, additional intermediation fees may apply depending on the consignor, ranging from 2.5% to 5%, which requires the system's financial engine to be highly configurable per lot or per auction.

## Auction Lifecycle and Asset State Management

Developing an auction system requires a rigorous state machine. A lot does not simply transition from "open" to "closed"; it goes through a series of phases that define which actions users can perform and which data can be modified.

### Stage 1: Preparation and Cataloging (Draft)

In this initial phase, the system acts as an Asset Management platform. The Catalog Administrator or Appraiser enters the asset's technical information and uploads photos and inspection reports. Images should be considered illustrative only and not used as the sole reference for the asset's condition, and the system is responsible for warning users about the need for prior inspection. The lot remains in Draft status until all mandatory documents, such as the public notice and the consignor's sale authorization, are attached and validated.

### Stage 2: Publication and Preview Period (Scheduled)

Once approved, the auction is published. In this state, users can view lots, download the public notice, and schedule in-person asset visits. It is essential that the system prevents edits to core asset characteristics after publication, unless there is a formal amendment to the public notice. A visible countdown timer must indicate the time remaining until the start and end of the public session.

### Stage 3: Public Session and Bidding (Active)

The start of the public session marks the most technically demanding phase. The system must process bids in real time, validate minimum bid increments, and update the interface for all connected users within milliseconds. During this phase, the system must support manual and automatic bids (proxy bidding), ensuring that bid precedence is respected based on the server timestamp.

### Stage 4: Closing and Time Extension (Closing)

To prevent bid sniping (bids in the final seconds), the system must implement automatic time extension. If a bid is received, for example, in the last 3 minutes, the timer is reset for that specific lot for an additional 3 minutes. This process continues until no further bids are placed, maximizing asset value for the seller and ensuring fairness among bidders.

### Stage 5: Post-Auction and Settlement (Settlement)

After closing, the system determines the winner. If the reserve price is met, the state changes to "Awarded" and charges are generated. If the reserve price is not met, the lot enters "Conditional" status, initiating an approval flow between the consignor and the auctioneer. The cycle ends with payment confirmation, release of the sale note, and scheduling of asset pickup.

| Lot State | Technical Description | Allowed Actions |
| --- | --- | --- |
| Draft | Internal creation of metadata and media. | Full editing by administrators. |
| Scheduled | Visible to the public; pre-bids accepted. | Viewing; user qualification/approval. |
| Active | Public real-time bidding session. | Manual and automatic bids. |
| Closing | Time-extension phase (Soft Close). | New bids extend the timer. |
| Closed | Timer expired; winner verification in progress. | No bidding actions allowed. |
| Conditional | Reserve price not met; pending approval. | Approval/rejection by the consignor. |
| Sold | Payment confirmed; sale completed. | Generation of sale note/pickup release. |
| Unsold | No bids or conditional sale rejected. | Transfer to a new auction or return. |

## Bidding Engine Engineering and Proxy Bidding Logic

The bidding engine is the most critical component of the architecture. It must be designed to handle high concurrency and preserve data integrity under extreme load.

### Increment Rules and Starting Value

Every lot starts with a minimum value defined by the auctioneer or consignor. The system must enforce a mandatory "minimum increment" for each new bid. For example, if the increment is BRL 1,000.00, a bid that is only BRL 500.00 above the current bid must be immediately rejected by the API. This increment value may be fixed or tiered according to the current asset value (for example: BRL 100 bids up to BRL 5,000, BRL 500 bids above that).

### Detailed Automatic Bid Logic (Proxy Bidding)

Proxy Bidding allows a bidder to set the maximum amount they are willing to pay, delegating to the system the task of covering competing offers with the smallest possible increment. Tie-break and progression logic are crucial:

1. Entry: User A sets a maximum of BRL 10,000.00 for a lot with a current bid of BRL 5,000.00 and an increment of BRL 500.00. The system records User A's current bid as BRL 5,500.00.

2. Competition: User B submits a manual bid of BRL 6,000.00. The system processes it instantly and places User A in the lead at BRL 6,500.00.

3. Maximum Tie: If User C sets an automatic bid of BRL 10,000.00 (exactly the same as User A), User A remains the leader because their proposal was submitted first. The system must inform User C that they were immediately outbid, since the current leader already covers that level.

4. Limit Reached: If a third manual bid is BRL 11,000.00, User A is notified that they were outbid and the system stops automatic bidding for them.

### Immutability and Auditability

Unlike e-commerce systems, where orders can be changed, an auction bid is a firm and irrevocable commitment. The bids table in the database must be treated as an immutable record (append-only log). Each entry must contain the user ID, lot ID, amount, high-precision timestamp, and source IP for audit and dispute-resolution purposes. In case of legal challenge, the system must be able to reconstruct the exact bidding sequence, proving that the engine followed the established time and value rules.

## User Governance and Bidder Qualification Process (KYC)

Participating in an auction requires the immediate formation of a legal contract at the moment the lot is awarded. Therefore, identity management and verification of legal capacity are fundamental security requirements.

### Roles and Permission Matrix

The system must implement Role-Based Access Control (RBAC) to manage each stakeholder's responsibilities.

| Role | Description and Responsibilities | Main Permissions |
| --- | --- | --- |
| Auctioneer/Owner | Legally responsible for the auction; highest authority. | Publish auction, finalize lots ("hammer down"), cancel lots. |
| Admin/Manager | Operational team of the auction office. | Register assets, manage users, view reports. |
| Seller/Consignor | Asset owner (bank, company, or individual). | Approve conditional bids, view sales metrics. |
| Bidder | External participant qualified to buy. | Place bids, download payment slips, edit personal profile. |
| Appraiser/Valuator | Specialist technician for asset inspections. | Add reports, technical photos, and market valuations. |
| Viewer | Auditor, inspector, or board member. | Full read-only access with no editing power. |

### Bidder Registration and Qualification Flow

No one should be allowed to bid without first completing a qualification process. This flow must be robust to prevent fraud and default.

1. Initial Registration: Collection of basic data (email, CPF/CNPJ, phone number). The system must validate CPF status in the Federal Revenue database and CNPJ status in SINTEGRA.

2. Document Upload: Required copies of documents (ID card, proof of address, and articles of incorporation for companies).

3. Terms and Conditions Acceptance: The user must explicitly acknowledge that they have read and agreed to the sale and payment terms for the specific auction.

4. Credit/Guarantee Analysis: In large auctions, the system may require a guarantee check deposit or proof of funds before enabling the bid button.

5. Manual/Automatic Approval: An administrator reviews the data and activates the user's profile for the requested auction.

## Financial, Tax, and Asset Settlement Management

Closing an auction triggers a complex financial flow involving multiple beneficiaries and immediate tax obligations.

### Invoicing and Final Price Composition

At the moment a lot is awarded, the system must automatically generate a cost breakdown for the buyer. The total amount payable ($V_t$) is generally composed by the following formula:

$$V_t = V_a + (V_a \times 0.05) + T_{adm} + T_{int} + Imp$$

Where:
* $V_a$ is the award value (winning bid).
* $0.05$ represents the mandatory 5% auctioneer commission.
* $T_{adm}$ represents fixed administrative fees per lot.
* $T_{int}$ is the platform intermediation fee (if applicable).
* $Imp$ refers to taxes such as ICMS (for certain movable assets) or ITBI (for real estate), which may be the direct responsibility of the winning bidder.

### Payment Deadlines and Consequences of Default

The standard rule in the electronic auction market is short-term settlement. The winning bidder must make full payment, usually via TED, DOC, or identified bank slip, within up to 3 business days. If payment is not detected by the system within this period, the award is canceled and the user is marked as delinquent. Automatic penalties should include:

* Withdrawal Penalty: Charge of 5% to 25% of the bid amount to cover expenses and lost commissions.
* Registration Suspension: Immediate block from participating in other auctions on the platform or partner network.
* Debt Protest: The system must integrate with credit-protection services to register the debt.

## Legal Documentation: Certificate of Sale and Deed of Sale

The legal validity of an electronic auction depends on correctly issuing title-transfer documents. The system must operate as a compliant document generator aligned with the Brazilian Code of Civil Procedure and Decree 21.981/32.

### Certificate of Sale (Preliminary)

Prepared immediately after the public session ends, the Certificate of Sale is the documentary summary of the judicial sale act. It must include identification of the property or asset, the name and qualification of the winning bidder, the sale amount, and the payment method. In electronic auctions, the auctioneer and winning bidder signatures may be digital (ICP-Brasil standard), but in judicial auctions, the document must be submitted to the judge for signature, making the sale final and irrevocable.

### Deed of Sale (Definitive)

This is the final ownership title that the winning bidder will present to the Real Estate Registry Office to transfer ownership into their name. The deed is issued by the judge after proof of full payment of the winning bid, auctioneer commission, and ITBI. The system must track the status of these proofs and alert the auctioneer's legal team to request issuance from the competent court as soon as the requirements are met.

## Non-Functional Requirements: Architecture, Security, and Performance

For the architect/developer, the technical specifications of an auction system are as critical as the business rules. A 2-second failure can result in major financial loss or legal annulment of the auction.

### Real-Time Communication and Latency

The platform must use low-latency bidirectional communication technologies, such as WebSockets (SignalR), to ensure immediate bid updates across all connected devices. The system should implement a heartbeat mechanism to monitor user connection quality and warn users when their latency is too high, which could impair last-second bidding.

### Scalability and Peak Management (Burst Traffic)

Auctions have a highly asymmetric traffic pattern. The system may have 100 users browsing calmly during the day and 50,000 users sending simultaneous requests in the final 5 minutes of a popular vehicle auction. Recommended measures:

* Serverless functions or auto-scaling: to handle bidding-processing spikes.
* High-performance cache (Redis): to store the current state of active lots, avoiding constant queries to the relational database during disputes.
* Message queue (Kafka): to process asynchronous tasks such as payment slip generation, email delivery, and audit-log registration without blocking the bidding engine.

### Security and Data Integrity

Following compliance standards for platforms that handle sensitive and financial data:

* Encryption: TLS 1.2+ in transit and AES-256 for data at rest.
* PCI DSS: if the system stores cards for guarantees, tokenization or certified gateways must be used.
* Audit trail logs: each administrative action (change reserve price, suspend user) must be logged with user, date, time, and previous/new value to support later forensic analysis.

## Real-Time Notifications and Engagement

Auction success depends on keeping the bidding dispute active. The notification system must be omnipresent and multi-channel.

### Critical Notification Scenarios

* Outbid: the most important trigger. It must be fired within milliseconds via Push and WebSocket so the user can return to the dispute.
* Conditional Lot: notify the winning bidder that their offer is under review and later whether it was approved or rejected by the consignor.
* Payment Slip Due Date: payment reminders to avoid default and account blocking.
* New Opportunities: notify users based on bid history (for example: "New Toyota Corolla auction started").

| Notification Channel | Recommended Use | Priority |
| --- | --- | --- |
| WebSocket | Current-bid UI updates while the user is logged in. | Critical |
| Push (Mobile) | Outbid alert when the user is outside the app. | High |
| Email | Award confirmation, sale note delivery, and payment slips. | Medium |
| SMS/WhatsApp | Urgent payment alerts or auction schedule changes. | High |

## Post-Sale Logistics and Asset Pickup

System responsibility does not end at payment; it must manage physical asset delivery, which is often an operational bottleneck.

### Scheduling and Yard Control

The system must allow the winning bidder to schedule an asset pickup time, integrated with the operational capacity of storage yards. At pickup time, the system should record delivery through a signed release term (digitally or physically) and verify whether the transport vehicle is compatible with the asset (for example: flatbeds for damaged vehicles).

### Storage and Abandonment Rules

To avoid indefinite occupation of space, the system must calculate storage fees if the asset is not picked up within the contractual deadline (for example: 10 business days). If the asset remains in the yard for more than 30 or 60 days without justification, the system must trigger legal alerts, since the sale may be rescinded with total loss of paid amounts, and the asset may be re-auctioned to cover custody costs.

## Conclusions and Recommendations for System Engineering

Building an auction system from scratch is an endeavor that requires a multidisciplinary approach, combining legal rigor, gamification strategy, and mission-critical software engineering. For the system architect, the primary recommendation is clear separation between the Real-Time Transaction Engine (Bidding) and the Back-Office Management System (auctioneer ERP/CRM). While the first requires horizontal scalability and near-zero latency, the second focuses on documentary integrity and financial workflows.

Transparency must be a pillar of the user experience. Providing clear information about asset condition, applicable fees, and Soft Close rules reduces friction and post-auction legal disputes. In addition, implementing automatic bids (Proxy Bidding) is not only a competitive differentiator but also a market necessity that increases engagement and final asset prices.

Atomic Time Extension: The time extension trigger must be atomic. If an auction is scheduled to close at 20:00:00 and a bid is received at 19:59:58, the new closing time must be calculated immediately and propagated via SignalR to prevent UI desynchronization. This ensures no user incorrectly perceives the lot as 'Closed' while it remains active due to the extension.

Finally, compliance with Decree 21.981/32 and Commercial Registry standards will ensure that the platform has the robustness required to operate as an official public-sales agent in Brazil. By automating Certificate of Sale generation and mandatory-book recordkeeping, the system not only simplifies the auctioneer's work but also raises the security standard for all participants in the electronic auction ecosystem.