# Pop

Pop is a small interpreted language implemented in this solution.

| Project | Purpose |
| --- | --- |
| `Language` | Lexer, parser, syntax tree, semantic model |
| `Runtime` | Interpreter and runtime behavior |
| `Pop` | Console host for running `.pop` scripts |
| `Tests` | Parser, semantic, and runtime coverage |

## Overview

| Area | Current behavior |
| --- | --- |
| Variables | Declared with `var` and inferred from the initializer |
| Types in source | Omitted for variables, parameters, and returns |
| Execution model | Syntax tree -> semantic model -> interpreter |
| Modules | Loaded with `inject "path"` |
| Exports | Controlled with `public` on `var` and `fun` |
| Built-ins | Available through the global `corn` module |

## Quick Start

Run a script file:

```text
dotnet run --project Pop\Pop.csproj -- Pop\Scripts\test.pop
```

Run inline source:

```text
dotnet run --project Pop\Pop.csproj -- "corn.println(1 + 2)"
```

Minimal example:

```text
public fun add(a, b) {
    ret a + b
}

var math -> inject "math.pop"
corn.println(math.add(10, 10))
```

## Files And Modules

A source file is a sequence of statements.

`inject` is an expression, not a statement. It executes another `.pop` file in isolated module scope and returns an object containing only that file's `public` variables and functions.

Example:

```text
var math -> inject "math.pop"
corn.println(math.add(1, 2))
```

Example exported module:

```text
public fun add(a, b) {
    ret a + b
}

fun hidden() {
    ret 0
}
```

`hidden` is not visible to the importing file.

## Comments

| Comment kind | Syntax | Notes |
| --- | --- | --- |
| Single-line comment | `// text` | Runs to the end of the current line |

Example:

```text
// initialize a value
var count -> 1
count -> count + 1 // increment it
```

## Statements

| Statement | Syntax | Notes |
| --- | --- | --- |
| Expression statement | `expr` | Any expression can stand alone |
| Variable declaration | `var name -> expr` | Type inferred from initializer |
| Public variable | `public var name -> expr` | Exported from injected file |
| Assignment | `name -> expr` | Right-associative, so `a -> b -> c` works |
| Return | `ret` or `ret expr` | Valid only in functions and lambdas |
| Continue | `cont` | Valid only in loops |
| Break | `abort` | Valid only in loops |
| While | `while condition { ... }` | Condition must be `bool` |
| If | `if condition { ... }` | Condition must be `bool` |
| Else if | `else if condition { ... }` | Chains with `if` |
| Else | `else { ... }` | Fallback branch |
| Function declaration | `fun name(a, b) { ... }` | No explicit parameter or return types |
| Public function | `public fun name(a, b) { ... }` | Exported from injected file |

Examples:

```text
var value -> 10
value -> value + 1

while true {
    if value == 2 {
        cont
    }

    if value == 3 {
        abort
    }
}
```

## Expressions

| Expression kind | Example |
| --- | --- |
| Integer literal | `123` |
| Double literal | `32.5` |
| String literal | `"hello"` |
| Char literal | `'x'` |
| Bool literal | `true`, `false` |
| Name | `value` |
| Parenthesized | `(a + b) * c` |
| Unary | `-value`, `+value`, `!flag`, `~bits` |
| Binary | `a + b`, `a * b`, `a == b`, `a && b` |
| Conditional | `condition ? whenTrue : whenFalse` |
| Call | `add(1, 2)` |
| Member access | `obj.name`, `text.len` |
| Object literal | `{ name: "bob" age: 32 }` |
| Array literal | `[1, 2, 3]` |
| Lambda | `@(x) { ret x + 1 }` |
| Inject | `inject "math.pop"` |

Supported binary operators:

```text
* / % + - << >> < <= > >= == != & ^ | && ||
```

## Semantic Model

The semantic layer binds the syntax tree into a typed bound tree.

| Area | Current behavior |
| --- | --- |
| `var` | Infers type from initializer |
| Function parameters | Bind as `any` |
| Lambda parameters | Bind as `any` |
| Function returns | Inferred from `ret` |
| Lambda returns | Inferred from `ret` |
| Object literals | Structural property types |
| Arrays | Infer a common element type when possible |
| Injected modules | Bound as object-like exported values |

Reported diagnostics include:

| Diagnostic category | Examples |
| --- | --- |
| Undefined names | Unknown variables/functions |
| Duplicate declarations | Same variable/function/property twice |
| Invalid member access | Missing members on a type |
| Invalid assignment | Incompatible assignment target/type |
| Invalid conditions | Non-`bool` conditions in `if`, `while`, `?:` |
| Invalid flow statements | `ret` outside functions/lambdas, `cont` or `abort` outside loops |

## Runtime

The runtime evaluates the semantic model directly.

| Supported behavior | Status |
| --- | --- |
| Variables and assignment | Yes |
| Functions and lambdas | Yes |
| Closures | Yes |
| `if`, `while`, `ret`, `cont`, `abort` | Yes |
| Objects and arrays | Yes |
| Module injection | Yes |
| Built-in `corn` module | Yes |

## Built-In Module

Every script starts with a global module named `corn`.

Examples:

```text
corn.print("hello")
corn.println(corn.type(1))
corn.println([1, 2, 3].len)
```

### Corn Functions

| Function | Description | Return |
| --- | --- | --- |
| `corn.print(value)` | Writes formatted output without a newline | `null` |
| `corn.println(value)` | Writes formatted output with a newline | `null` |
| `corn.type(value)` | Returns the runtime type name | `string` |
| `corn.str(value)` | Formats a value as text | `string` |
| `corn.int(value)` | Converts to integer when possible | `int` |
| `corn.double(value)` | Converts to double when possible | `double` |
| `corn.bool(value)` | Converts to bool | `bool` |
| `corn.input()` | Reads one input line | `string` |
| `corn.keys(obj)` | Returns object property names | `array` |
| `corn.has(obj, name)` | Checks whether an object contains a property | `bool` |
| `corn.clock()` | Returns UTC Unix time in seconds | `double` |
| `corn.read(path)` | Reads a file as text | `string` |
| `corn.write(path, text)` | Writes text to a file | `null` |

`corn.type(value)` currently returns names such as `int`, `double`, `bool`, `char`, `string`, `array`, `object`, `function`, and `null`.

## Built-In Member APIs

### Strings

| Member | Description | Return |
| --- | --- | --- |
| `text.len` | Number of characters | `int` |
| `text.at(index)` | Character at `index`, or `null` if out of range | `char` or `null` |
| `text.add(item)` | Returns a new string with the formatted item appended | `string` |
| `text.remove(index)` | Returns a new string with the character at `index` removed; out-of-range returns the original string | `string` |
| `text.forEach(@(character) { ... })` | Invokes the lambda once per character | `null` |

Example:

```text
var text -> "hello"
corn.println(text.at(1))
text.forEach(@(character) {
    corn.println(character)
})
text -> text.add("!")
text -> text.remove(1)
corn.println(text)
```

### Arrays

| Member | Description | Return |
| --- | --- | --- |
| `arr.len` | Number of elements | `int` |
| `arr.at(index)` | Element at `index`, or `null` if out of range | element type or `null` |
| `arr.add(item)` | Appends an item in place | `null` |
| `arr.remove(index)` | Removes and returns the item at `index`, or `null` if out of range | element type or `null` |
| `arr.forEach(@(elem) { ... })` | Invokes the lambda once per element | `null` |

Example:

```text
var arr -> [10, 20, 30]
arr.add(40)
corn.println(arr.remove(1))
arr.forEach(@(elem) {
    corn.println(elem)
})
```

### Objects

| Member | Description | Return |
| --- | --- | --- |
| `obj.len` | Number of members, unless the object already defines a real `len` property | `int` or property value |
| `obj.get(name)` | Gets a property by string key | property value or `null` |
| `obj.add(name, value)` | Adds or replaces a property in place | `null` |
| `obj.remove(name)` | Removes and returns a property value | property value or `null` |
| `obj.forEach(@(key, value) { ... })` | Invokes the lambda once per member pair | `null` |

Example:

```text
var obj -> { name: "bob" }
obj.add("age", 32)
corn.println(obj.get("age"))
obj.forEach(@(key, value) {
    corn.println(key)
    corn.println(value)
})
corn.println(obj.remove("name"))
```

## Notes And Limitations

| Area | Current limitation |
| --- | --- |
| Assignment syntax | Uses `->`, not `=` |
| Type syntax | No explicit source-level type annotations |
| Parameters | Always untyped in source |
| Built-ins | Only exposed through `corn` |
| Array indexing | `arr[index]` syntax is not implemented |
| Increment/decrement | `i++` and `i--` are not implemented |
| Inject | Expression-based only; no standalone `inject` statement form |
