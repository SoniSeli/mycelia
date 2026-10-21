# ADR-0001: Record architecture decisions

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-10-21 |
| **Deciders** | Soni Seli |

## Context

Mycelia is a solo project intended to show how I design and build a mobile game. Much of the value is in *why* the code looks the way it does, and that reasoning disappears from commit history and is easy to forget by interview time.

## Options considered

### Option A: Code comments and commit messages only
- **Pros:** No extra files.
- **Cons:** Rejected alternatives and trade-offs are lost; reasoning is scattered.

### Option B: Lightweight Architecture Decision Records (ADRs)
- **Pros:** One short, dated file per significant decision; easy to review and link from the README.
- **Cons:** Small writing overhead per decision.

## Decision

We will record significant decisions as ADRs in `docs/adr/`, using [0000-template.md](0000-template.md). A decision is significant if it is costly to reverse, affects more than one system, or is one an experienced reviewer would likely question.

## Consequences

- **Positive:** Trade-offs are explicit and reviewable; the ADRs double as interview preparation.
- **Negative:** Each decision takes 10–15 minutes to write up.
- **Follow-up:** Accepted ADRs are never edited to change the decision. A new ADR supersedes the old one, which keeps the history honest.
