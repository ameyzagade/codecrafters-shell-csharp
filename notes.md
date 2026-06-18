# CodeCrafters Shell – Learning Notes

## Goal

Build a simple shell that can:

* Execute built-in commands (`echo`, `pwd`, `cd`, `type`, `exit`)
* Execute external programs from PATH
* Parse quoted and escaped arguments

---

## Key Concepts Learned

### Command Dispatch

Use a dictionary to map command names to handlers:

* `Dictionary<string, Action<IReadOnlyList<string>>>`
* Cleaner than large `switch` statements.
* Easy to add new commands.

Lesson:
> When behavior depends on a string key, a dictionary is often simpler than a growing switch statement.

---

### Read-Only Interfaces

Use `IReadOnlyList<string>` when commands only consume arguments.

Lesson:
> Prefer the least powerful abstraction required by the caller.

---

### PATH Lookup

To execute an external command:

1. Check whether the user supplied a path (`./foo`, `/usr/bin/foo`).
2. Otherwise search directories listed in the PATH environment variable.
3. Return the first matching executable.

Lesson:
> Executable discovery is separate from process execution.

---

### Unix Executable Permissions

On Linux/macOS, file existence is not enough.

Need to verify execute permissions:
* UserExecute
* GroupExecute
* OtherExecute

Lesson:
> A file can exist but still not be executable.

---

### Directory Navigation

`cd` implementation needs to:

1. Expand `~` and `~/...`
2. Resolve relative paths
3. Validate directory existence
4. Change current directory

Lesson:
> Normalize paths first, validate once, then perform the operation.

---

### Argument Parsing

Most difficult part of the project.

Parser tracks:

* Single-quote state
* Double-quote state
* Escape state

Examples:

* `echo hello world`
* `echo "hello world"`
* `echo 'hello world'`
* `echo hello\ world`

Lesson:
> Parsing user input is often harder than executing the command.

---

## Refactoring Lessons

### Use the Right Collection

* Dictionary → command lookup + behavior
* HashSet → membership checks
* List → ordered data

Lesson:
> Choose collections based on intent, not habit.

---

### Remove Dead Code

Example:

* `action is null` after `TryGetValue()`

Lesson:
> If a branch can never execute, remove it.

---

### Avoid Premature Optimization

Examples removed:

* Unnecessary capacity estimates
* Tiny wrapper methods

Lesson:
> Optimize for readability first.


### Process Execution

External commands are executed using:

* `ProcessStartInfo`
* `Process.Start()`
* `Process.WaitForExit()`

Flow:

1. Resolve the executable path.
2. Create a `ProcessStartInfo`.
3. Populate `ArgumentList`.
4. Start the process.
5. Wait for completion.

Important observations:

* Command discovery and command execution are separate concerns.
* `UseShellExecute = false` allows direct process execution.
* Arguments should be passed via `ArgumentList` instead of manually building a command string.
* Process execution itself is relatively simple; finding the correct executable and parsing arguments is harder.

Lesson:
> Most of the complexity of a shell is input parsing and executable discovery, not launching the process.


---

## What Was Surprisingly Hard?

* Shell quoting rules
* Escape sequence handling
* Edge cases in argument parsing

---

## What I'd Improve Next Time

* Add parser test cases earlier.
* Separate tokenizer/parser from shell logic.
* Keep parsing logic isolated from console output.

---

## Biggest Takeaway

The challenge was not running programs.

The challenge was correctly interpreting what the user typed before running them.
