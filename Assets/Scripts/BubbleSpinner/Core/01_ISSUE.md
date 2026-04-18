### 🔧 CODE REFACTOR PROMPT — SAFE & CONTEXT-AWARE

You are reviewing my code for **refactoring only**, not rewriting.

## 🔒 Strict Rules

* Do NOT assume missing functionality.
* Do NOT invent context or dependencies.
* If something is unclear → ASK before suggesting changes.
* If additional scripts are required → request them first.
* Preserve existing behavior unless I explicitly allow changes.

## 🎯 Goals

Identify and improve:

* DRY violations (duplicate logic, repeated patterns)
* Dead or unused code
* Overly complex or redundant functions
* Beginner mistakes (unnecessary state, bad separation, poor naming if applicable)
* Opportunities to simplify without changing logic

## 🚫 Do NOT

* Do not redesign architecture unless clearly necessary
* Do not suggest patterns that require large rewrites
* Do not introduce new systems or abstractions unless justified
* Do not optimize prematurely

## 📌 Output Format

1. **Findings**

   * List duplicated patterns or issues
   * Be specific (function names, code sections)

2. **Risk Level**

   * Mark each as:

     * SAFE (no behavior change)
     * CAUTION (needs confirmation)
     * RISKY (could break logic)

3. **Refactor Suggestions**

   * Provide minimal, targeted improvements
   * Show BEFORE → AFTER code snippets
   * Keep changes small and isolated

4. **Clarification Needed (if any)**

   * Ask precise questions before touching uncertain logic

## 🧠 Extra Context

This is part of a system where:

* State consistency is important
* Execution order matters (don’t reorder logic casually)
* UI sync / callbacks may depend on exact timing