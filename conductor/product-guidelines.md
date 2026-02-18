# Aero Product Guidelines

## Brand Identity

### The WARP Stack

Aero is part of the **WARP stack**—a modern, high-performance technology combination for building .NET applications:

- **W**olverine - Message bus and mediator
- **A**spNet Core / **A**ero - Web framework and application accelerator
- **R**avenDB - Document database
- **P**react + HTMX - Frontend layer

Together, WARP delivers unparalleled developer velocity and application performance.

### Logo

The Aero logo is located at `assets/aero-logo.jpg` and should be prominently displayed in documentation, READMEs, and promotional materials.

---

## Tone & Voice

### Bold & Energetic

Aero speaks with confidence and excitement. We don't whisper—we announce.

- **Be punchy.** Short sentences. Strong verbs. No fluff.
- **Be inspiring.** Paint the picture of what's possible.
- **Be confident.** We know this works. We've battle-tested it.
- **Drive action.** Every message should move the developer forward.

**Examples:**

| Avoid | Use Instead |
|-------|-------------|
| "You might consider using Aero for your project..." | "Ship faster with Aero." |
| "Aero provides various features that could help..." | "Aero delivers authentication, caching, and persistence—ready to run." |
| "It is recommended to follow these patterns..." | "Build it right. Here's how." |

---

## Visual Identity

### Technical & Precise

Aero's aesthetic reflects its engineering roots. Code is front and center. Architecture matters.

- **Clean typography** optimized for code readability
- **Syntax highlighting** in all documentation examples
- **Architecture diagrams** that clearly show structure and flow
- **Precise language** that leaves no ambiguity

### Vibrant & Dynamic

But we're not boring. Aero moves fast, and our visuals should reflect that energy.

- **Bold accent colors** that draw attention to key information
- **Dynamic layouts** that guide the eye through content
- **Motion and energy** in presentations and promotional materials
- **Alive documentation** with interactive examples where possible

---

## Naming Conventions

### Descriptive & Explicit

Names in Aero should immediately communicate purpose. No guessing games.

**Principles:**

1. **Say what it does.** `CreateUserCommand` is better than `UserCmd`.
2. **Be specific.** `RavenDbRepository` is better than `DocumentRepository`.
3. **Avoid abbreviations.** `AuthenticationMiddleware` not `AuthMiddleware`.
4. **Follow .NET conventions.** PascalCase for public members, clear namespaces.

**Examples:**

| Avoid | Use Instead |
|-------|-------------|
| `IRepo<T>` | `IRepository<T>` |
| `AuthSvc` | `AuthenticationService` |
| `UsrMgmt` | `UserManagement` |
| `GetCmd()` | `GetCommand()` |

---

## Documentation Standards

### Code-First, Always

Developers want to see code. Lead with examples, explain after.

**Structure for feature documentation:**

1. **One-line pitch** - What does this feature do?
2. **Code example** - Show it in action immediately
3. **Explanation** - Now explain what's happening
4. **Configuration options** - Full reference
5. **Advanced scenarios** - Power user content

### Keep It Scannable

Developers scan, they don't read. Structure for the scanner:

- **Bold key terms** on first mention
- **Tables** for comparisons and options
- **Bullet lists** for features and steps
- **Code blocks** for everything technical

---

## Messaging Framework

### One-Liners

- "From zero to production in record time."
- "Stop reinventing the wheel. Start building your app."
- "The .NET application accelerator."

### Elevator Pitch

Aero is your launchpad for production-ready .NET applications. Whether you're building web, mobile, or desktop experiences, Aero eliminates the boilerplate grind so you can focus on what matters—shipping features. Part of the WARP stack.

### Value Propositions

- **Speed:** Weeks of setup → hours with Aero
- **Quality:** Battle-tested patterns from day one
- **Flexibility:** Modular architecture, use what you need
- **Scale:** Grows with your application, no rewrites needed
