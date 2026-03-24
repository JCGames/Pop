# Pop Built-Ins

This file documents the built-in `corn` module and its nested built-in modules.

## `corn`

General built-ins:

- `corn.print(value)`
  Writes `value` without a trailing newline.
- `corn.println(value)`
  Writes `value` with a trailing newline.
- `corn.type(value)`
  Returns the Pop type name as a string.
- `corn.str(value)`
  Converts a value to its string form.
- `corn.int(value)`
  Converts a value to an int. Returns an error object on failure.
- `corn.double(value)`
  Converts a value to a double. Returns an error object on failure.
- `corn.bool(value)`
  Converts a value to a bool.
- `corn.isError(value)`
  Returns `true` if `value` is an error object.
- `corn.error(code, message)`
  Creates an error object.
- `corn.input()`
  Reads one line from standard input.
- `corn.keys(value)`
  Returns the keys of an object as an array.
- `corn.has(value, name)`
  Returns whether an object contains a property.
- `corn.clock()`
  Returns the current UTC Unix time in seconds as a double.

## `corn.fs`

Path helpers:

- `corn.fs.join(left, right)`
- `corn.fs.name(path)`
- `corn.fs.stem(path)`
- `corn.fs.ext(path)`
- `corn.fs.parent(path)`
- `corn.fs.absolute(path)`

Checks:

- `corn.fs.exists(path)`
- `corn.fs.isFile(path)`
- `corn.fs.isDir(path)`

Metadata:

- `corn.fs.info(path)`
  Returns an object with fields including `path`, `name`, `exists`, `isFile`, `isDir`, `size`, `created`, and `modified`.
- `corn.fs.size(path)`
  Returns file size in bytes. Returns an error object for missing or non-file paths.
- `corn.fs.created(path)`
  Returns the creation time as Unix seconds.
- `corn.fs.modified(path)`
  Returns the last modified time as Unix seconds.

File operations:

- `corn.fs.read(path)`
- `corn.fs.write(path, text)`
- `corn.fs.append(path, text)`
- `corn.fs.copy(source, destination)`
- `corn.fs.move(source, destination)`
- `corn.fs.remove(path)`

Directory operations:

- `corn.fs.list(path)`
  Returns file and directory names.
- `corn.fs.files(path)`
  Returns file names only.
- `corn.fs.dirs(path)`
  Returns directory names only.
- `corn.fs.mkdir(path)`
- `corn.fs.cwd()`
- `corn.fs.chdir(path)`

Failures return error objects.

## `corn.math`

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

Invalid numeric domains return error objects.

## `corn.json`

- `corn.json.parse(text)`
  Parses JSON into Pop values. Invalid JSON returns an error object.
- `corn.json.stringify(value)`
  Serializes a Pop value to compact JSON.
- `corn.json.pretty(value)`
  Serializes a Pop value to indented JSON.

Supported JSON value mapping:

- object -> Pop object
- array -> Pop array
- string -> Pop string
- number -> Pop int or double
- boolean -> Pop bool
- `null` -> `nil`

`stringify` and `pretty` support `nil`, `bool`, `int`, `double`, `char`, `string`, arrays, and objects.

## `corn.http`

Synchronous HTTP helpers:

- `corn.http.get(url)`
- `corn.http.post(url, body)`
- `corn.http.put(url, body)`
- `corn.http.delete(url)`
- `corn.http.request(method, url, body, headers)`

HTTP responses are returned as Pop objects with fields such as:

- `ok`
- `status`
- `reason`
- `body`
- `headers`
- `url`
- `method`

`headers` is a Pop object keyed by lower-case header name.

Request failures return error objects.
