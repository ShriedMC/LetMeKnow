# Agent Instructions Reference

Add this section to your agent configuration (`CLAUDE.md`, `.cursorrules`, `.windsurfrules`, or workspace instructions):

```markdown
### Human Desktop Alerts
When you need user input, credentials, clarification, or human review:
- Run: lmk "<YourAgentName>" "<What you need>"
- Example: lmk "Claude" "Need STRIPE_SECRET_KEY in .env to run tests"

For critical errors or blockers requiring immediate attention:
- Run: lmk --urgent "<YourAgentName>" "<Error summary>"
- Example: lmk --urgent "Cursor" "Build failed on line 42 in auth.py"
```

### CLI Reference

```bash
# Standard alert:
lmk <WhoYouAre> <WhatYouNeed> [<OptionalDetails>]

# High priority / urgent alert:
lmk --urgent <WhoYouAre> <WhatYouNeed> [<OptionalDetails>]
```
