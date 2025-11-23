# COPPA Compliance Feature PRD

**Status:** 🚧 **IN PROGRESS - NOT YET IMPLEMENTED**
**Priority:** 🔴 **CRITICAL - LEGAL REQUIREMENT**
**Owner:** Product & Engineering
**Last Updated:** November 23, 2025
**Related:** BUG-1 (Security), FEAT-INC-1 (Feature Gap), FEAT-NEW-2 (Parent Dashboard)

---

## Executive Summary

This PRD defines the requirements for full compliance with the Children's Online Privacy Protection Act (COPPA), a United States federal law that governs online collection of personal information from children under 13. **Implementation is mandatory before any children's data can be collected.** Non-compliance risks $50,000+ FTC fines per violation and platform shutdown.

**Current Status:** COPPA compliance features are **NOT IMPLEMENTED**. A prominent banner has been added to the PWA alerting stakeholders.

**Critical Path Items:**
1. Age gate before any data collection
2. Parental consent mechanism with verifiable methods
3. Parent dashboard for oversight and control
4. Data minimization and protection policies
5. Legal review and attestation

---

## Background & Problem Statement

### **Business Context**
Mystira is a dynamic storytelling platform designed for children, featuring character progression, developmental tracking, and interactive narratives. Under COPPA:
- **Children under 13** require verifiable parental consent before personal information collection
- **Parents** must have controls to review, delete, and manage their child's data
- **Platform** must minimize data collection and implement strong privacy protections

### **Regulatory Requirements**
COPPA mandates:
- **Notice:** Clear privacy policy explaining data practices
- **Verifiable Parental Consent:** Multiple acceptable methods (credit card, government ID, video call, etc.)
- **Parent Access:** Ability to review child's personal information
- **Parent Controls:** Right to refuse further collection and delete account
- **Data Minimization:** Collect only necessary information
- **Confidentiality:** Reasonable security measures
- **Prohibition:** No conditioning participation on disclosing more information than necessary

### **Current State**
- ❌ No age gate
- ❌ No parental consent system
- ❌ No parent dashboard
- ❌ No data deletion workflows
- ❌ Privacy policy does not address COPPA
- ⚠️ PII currently logged without redaction (being fixed)
- ⚠️ No data retention policies

**Risk Assessment:** **CRITICAL** - Operating illegally if collecting children's data

---

## Goals & Success Metrics

### **Business Goals**
1. **Legal Compliance:** Achieve 100% COPPA compliance
2. **Parent Trust:** Build confidence through transparency and control
3. **User Experience:** Minimize friction while ensuring safety
4. **Operational Excellence:** Scalable, auditable consent and data management

### **Success Metrics**
| Metric | Target | Measurement |
|--------|--------|-------------|
| Parental Consent Approval Rate | 95%+ | % of consent requests approved |
| Consent Verification Time | < 24 hours | Time from request to verification |
| Parent Dashboard Adoption | 80%+ | % of parents who use dashboard |
| Data Deletion Response Time | < 7 days | Time to fulfill deletion requests |
| Zero FTC Violations | 100% | No compliance failures |
| Parent Satisfaction (NPS) | 70+ | Survey score |

---

## User Personas

### **Primary: Parents/Guardians**
- **Age:** 25-45
- **Tech Savviness:** Varies (design for low to high)
- **Motivations:** Child's safety, development, entertainment
- **Concerns:** Privacy, data security, age-appropriate content
- **Needs:**
  - Easy consent process
  - Visibility into child's activity
  - Simple data controls
  - Trust signals (certifications, transparency)

### **Secondary: Children (Under 13)**
- **Age:** 5-12 (varies by family)
- **Tech Savviness:** Growing, requires guidance
- **Motivations:** Fun stories, character progression, achievements
- **Needs:**
  - Age-appropriate UX
  - Parental approval understanding
  - No exposure to personal data flows

### **Tertiary: Platform Administrators**
- **Role:** Support, compliance, operations
- **Needs:**
  - Consent audit trails
  - Data deletion tools
  - Compliance reporting
  - Incident response procedures

---

## Functional Requirements

### **FR-1: Age Gate**
**Priority:** 🔴 Critical
**Status:** Not Implemented

**Description:** Implement age verification before ANY data collection or account creation.

**Requirements:**
- **FR-1.1:** Display age gate on first visit (before signup/signin)
- **FR-1.2:** Ask for birth date or age
- **FR-1.3:** If under 13, redirect to parental consent flow
- **FR-1.4:** If 13+, proceed to normal signup/signin
- **FR-1.5:** Store age verification status (no PII) in session
- **FR-1.6:** Block all personal data collection until parental consent obtained

**Acceptance Criteria:**
- ✅ Age gate appears before signup/signin
- ✅ Under-13 users cannot create accounts without parental consent
- ✅ Age determination does not store birth dates (COPPA-compliant)
- ✅ Clear messaging explains why parental consent is required

**Edge Cases:**
- User lies about age → Detected via behavior patterns, account review
- User refreshes page → Age gate reappears until consent obtained
- Multiple children in household → Parent dashboard handles multiple child profiles

---

### **FR-2: Parental Consent Request**
**Priority:** 🔴 Critical
**Status:** Not Implemented

**Description:** Implement verifiable parental consent mechanism before collecting children's personal information.

**Requirements:**
- **FR-2.1:** Collect parent email (separate from child profile)
- **FR-2.2:** Send consent request email with:
  - Clear explanation of data practices
  - Link to full privacy policy
  - Verification options
- **FR-2.3:** Support multiple verification methods (COPPA-compliant):
  - **Method 1:** Credit card verification ($0.50 charge + immediate refund)
  - **Method 2:** Government ID verification (3rd party service)
  - **Method 3:** Video call verification (scheduled appointment)
  - **Method 4:** Signed consent form (mail + notary)
- **FR-2.4:** Store consent records with:
  - Parent email (hashed)
  - Child profile ID
  - Consent date/time
  - Verification method used
  - Consent status
  - IP address (for fraud detection)
- **FR-2.5:** Block child account activation until consent verified
- **FR-2.6:** Consent expiration: None (or re-verify annually)

**Acceptance Criteria:**
- ✅ Parent receives clear consent request email
- ✅ At least one verification method available
- ✅ Consent audit trail maintained
- ✅ Child account blocked until verified
- ✅ FTC-compliant consent process

**Edge Cases:**
- Parent doesn't respond → Reminder emails (3x over 30 days), then expire request
- Invalid parent email → Request retry with validation
- Verification fails → Offer alternative methods
- Parent revokes consent → Immediate account deletion triggered

---

###**FR-3: Parent Dashboard**
**Priority:** 🔴 Critical
**Status:** Not Implemented (see FEAT-NEW-2)

**Description:** Provide parents with oversight and control over their child's data and activity.

**Requirements:**
- **FR-3.1:** Parent login (separate from child account)
- **FR-3.2:** Dashboard Features:
  - **Activity Log:** View child's game sessions, choices, progress (sanitized)
  - **Data Summary:** See what personal information is stored
  - **Privacy Controls:**
    - Revoke consent → Trigger account deletion
    - Export data → Download all child data (JSON/PDF)
    - Delete account → Permanent removal of all data
  - **Content Controls:**
    - View scenarios child accessed
    - Set age-appropriate content filters
    - Pause/unpause account access
  - **Reports:** Weekly/monthly activity summaries (email opt-in)
- **FR-3.3:** Real-time notifications:
  - New game session started
  - Achievement earned
  - Privacy policy updates
- **FR-3.4:** Multi-child support: One parent can manage multiple child accounts

**Acceptance Criteria:**
- ✅ Parent can log in securely
- ✅ All child data is viewable
- ✅ Export functionality works (JSON + PDF)
- ✅ Delete functionality removes all data within 7 days
- ✅ Mobile-responsive design
- ✅ Clear, non-technical language

**Edge Cases:**
- Multiple parents per child → Primary + secondary parent roles
- Parent forgets password → Secure recovery via email + verification
- Child tries to access parent dashboard → Separate auth system prevents access

---

### **FR-4: Data Minimization**
**Priority:** 🔴 Critical
**Status:** Partially Implemented

**Description:** Collect only the minimum necessary personal information from children.

**Requirements:**
- **FR-4.1:** Required Fields Only:
  - **Required:** Display name (pseudonymous encouraged)
  - **Optional:** Age range (not exact birth date)
  - **Prohibited:** Email, phone, address, school name, photos
- **FR-4.2:** PII Redaction in Logs (✅ In Progress - see BUG-4 fix)
- **FR-4.3:** No tracking cookies or behavioral advertising
- **FR-4.4:** No third-party data sharing without explicit consent
- **FR-4.5:** Anonymous usage analytics only (no cross-session tracking)

**Acceptance Criteria:**
- ✅ Signup form requests minimal fields
- ✅ No PII in server logs
- ✅ No tracking cookies without consent
- ✅ Privacy policy clearly states data practices

---

### **FR-5: Data Deletion & Retention**
**Priority:** 🔴 Critical
**Status:** Not Implemented

**Description:** Implement data deletion workflows and retention policies.

**Requirements:**
- **FR-5.1:** Data Retention Policy:
  - Active accounts: Indefinite (with parent consent)
  - Inactive accounts: 180 days warning → 365 days deletion
  - Deleted accounts: 7-day soft delete → permanent deletion
  - Revoked consent: Immediate soft delete → 7-day permanent deletion
- **FR-5.2:** Deletion Process:
  - User-initiated (via parent dashboard)
  - System-initiated (retention policy)
  - Support-initiated (upon request)
- **FR-5.3:** Deletion Scope:
  - Remove from Cosmos DB
  - Remove media assets from Blob Storage
  - Purge from logs (where possible)
  - Remove from backups (within retention window)
  - Update audit trail (record deletion, not recover data)
- **FR-5.4:** Deletion Confirmation:
  - Email to parent confirming deletion
  - Audit log entry
  - Compliance report generation

**Acceptance Criteria:**
- ✅ Parent can request deletion from dashboard
- ✅ Deletion completes within 7 days
- ✅ All child data removed from all systems
- ✅ Parent receives confirmation
- ✅ Audit trail maintained (deletion event, not deleted data)

---

### **FR-6: Privacy Policy & Notice**
**Priority:** 🔴 Critical
**Status:** Not Implemented

**Description:** Provide clear, COPPA-compliant privacy notice.

**Requirements:**
- **FR-6.1:** Privacy Policy Document:
  - Types of information collected
  - How information is used
  - Third-party sharing (if any)
  - Parent rights (review, delete, refuse)
  - Contact information for privacy inquiries
  - Date of last update
- **FR-6.2:** Prominent display:
  - Link in footer
  - During consent flow
  - In parent dashboard
- **FR-6.3:** Plain language (8th grade reading level max)
- **FR-6.4:** Available before any data collection

**Acceptance Criteria:**
- ✅ Privacy policy published and accessible
- ✅ FTC-compliant content
- ✅ Legal review completed
- ✅ Versioning tracked (for updates)

---

### **FR-7: Data Security**
**Priority:** 🔴 Critical
**Status:** Partially Implemented

**Description:** Implement reasonable security measures to protect children's data.

**Requirements:**
- **FR-7.1:** Encryption:
  - HTTPS for all connections (✅ Implemented)
  - At-rest encryption for database (✅ Azure Cosmos DB default)
  - At-rest encryption for blob storage (✅ Azure Blob default)
- **FR-7.2:** Access Controls:
  - Role-based access (API, Admin API separation) (✅ Implemented)
  - Least privilege principle
  - MFA for admin access (⚠️ Not verified)
- **FR-7.3:** Secret Management:
  - Azure Key Vault for secrets (⚠️ In progress - see BUG-1 fix)
  - No hardcoded secrets (✅ Fixed in this PR)
  - Regular key rotation
- **FR-7.4:** Audit Logging:
  - Log all data access (PII-redacted) (✅ In progress)
  - Monitor for suspicious activity
  - Incident response procedures

**Acceptance Criteria:**
- ✅ All data encrypted in transit and at rest
- ✅ Access controls enforced
- ✅ Secrets managed securely
- ✅ Audit trail maintained
- ✅ Security assessment passed

---

## Non-Functional Requirements

### **NFR-1: Performance**
- Consent request email delivery: < 60 seconds
- Parent dashboard load time: < 2 seconds (P99)
- Data export generation: < 5 minutes
- Data deletion completion: < 7 days (soft delete immediate)

### **NFR-2: Scalability**
- Support 100,000+ parent accounts
- Support 500,000+ child accounts
- Handle 1,000+ concurrent parent dashboard users
- Consent verification: 10,000+ requests/day

### **NFR-3: Reliability**
- Parent dashboard uptime: 99.9%
- Consent system uptime: 99.95%
- Data deletion SLA: 100% within 7 days
- Zero data loss

### **NFR-4: Usability**
- Parent dashboard mobile-responsive
- Consent flow completable in < 5 minutes
- Privacy policy readable by 8th grade level
- Multilingual support (initially: English, Spanish)

### **NFR-5: Compliance**
- 100% COPPA adherence
- Regular FTC compliance audits
- Incident response plan (< 1 hour notification)
- Legal review approval required

---

## User Flows

### **Flow 1: Child Signup with Parental Consent**

```
1. User visits mystira.app
2. Age gate appears: "How old are you?"
3. User enters age (e.g., 9 years old)
4. System detects under 13 → Parental consent required
5. Display: "To keep you safe, we need your parent's permission!"
6. Form: "Parent's Email Address"
7. User enters parent email → Submit
8. Confirmation: "We've sent an email to [parent email]. Ask your parent to check their email!"
9. System sends consent email to parent with verification link
10. Parent clicks link → Consent page
11. Parent reviews privacy policy and data practices
12. Parent selects verification method (e.g., credit card)
13. Parent completes verification
14. System marks consent as verified
15. Confirmation email to parent + child account activated
16. Child can now sign in and use platform
```

### **Flow 2: Parent Dashboard - View Activity**

```
1. Parent navigates to parent.mystira.app (or /parent-dashboard)
2. Parent logs in (email + passwordless code)
3. Dashboard displays:
   - Child accounts (if multiple)
   - Recent activity summary
   - Quick actions (export data, delete account, settings)
4. Parent selects "View Activity"
5. Activity log displays:
   - Date/time of sessions
   - Scenarios played
   - Choices made (sanitized)
   - Achievements earned
   - Developmental progress (compass axes)
6. Parent can filter by date range, scenario, or activity type
7. Parent can export activity log (PDF/JSON)
```

### **Flow 3: Parent-Initiated Account Deletion**

```
1. Parent logs into parent dashboard
2. Navigates to child's account settings
3. Clicks "Delete Account"
4. Warning modal:
   "This will permanently delete all of [child name]'s data, including:
   - Game progress
   - Achievements
   - Character data
   This action cannot be undone. Are you sure?"
5. Parent confirms deletion
6. System:
   - Soft deletes account (immediate)
   - Sends confirmation email to parent
   - Queues permanent deletion job (7 days)
7. Confirmation message: "Account deletion in progress. All data will be permanently removed within 7 days."
8. Parent receives email confirmation with deletion ID
9. After 7 days: Permanent deletion job runs
   - Removes from Cosmos DB
   - Removes media from Blob Storage
   - Updates audit log
10. Final confirmation email to parent: "Deletion complete"
```

---

## Technical Implementation

### **Architecture**

```
┌─────────────────────────────────────────────────────────┐
│  PWA (Blazor WebAssembly)                               │
│  - Age Gate Component                                   │
│  - Parental Consent Flow                                │
│  - Parent Dashboard (new)                               │
└────────────────────┬────────────────────────────────────┘
                     │ HTTPS
┌────────────────────▼────────────────────────────────────┐
│  API Layer                                              │
│  - /api/coppa/age-gate                                  │
│  - /api/coppa/consent/request                           │
│  - /api/coppa/consent/verify                            │
│  - /api/parent/dashboard (new)                          │
│  - /api/parent/data-export (new)                        │
│  - /api/parent/data-delete (new)                        │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│  Application Layer (Use Cases)                          │
│  - RequestParentalConsentUseCase                        │
│  - VerifyParentalConsentUseCase                         │
│  - GetParentDashboardDataUseCase                        │
│  - ExportChildDataUseCase                               │
│  - DeleteChildAccountUseCase                            │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│  Domain Layer                                           │
│  - ParentalConsent (entity)                             │
│  - ConsentStatus enum                                   │
│  - VerificationMethod enum                              │
│  - DataDeletionRequest (entity)                         │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│  Infrastructure Layer                                   │
│  - ParentalConsentRepository                            │
│  - DataDeletionService                                  │
│  - ConsentEmailService                                  │
│  - VerificationService (credit card, ID, etc.)          │
└─────────────────────────────────────────────────────────┘
```

### **Database Schema (Cosmos DB)**

```json
// ParentalConsent Document
{
  "id": "guid",
  "type": "ParentalConsent",
  "parentEmailHash": "sha256-hash",
  "childProfileId": "guid",
  "childDisplayName": "pseudonym",
  "consentDate": "2025-11-23T12:00:00Z",
  "verificationMethod": "CreditCard|GovernmentID|VideoCall|SignedForm",
  "verificationDate": "2025-11-23T12:05:00Z",
  "consentStatus": "Pending|Approved|Denied|Revoked",
  "ipAddress": "hashed-ip",
  "expirationDate": null,
  "consentText": "Full text of what parent consented to",
  "privacyPolicyVersion": "v1.0",
  "createdAt": "2025-11-23T12:00:00Z",
  "updatedAt": "2025-11-23T12:05:00Z"
}

// DataDeletionRequest Document
{
  "id": "guid",
  "type": "DataDeletionRequest",
  "childProfileId": "guid",
  "requestedBy": "Parent|System|Support",
  "requestDate": "2025-11-23T12:00:00Z",
  "scheduledDeletionDate": "2025-11-30T12:00:00Z",
  "status": "Pending|InProgress|Completed",
  "deletionScope": ["CosmosDB", "BlobStorage", "Logs"],
  "confirmationEmailSent": true,
  "completedAt": null,
  "auditTrail": [
    {
      "action": "Requested",
      "timestamp": "2025-11-23T12:00:00Z",
      "performedBy": "parent-email-hash"
    }
  ]
}
```

### **API Endpoints**

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/coppa/age-gate` | Submit age verification | None |
| POST | `/api/coppa/consent/request` | Request parental consent | None |
| POST | `/api/coppa/consent/verify` | Verify consent (callback) | Signed token |
| GET | `/api/parent/dashboard` | Get parent dashboard data | Parent auth |
| POST | `/api/parent/data-export` | Export child data | Parent auth |
| POST | `/api/parent/data-delete` | Request account deletion | Parent auth |
| GET | `/api/parent/activity-log` | Get child activity | Parent auth |
| PUT | `/api/parent/content-controls` | Update content filters | Parent auth |

---

## Dependencies & Integration Points

### **Internal Dependencies**
- **Authentication System:** Parent authentication separate from child accounts
- **Email Service:** Azure Communication Services for consent emails
- **User Profile System:** Extend to include age, consent status
- **Game Session System:** Sanitize activity logs for parent viewing

### **External Dependencies**
- **Credit Card Verification:** Stripe, PayPal, or similar (micro-transaction)
- **ID Verification:** Persona, Onfido, or Jumio for government ID
- **Video Call Scheduling:** Calendly or custom scheduling (for video verification)
- **Legal Review:** External counsel for privacy policy and compliance attestation

### **Third-Party Services (Potential)**
- **Privo:** COPPA-compliant consent platform (turnkey solution)
- **SuperAwesome:** Kid-safe technology platform
- **Kidoz:** Parental consent and kid-safe SDK

---

## Rollout Plan

### **Phase 1: Foundation (Weeks 1-2)**
- ✅ Remove hardcoded secrets (completed in this PR)
- ✅ Add PII redaction (completed in this PR)
- ✅ Add security headers (completed in this PR)
- Create privacy policy (legal review)
- Set up parent authentication system

### **Phase 2: Age Gate & Consent Request (Weeks 3-4)**
- Implement age gate component
- Build parental consent request flow
- Integrate email service for consent requests
- Implement one verification method (credit card recommended)
- Create consent audit logging

### **Phase 3: Parent Dashboard (Weeks 5-6)**
- Build parent login/dashboard UI
- Implement activity log viewing
- Add data export functionality
- Add data deletion request workflow
- Mobile responsiveness

### **Phase 4: Data Lifecycle (Week 7)**
- Implement data deletion jobs
- Set up data retention policies
- Build audit trail reporting
- Create compliance dashboard (admin)

### **Phase 5: Testing & Compliance (Week 8)**
- End-to-end testing of all flows
- Security audit and penetration testing
- Legal compliance review
- FTC guidance verification
- User acceptance testing (parents)

### **Phase 6: Launch (Week 9)**
- Deploy to production
- Monitor consent request success rates
- Parent onboarding support
- Incident response readiness

---

## Success Criteria

### **Go-Live Checklist**
- [ ] Age gate implemented and tested
- [ ] At least one consent verification method working
- [ ] Parental consent records stored securely
- [ ] Parent dashboard functional (view, export, delete)
- [ ] Data deletion workflow tested
- [ ] Privacy policy published and FTC-compliant
- [ ] PII redaction in place across all systems
- [ ] Security assessment passed
- [ ] Legal review approved
- [ ] Incident response plan documented
- [ ] Support team trained

### **Post-Launch Monitoring**
- Daily: Consent request success rate
- Daily: Data deletion request fulfillment rate
- Weekly: Parent dashboard adoption rate
- Weekly: Privacy policy acceptance rate
- Monthly: FTC compliance audit
- Quarterly: Parent satisfaction survey (NPS)

---

## Risks & Mitigation

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Low parent consent approval rate | High | Medium | Streamline verification, offer multiple methods |
| FTC audit failure | Critical | Low | Pre-launch legal review, regular compliance checks |
| Data deletion not complete | Critical | Low | Automated testing, audit trails, 7-day SLA buffer |
| Parent dashboard poor UX | Medium | Medium | User testing, iterative design, clear documentation |
| Credit card verification friction | Medium | High | Offer alternative methods, explain necessity |
| Consent email not delivered | Medium | Medium | Email deliverability testing, retry logic, parent support |
| Data breach during implementation | Critical | Low | Security audit, encryption, access controls |

---

## Open Questions

1. **Verification Method Priority:** Which verification method should be primary? (Recommendation: Credit card for speed)
2. **Consent Expiration:** Should parental consent expire annually? (FTC allows indefinite with proper notice)
3. **Multi-Language Support:** Which languages to support initially? (Recommendation: English + Spanish)
4. **Data Export Format:** JSON only, or also PDF/CSV? (Recommendation: JSON + user-friendly PDF)
5. **Third-Party Solution:** Should we use a turnkey COPPA service like Privo? (Trade-off: Cost vs. speed vs. control)
6. **Age Gate Placement:** Show before or after landing page content? (Recommendation: Immediate gate for compliance)

---

## Resources & References

### **FTC COPPA Resources**
- [COPPA Rule Overview](https://www.ftc.gov/business-guidance/privacy-security/childrens-privacy)
- [Complying with COPPA: Frequently Asked Questions](https://www.ftc.gov/business-guidance/resources/complying-coppa-frequently-asked-questions)
- [COPPA Safe Harbor Programs](https://www.ftc.gov/business-guidance/privacy-security/childrens-privacy/coppa-safe-harbor-programs)

### **Technical References**
- Age gate best practices: [Common Sense Media](https://www.commonsensemedia.org/)
- Parental consent UX: [Privo Case Studies](https://www.privo.com/)
- Data minimization: [Privacy by Design Principles](https://www.ipc.on.ca/wp-content/uploads/resources/7foundationalprinciples.pdf)

### **Internal Documents**
- `docs/best-practices.md` - Security and privacy standards
- `PRODUCTION_REVIEW_REPORT.md` - BUG-1 (secrets), BUG-4 (PII logging), FEAT-INC-1 (this PRD)
- `docs/POTENTIAL_ENHANCEMENTS_ROADMAP.md` - Section 10 (COPPA implementation roadmap)

---

## Approval & Sign-Off

**Required Approvals:**
- [ ] Product Owner
- [ ] Engineering Lead
- [ ] Legal Counsel
- [ ] Privacy Officer (if applicable)
- [ ] Executive Sponsor

**Approval Date:** _________________

**Notes:**
_____________________________________________________________________________

---

*This PRD is a living document and will be updated as implementation progresses and requirements evolve.*
