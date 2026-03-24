# Pop

Pop is a small interpreted scripting language for simple programs, reusable modules, file access, and math-heavy utility scripts.

This repository contains:

- the Pop language implementation
- the console runner
- built-in runtime modules
- a small standard library written in Pop under `Pop\lib`

## Getting Started

Run a script file:

```text
dotnet run --project Pop\Pop.csproj -- Pop\test.pop
```

Run inline source:

```text
dotnet run --project Pop\Pop.csproj -- "corn.println(1 + 2)"
```

Minimal example:

```text
var math -> inject "lib/math.pop"
corn.println(math.clamp(12, 0, 5))
```

## Language Overview

Pop source files are sequences of statements.

### Comments

Single-line comments start with `//`.

```text
// this is a comment
var count -> 1
```

### Variables

Variables are declared with `var`.

```text
var name -> "Pop"
var count -> 1
```

Assignments use `->`.

```text
count -> count + 1
```

Public variables are exported from modules:

```text
public var version -> "1.0"
```

### Functions

Functions are declared with `fun`.

```text
fun add(left, right) {
    ret left + right
}

corn.println(add(2, 3))
```

Public functions are exported from modules:

```text
public fun double(value) {
    ret value * 2
}
```

Return uses `ret` and may be bare:

```text
ret
ret 42
```

### Control Flow

`if`, `else if`, `else`, and `while` are supported.

```text
while true {
    if ready {
        break
    }

    if shouldSkip {
        skip
    }
}
```

- `skip` continues the current loop
- `break` exits the current loop

### Lambdas

Lambdas use `@(...) { ... }`.

```text
var square -> @(value) {
    ret value * value
}

corn.println(square(4))
```

### Modules

Use `inject "path"` to load another `.pop` file. `inject` is an expression and returns an object containing only that file's public exports.

```text
var math -> inject "lib/math.pop"
corn.println(math.max(10, 4))
```

Example module:

```text
public fun add(a, b) {
    ret a + b
}

fun hidden() {
    ret 0
}
```

Only `add` is visible to the importer.

Injected modules are cached at the parse/bind level, so repeated injects avoid recompiling the same file path. Each inject still executes the module and returns fresh runtime exports.

## Values

Pop currently supports these value kinds:

- integers: `123`
- doubles: `32.5`
- strings: `"hello"`
- chars: `'x'`
- bools: `true`, `false`
- nil: `nil`
- objects: `{ name: "Ada" age: 32 }`
- arrays: `[1, 2, 3]`
- functions
- error objects

### Objects

Object literals use `{ ... }`.

```text
var user -> { name: "Ada" age: 32 }
corn.println(user.name)
```

Object member assignment is supported:

```text
user.age -> 33
user.level -> 5
```

### Arrays

Array literals use `[ ... ]`.

```text
var numbers -> [1, 2, 3]
numbers.add(4)
corn.println(numbers.len)
```

## Expressions And Operators

### Unary Operators

```text
+value
-value
!flag
~bits
```

### Postfix Update

```text
value++
value--
obj.count++
obj.count--
```

`++` and `--` are supported on numeric variables and object members.

### Binary Operators

```text
* / % + - << >> < <= > >= == != & ^ | && ||
```

### Conditional Expression

```text
condition ? whenTrue : whenFalse
```

### Member Access

```text
obj.name
corn.fs.cwd()
corn.math.sqrt(9)
```

## Built-In Value APIs

### Strings

- `text.len`
- `text.at(index)`
- `text.add(item)`
- `text.contains(item)`
- `text.insert(index, item)`
- `text.replace(index, item)`
- `text.remove(index)`
- `text.forEach(@(character) { ... })`

Example:

```text
var text -> "hello"
corn.println(text.len)
corn.println(text.at(1))
text -> text.add("!")
```

### Arrays

- `arr.len`
- `arr.at(index)`
- `arr.add(item)`
- `arr.insert(index, item)`
- `arr.replace(index, item)`
- `arr.remove(index)`
- `arr.forEach(@(elem) { ... })`

Example:

```text
var arr -> [10, 20, 30]
arr.add(40)
corn.println(arr.at(1))
```

### Objects

- `obj.len`
- `obj.get(name)`
- `obj.add(name, value)`
- `obj.remove(name)`
- `obj.forEach(@(key, value) { ... })`

Example:

```text
var obj -> { name: "bob" }
obj.add("age", 32)
corn.println(obj.get("age"))
```

### Numbers

- `number.min`
- `number.max`

These return the runtime min/max for the current numeric type.

## Errors As Values

Pop runtime failures are represented as ordinary error objects instead of crashing the host process.

Example:

```text
var value -> corn.int("hello")

if corn.isError(value) {
    corn.println(value.code)
    corn.println(value.message)
}
```

Error objects include at least:

- `code`
- `message`

You can also create one directly:

```text
var err -> corn.error("bad_input", "Input was invalid.")
```

## Built-In Modules

Every script starts with a global `corn` module.

### `corn`

General built-ins:

- `corn.print(value)`
- `corn.println(value)`
- `corn.type(value)`
- `corn.str(value)`
- `corn.int(value)`
- `corn.double(value)`
- `corn.bool(value)`
- `corn.isError(value)`
- `corn.error(code, message)`
- `corn.input()`
- `corn.keys(obj)`
- `corn.has(obj, name)`
- `corn.clock()`

`corn.type(value)` currently returns:

- `int`
- `double`
- `bool`
- `char`
- `string`
- `array`
- `object`
- `function`
- `error`
- `nil`

### `corn.fs`

File and directory helpers:

- `corn.fs.read(path)`
- `corn.fs.write(path, text)`
- `corn.fs.exists(path)`
- `corn.fs.info(path)`
- `corn.fs.list(path)`
- `corn.fs.cwd()`

`corn.fs.info(path)` returns an object with fields such as:

- `path`
- `name`
- `exists`
- `isFile`
- `isDir`
- `size`
- `created`
- `modified`

Example:

```text
var info -> corn.fs.info("notes.txt")
if info.exists {
    corn.println(info.size)
}
```

### `corn.math`

Constants:

- `corn.math.pi`
- `corn.math.tau`
- `corn.math.e`

Functions:

- `corn.math.abs(value)`
- `corn.math.min(left, right)`
- `corn.math.max(left, right)`
- `corn.math.clamp(value, min, max)`
- `corn.math.sqrt(value)`
- `corn.math.pow(value, power)`
- `corn.math.sin(value)`
- `corn.math.cos(value)`
- `corn.math.tan(value)`
- `corn.math.asin(value)`
- `corn.math.acos(value)`
- `corn.math.atan(value)`
- `corn.math.atan2(y, x)`
- `corn.math.log(value)`
- `corn.math.log10(value)`
- `corn.math.log2(value)`
- `corn.math.exp(value)`
- `corn.math.floor(value)`
- `corn.math.ceil(value)`
- `corn.math.round(value)`
- `corn.math.trunc(value)`

Invalid math domains return error objects.

Example:

```text
var value -> corn.math.sqrt(-1)
corn.println(corn.isError(value))
```

## Shipped Library Modules

This repository also ships Pop-written library modules under `Pop\lib`.

### `lib/math.pop`

Higher-level math helpers built on `corn.math`.

Exports:

- constants: `pi`, `tau`, `e`
- `min`, `max`, `clamp`, `clamp01`
- `abs`, `sign`, `lerp`
- `sqrt`, `pow`
- `sin`, `cos`, `tan`
- `asin`, `acos`, `atan`, `atan2`
- `log`, `log10`, `log2`, `exp`
- `floor`, `ceil`, `round`, `trunc`
- `degToRad`, `radToDeg`
- `isEven`, `isOdd`

Example:

```text
var math -> inject "lib/math.pop"
corn.println(math.lerp(10, 20, 0.25))
corn.println(math.radToDeg(math.pi))
```

### `lib/errors.pop`

Helpers for error values.

Exports:

- `make(code, message)`
- `is(value)`
- `code(value)`
- `message(value)`

Example:

```text
var errors -> inject "lib/errors.pop"
var err -> errors.make("bad_input", "Nope")
corn.println(errors.code(err))
```

### `lib/fs.pop`

Convenience wrappers over `corn.fs`.

Exports:

- `read(path)`
- `write(path, text)`
- `exists(path)`
- `info(path)`
- `list(path)`
- `cwd()`

Example:

```text
var fs -> inject "lib/fs.pop"
corn.println(fs.cwd())
```

## Example Script

`Pop\test.pop` demonstrates using the shipped modules:

- `lib/math.pop`
- `lib/errors.pop`
- `lib/fs.pop`

## Current Limits

Current notable limitations:

- assignment uses `->`, not `=`
- there are no source-level type annotations
- `arr[index]` syntax is not supported
- `inject` is an expression, not a standalone statement
