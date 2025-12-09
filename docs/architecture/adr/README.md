# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records (ADRs) for the Mystira.App project.

---

## What is an ADR?

An **Architecture Decision Record** (ADR) is a document that captures an important architectural decision made along with its context and consequences.

### Why ADRs?

- 📝 **Document decisions** - Capture the "why" behind architectural choices
- 🕐 **Historical context** - Future developers understand past decisions
- 🤔 **Thoughtful decisions** - Writing forces deliberate thinking
- 🔍 **Transparency** - Team alignment on technical direction
- 📊 **Evaluation** - Track decision outcomes over time

---

## ADR Format

Each ADR follows this structure:

```markdown
# ADR-XXXX: [Title]

**Status**: [Proposed | Accepted | Deprecated | Superseded]
**Date**: YYYY-MM-DD
**Deciders**: [Who made the decision]
**Tags**: [Relevant tags]

## Context
[What is the issue we're facing?]

## Decision
[What did we decide to do?]

## Consequences
[What becomes easier or harder as a result?]

## References
[Links to supporting documentation]
```

### Status Values

- **Proposed** 💭 - Decision is under consideration
- **Accepted** ✅ - Decision has been made and is being implemented
- **Deprecated** ⚠️ - Decision is no longer valid but kept for historical context
- **Superseded** 🔄 - Decision has been replaced by a newer ADR

---

## Current ADRs

| ADR | Title | Status | Date | Tags |
|-----|-------|--------|------|------|
| [ADR-0001](ADR-0001-adopt-cqrs-pattern.md) | Adopt CQRS Pattern | ✅ Accepted | 2025-11-24 | architecture, cqrs, patterns, application-layer |
| [ADR-0002](ADR-0002-adopt-specification-pattern.md) | Adopt Specification Pattern | ✅ Accepted | 2025-11-24 | architecture, specification, patterns, domain-layer |
| [ADR-0003](ADR-0003-adopt-hexagonal-architecture.md) | Adopt Hexagonal Architecture | ✅ Accepted | 2025-11-24 | architecture, hexagonal, ports-and-adapters, clean-architecture |
| [ADR-0004](ADR-0004-use-mediatr-for-cqrs.md) | Use MediatR for Request/Response Handling | ✅ Accepted | 2025-11-24 | technology, mediatr, cqrs, request-response |
| [ADR-0005](ADR-0005-separate-api-and-admin-api.md) | Separate API and Admin API | ✅ Accepted | 2025-11-24 | architecture, api, security, separation-of-concerns |
| [ADR-0006](ADR-0006-phase-5-cqrs-migration.md) | Phase 5 - Complete CQRS Migration | ✅ Implemented | 2025-11-24 | migration, cqrs, implementation, phase-5 |
| [ADR-0007](ADR-0007-implement-azure-front-door.md) | Implement Azure Front Door for Edge Security | 💭 Proposed | 2025-12-07 | infrastructure, security, azure, front-door, waf |
| [ADR-0008](ADR-0008-separate-staging-environment.md) | Implement Separate Staging Environment | 💭 Proposed | 2025-12-07 | infrastructure, environments, staging, devops |
| [ADR-0009](ADR-0009-staging-pwa-deployment-strategy.md) | Use App Service for Staging PWA Deployment | ✅ Accepted | 2025-12-08 | infrastructure, deployment, pwa, staging, azure, static-web-apps, app-service |

---

## When to Create an ADR

Create an ADR for decisions that:

✅ **Affect project structure** - Layering, module boundaries, etc.
✅ **Introduce new patterns** - CQRS, Event Sourcing, etc.
✅ **Change technology stack** - New libraries, frameworks, databases
✅ **Impact team workflow** - Development processes, testing strategies
✅ **Have long-term consequences** - Hard to reverse decisions
✅ **Are controversial** - When team needs alignment

### Don't Create ADRs For:

❌ **Trivial decisions** - Variable naming, code formatting (use linters)
❌ **Temporary workarounds** - Short-term fixes
❌ **Implementation details** - Specific algorithm choices (document in code)
❌ **Operational concerns** - Deployment configs (use runbooks)

---

## How to Create an ADR

### 1. Create a New File

```bash
cd docs/architecture/adr
touch ADR-XXXX-short-title.md
```

**Naming Convention**:
- Use sequential numbering: `ADR-0001`, `ADR-0002`, etc.
- Use kebab-case for title: `adopt-cqrs-pattern`
- Example: `ADR-0003-adopt-event-sourcing.md`

### 2. Fill in the Template

```markdown
# ADR-XXXX: [Descriptive Title]

**Status**: Proposed
**Date**: 2025-XX-XX
**Deciders**: [Your Name, Team Name]
**Tags**: [Relevant tags]

## Context

Describe the current situation and the problem you're trying to solve.

### Problems with Current Approach

- Problem 1
- Problem 2

### Considered Alternatives

1. **Alternative 1**
   - ✅ Pro 1
   - ❌ Con 1

2. **Alternative 2** ⭐ CHOSEN
   - ✅ Pro 1
   - ✅ Pro 2

## Decision

Explain what you decided to do and why.

## Consequences

### Positive Consequences ✅

1. Benefit 1
2. Benefit 2

### Negative Consequences ❌

1. Drawback 1 (and mitigation strategy)
2. Drawback 2 (and mitigation strategy)

## References

- Link 1
- Link 2
```

### 3. Review with Team

- Share the ADR in a pull request
- Gather feedback from team members
- Update based on discussion
- Merge when consensus is reached

### 4. Update the Index

Add your ADR to the table in this README.

---

## Best Practices

### Writing ADRs

✅ **Be concise** - Focus on the decision, not implementation details
✅ **Explain trade-offs** - Include both pros and cons
✅ **Document alternatives** - Show what you considered
✅ **Include context** - Help future readers understand the situation
✅ **Link to resources** - Reference articles, docs, related ADRs
✅ **Update status** - Keep ADRs current (Deprecated, Superseded)

### Reviewing ADRs

✅ **Check completeness** - Is the context clear? Are consequences listed?
✅ **Validate alternatives** - Were reasonable options considered?
✅ **Assess reversibility** - How hard is it to undo this decision?
✅ **Consider timing** - Is now the right time for this decision?
✅ **Align with vision** - Does this support project goals?

---

## ADR Lifecycle

```
Proposed → Accepted → [Implemented] → [Evaluated]
                    ↓
                  Deprecated (if no longer valid)
                    ↓
                  Superseded (if replaced by newer ADR)
```

### Evaluation

After 3-6 months of implementation:
- Review the decision's impact
- Update the ADR with lessons learned
- Consider whether to continue, modify, or deprecate

---

## Related Documentation

- [CQRS Pattern](../patterns/CQRS_PATTERN.md) - Implementation of ADR-0001
- [Specification Pattern](../patterns/SPECIFICATION_PATTERN.md) - Implementation of ADR-0002
- [Hexagonal Architecture](../HEXAGONAL_ARCHITECTURE_REFACTORING_SUMMARY.md) - Overall architecture
- [Architectural Rules](../ARCHITECTURAL_RULES.md) - Enforced patterns

---

## References

- [Documenting Architecture Decisions - Michael Nygard](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
- [ADR GitHub Organization](https://adr.github.io/)
- [Architecture Decision Records - ThoughtWorks](https://www.thoughtworks.com/radar/techniques/lightweight-architecture-decision-records)

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
