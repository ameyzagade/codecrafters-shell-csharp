# Pattern: Solving Problems with State Machines

## When to Use This Pattern

Use a state machine when the meaning of an input depends on what happened before.

Ask:

> "Can the same input mean different things depending on context?"

If yes, think about modeling the problem with states.

---

## Mental Model

Instead of writing many nested `if` statements, define:

```text
Current State + Input -> Action + Next State
```

Every input is interpreted according to the current state.

---

## Example: Shell Argument Parser

While parsing command arguments, the same character can mean different things depending on context.

Example:

```text
"
```

can mean:

* Start a quoted string
* End a quoted string
* A literal quote character

depending on the current state.

---

## States Used

```text
Normal
SingleQuote
DoubleQuote
Escape
```

Implementation used:

```csharp
inSingleQuote
inDoubleQuote
isEscapeChar
```

These variables represent the parser's current state.

---

## Process for Building a State Machine

### Step 1: Identify states

Ask:

> "What contexts can I be in?"

Example:

```text
Normal text
Inside single quotes
Inside double quotes
Escaping next character
```

---

### Step 2: Identify transitions

Ask:

> "What inputs change my state?"

Example:

```text
"  -> enter/exit DoubleQuote
'  -> enter/exit SingleQuote
\  -> enter Escape
```

---

### Step 3: Process input based on state

For each character:

```text
Current State
      +
Current Character
      =
Action
      +
Next State
```

---

## Common Places Where This Appears

### Parsing

* Shell commands
* JSON
* CSV
* SQL
* Programming languages

### User Interfaces

```text
Loading
Loaded
Error
Submitting
Success
```

### Network Protocols

```text
Reading Header
Reading Body
Reading Trailer
```

### Games

```text
Idle
Walking
Jumping
Attacking
Dead
```

---

## Lessons Learned

1. The difficulty is usually not processing input.
   The difficulty is tracking context correctly.

2. State machines reduce complex nested conditions into explicit states.

3. When code becomes full of:

   * flags
   * nested conditionals
   * context-dependent behavior

   ask:

   > "Am I really implementing a state machine?"

4. Start by identifying states before writing code.

---

## Personal Takeaway

While building the shell parser, the hardest part was not parsing characters.

The hardest part was tracking how the meaning of a character changed based on previous input.

The solution was to model the parser as a state machine.

Future reminder:

> When behavior depends on context, identify the states first.
