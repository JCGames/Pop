# Pop

Pop is a small interpreted language implemented in this solution with four main parts:

- `Language`: lexer, parser, syntax tree, semantic model
- `Runtime`: interpreter and built-in runtime functions
- `Pop`: console host for running `.pop` scripts
- `Tests`: parser, semantic, and runtime coverage

This README describes the language as it exists today.

## Overview

Pop is currently:

- dynamically declared with `var`
- semantically bound with inferred types
- interpreted from the semantic model
- file-based, with module-style imports through `inject`

Types are not written in source code for variables, parameters, or function return values. The semantic layer infers what it can and falls back to `any` where appropriate.

## Source Files

A source file is a sequence of statements.

```text
public fun add(a, b) {
    ret a + b
}

var math -> inject "math.pop"
corn.print(math.add(10, 10))
```

## Statements

### Expression Statement

Any expression can be used as a statement.

```text
corn.print("hello")
obj.name
arr.forEach(@(elem) {
    corn.print(elem)
})
```

### Variable Declaration

Variables are declared with `var` and assigned with `->`.

```text
var a -> 32
var b -> 32.5
var message -> "hello"
var arr -> [1, 2, 3]
```

Public variables are exported from injected files:

```text
public var answer -> 42
```

### Assignment

Assignment also uses `->`.

```text
value -> 10
a -> b -> c
```

Assignment chains are right-associative.

### Return

Use `ret` inside functions and lambdas.

```text
ret
ret value
ret a + b
```

### Continue

Use `cont` inside loops.

```text
cont
```

### Break

Use `abort` inside loops.

```text
abort
```

### While

```text
while condition {
    if shouldSkip {
        cont
    }

    if shouldStop {
        abort
    }

    value -> value + 1
}
```

### If / Else / Else If

```text
if condition {
    value -> 1
}
else if otherCondition {
    value -> 2
}
else {
    value -> 3
}
```

### Function Declaration

Functions do not declare parameter types or return types.

```text
fun add(a, b) {
    ret a + b
}
```

Public functions are exported from injected files:

```text
public fun add(a, b) {
    ret a + b
}
```

## Expressions

### Literals

```text
123
32.5
"hello"
'x'
true
false
```

### Names

```text
value
myVariable
```

### Parenthesized Expressions

```text
(a + b) * c
```

### Unary Operators

```text
-value
+value
!flag
~bits
```

### Binary Operators

Supported operator families:

```text
a * b
a / b
a % b
a + b
a - b
a << b
a >> b
a < b
a <= b
a > b
a >= b
a == b
a != b
a & b
a ^ b
a | b
a && b
a || b
```

### Conditional Expression

```text
condition ? whenTrue : whenFalse
```

### Call Expression

```text
nameOfFunction(arg1, arg2)
getHandler()(value)
```

### Member Access

```text
obj.name
makePerson().name
```

### Object Literals

```text
{
    name: "bob"
    location: "france"
    age: 32
}
```

Example:

```text
var obj -> {
    name: "bob"
    location: "france"
    age: 32
}
```

### Array Literals

Elements can be any expression type.

```text
[elem1, elem2, elem3]
[1, "hello", obj.name, add(1, 2), { age: 32 }, @(x) { ret x }]
```

### Lambda Expressions

Lambdas do not declare parameter types or return types.

```text
@(param1, param2) {
    ret param1 + param2
}
```

Example:

```text
var transform -> @(value) {
    ret value + 1
}
```

### Inject Expression

`inject` is an expression, not a statement. It loads another `.pop` file, executes it in isolated module scope, and returns an object containing only its `public` variables and functions.

```text
var math -> inject "math.pop"
corn.print(math.add(10, 10))
```

## Modules And Visibility

Only `public` declarations are exposed to an injecting file.

`math.pop`:

```text
public fun add(a, b) {
    ret a + b
}

fun hidden() {
    ret 0
}
```

consumer:

```text
var math -> inject "math.pop"
corn.print(math.add(1, 2))
```

`math.hidden` is not available outside the file.

## Semantic Model

The semantic layer binds the syntax tree into a typed bound tree.

Current behavior:

- `var` infers its type from the initializer
- function parameters are currently `any`
- lambda parameters are currently `any`
- function and lambda return types are inferred from `ret`
- object literals have structural property types
- arrays infer a common element type when possible
- module exports are represented as object-like values

The semantic model also reports diagnostics for:

- undefined names
- duplicate declarations
- duplicate object properties
- invalid member access
- invalid assignment types
- invalid conditions in `if`, `while`, and `?:`
- invalid `ret` outside functions/lambdas
- invalid `cont` / `abort` outside loops

## Runtime Behavior

The runtime evaluates the semantic model directly.

Supported runtime features:

- variables and assignment
- functions and lambdas
- closures
- `if`, `while`, `ret`, `cont`, `abort`
- objects and arrays
- module injection through `inject`
- built-in functions

## Built-In Module

Every script starts with a built-in module named `corn` already available in global scope.

`corn` is created before user code runs and exposes the built-in runtime functions available to every script.

Examples:

```text
corn.print("hello")
corn.print(corn.type(1))
corn.print(corn.len([1, 2, 3]))
```

## Built-In Corn Functions

### `print(value)`

Writes a formatted value to the output writer.

### `len(value)`

Returns the length of:

- a string
- an array
- an object (property count)

### `type(value)`

Returns a string such as:

- `"int"`
- `"double"`
- `"bool"`
- `"char"`
- `"string"`
- `"array"`
- `"object"`
- `"function"`
- `"null"`

### `str(value)`

Formats a value as a string.

### `int(value)`

Converts a value to `int` when possible.

### `double(value)`

Converts a value to `double` when possible.

### `bool(value)`

Converts a value to `bool`.

### `input()`

Reads one line of input.

### `keys(obj)`

Returns an array of object property names.

### `has(obj, name)`

Returns whether an object contains a property.

### `push(array, value)`

Appends a value to an array.

### `pop(array)`

Removes and returns the last value from an array. Returns `null` for an empty array.

### `clock()`

Returns the current UTC Unix time as a `double` number of seconds.

### `read(path)`

Reads a file as text.

### `write(path, text)`

Writes text to a file.

## Built-In Array Members

Arrays expose native members through `.` access.

### `arr.len`

Returns the number of elements in the array.

```text
var arr -> [10, 20, 30]
corn.print(arr.len)
```

### `arr.at(index)`

Returns the element at the given index, or `null` if out of range.

```text
var arr -> [10, 20, 30]
corn.print(arr.at(1))
```

### `arr.forEach(@(elem) { ... })`

Invokes the given callable once for each element.

```text
var arr -> [10, 20, 30]
arr.forEach(@(elem) {
    corn.print(elem)
})
```

## Built-In Object Members

Objects expose native members through `.` access.

### `obj.get(propertyName)`

Returns the property value for the given string key, or `null` if the property does not exist.

```text
var obj -> {
    name: "bob"
    age: 32
}

corn.print(obj.get("name"))
corn.print(obj.get("missing"))
```

## Notes And Current Limitations

- Assignment and declaration use `->`, not `=`.
- Functions do not declare explicit types.
- Parameters are untyped in source and currently bind as `any`.
- Built-in runtime functions are available through the global `corn` module.
- Arrays support `.len`, `.at(index)`, and `.forEach(...)`, but do not support `arr[index]` syntax.
- Increment/decrement operators like `i++` are not implemented.
- Objects support `.get(propertyName)` for string-based property access.
- `inject` is module-style and expression-based; it is no longer a standalone statement form.

## Running Scripts

Run the console host with a `.pop` file:

```text
dotnet run --project Pop\Pop.csproj -- Pop\Scripts\test.pop
```

Or pass inline source:

```text
dotnet run --project Pop\Pop.csproj -- "corn.print(1 + 2)"
```
