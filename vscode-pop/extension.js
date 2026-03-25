const vscode = require("vscode");
const cp = require("child_process");
const fs = require("fs");
const path = require("path");

const diagnosticTimers = new Map();
const diagnosticVersions = new Map();
let analyzerClient = null;
let outputChannel = null;

const KEYWORDS = {
    public: { kind: "keyword", detail: "keyword", documentation: "Marks a global variable or function as exported from a module." },
    fun: { kind: "keyword", detail: "keyword", documentation: "Declares a function." },
    var: { kind: "keyword", detail: "keyword", documentation: "Declares a variable." },
    ret: { kind: "keyword", detail: "keyword", documentation: "Returns from the current function or lambda." },
    if: { kind: "keyword", detail: "keyword", documentation: "Conditional branch." },
    else: { kind: "keyword", detail: "keyword", documentation: "Fallback branch for an if statement." },
    while: { kind: "keyword", detail: "keyword", documentation: "Loop while a condition is true." },
    skip: { kind: "keyword", detail: "keyword", documentation: "Continue to the next loop iteration." },
    break: { kind: "keyword", detail: "keyword", documentation: "Exit the current loop." },
    inject: { kind: "keyword", detail: "keyword", documentation: "Loads another Pop module and returns its exports." },
    true: { kind: "constant", detail: "bool", documentation: "Boolean true." },
    false: { kind: "constant", detail: "bool", documentation: "Boolean false." },
    nil: { kind: "constant", detail: "nil", documentation: "The Pop null value." }
};

const ROOT_SYMBOLS = {
    corn: moduleEntry("corn", "Built-in root module.")
};

const CORE_LIBS = {
    math: {
        detail: "core library",
        documentation: "Exports helpers from `Pop/lib/math.pop`.",
        members: {
            pi: constant("pi", "double", "The circle constant pi."),
            tau: constant("tau", "double", "Two times pi."),
            e: constant("e", "double", "Euler's number."),
            min: fn("min(left, right)", ["left", "right"], "Returns the smaller numeric value."),
            max: fn("max(left, right)", ["left", "right"], "Returns the larger numeric value."),
            clamp: fn("clamp(value, min, max)", ["value", "min", "max"], "Clamps a value to a numeric range."),
            clamp01: fn("clamp01(value)", ["value"], "Clamps a value to the 0..1 range."),
            abs: fn("abs(value)", ["value"], "Returns the absolute value."),
            sign: fn("sign(value)", ["value"], "Returns -1, 0, or 1."),
            lerp: fn("lerp(start, end, amount)", ["start", "end", "amount"], "Linear interpolation."),
            sqrt: fn("sqrt(value)", ["value"], "Square root."),
            pow: fn("pow(value, power)", ["value", "power"], "Raise a number to a power."),
            sin: fn("sin(value)", ["value"], "Sine."),
            cos: fn("cos(value)", ["value"], "Cosine."),
            tan: fn("tan(value)", ["value"], "Tangent."),
            asin: fn("asin(value)", ["value"], "Inverse sine."),
            acos: fn("acos(value)", ["value"], "Inverse cosine."),
            atan: fn("atan(value)", ["value"], "Inverse tangent."),
            atan2: fn("atan2(y, x)", ["y", "x"], "Two-argument inverse tangent."),
            log: fn("log(value)", ["value"], "Natural logarithm."),
            log10: fn("log10(value)", ["value"], "Base-10 logarithm."),
            log2: fn("log2(value)", ["value"], "Base-2 logarithm."),
            exp: fn("exp(value)", ["value"], "Exponential function."),
            floor: fn("floor(value)", ["value"], "Rounds down."),
            ceil: fn("ceil(value)", ["value"], "Rounds up."),
            round: fn("round(value)", ["value"], "Rounds to the nearest integer."),
            trunc: fn("trunc(value)", ["value"], "Truncates the fractional portion."),
            degToRad: fn("degToRad(value)", ["value"], "Converts degrees to radians."),
            radToDeg: fn("radToDeg(value)", ["value"], "Converts radians to degrees."),
            isEven: fn("isEven(value)", ["value"], "Returns true for even integers."),
            isOdd: fn("isOdd(value)", ["value"], "Returns true for odd integers.")
        }
    },
    fs: {
        detail: "core library",
        documentation: "Exports wrappers around `corn.fs`.",
        members: {}
    },
    errors: {
        detail: "core library",
        documentation: "Exports helpers around Pop error objects.",
        members: {
            make: fn("make(code, message)", ["code", "message"], "Creates an error object."),
            is: fn("is(value)", ["value"], "Returns true when a value is an error object."),
            code: fn("code(value)", ["value"], "Returns the error code or nil."),
            message: fn("message(value)", ["value"], "Returns the error message or nil.")
        }
    }
};

const BUILT_INS = {
    root: {
        print: fn("print(value)", ["value"], "Writes a value without a trailing newline."),
        println: fn("println(value)", ["value"], "Writes a value with a trailing newline."),
        type: fn("type(value)", ["value"], "Returns the Pop type name as a string."),
        str: fn("str(value)", ["value"], "Formats a value as a string."),
        int: fn("int(value)", ["value"], "Converts a value to an int. Returns an error object on failure."),
        double: fn("double(value)", ["value"], "Converts a value to a double. Returns an error object on failure."),
        bool: fn("bool(value)", ["value"], "Converts a value to a bool."),
        isError: fn("isError(value)", ["value"], "Returns true when the value is a Pop error object."),
        error: fn("error(code, message)", ["code", "message"], "Creates a Pop error object."),
        input: fn("input()", [], "Reads one line from standard input."),
        keys: fn("keys(value)", ["value"], "Returns the keys of an object."),
        has: fn("has(value, name)", ["value", "name"], "Returns whether an object contains a property."),
        clock: fn("clock()", [], "Returns the current UTC Unix time in seconds as a double."),
        fs: moduleEntry("fs", "Filesystem helpers."),
        math: moduleEntry("math", "Math helpers."),
        json: moduleEntry("json", "JSON helpers."),
        http: moduleEntry("http", "HTTP helpers.")
    },
    modules: {
        fs: {
            read: fn("read(path)", ["path"], "Reads a text file."),
            write: fn("write(path, text)", ["path", "text"], "Writes a text file."),
            append: fn("append(path, text)", ["path", "text"], "Appends text to a file."),
            copy: fn("copy(source, destination)", ["source", "destination"], "Copies a file."),
            move: fn("move(source, destination)", ["source", "destination"], "Moves or renames a file or directory."),
            remove: fn("remove(path)", ["path"], "Removes a file or an empty directory."),
            exists: fn("exists(path)", ["path"], "Returns true when a path exists."),
            isFile: fn("isFile(path)", ["path"], "Returns true when a path is a file."),
            isDir: fn("isDir(path)", ["path"], "Returns true when a path is a directory."),
            info: fn("info(path)", ["path"], "Returns a metadata object for a file or directory."),
            size: fn("size(path)", ["path"], "Returns the size of a file in bytes."),
            modified: fn("modified(path)", ["path"], "Returns the last modified time as Unix seconds."),
            created: fn("created(path)", ["path"], "Returns the creation time as Unix seconds."),
            list: fn("list(path)", ["path"], "Lists file and directory names in a directory."),
            files: fn("files(path)", ["path"], "Lists file names in a directory."),
            dirs: fn("dirs(path)", ["path"], "Lists directory names in a directory."),
            mkdir: fn("mkdir(path)", ["path"], "Creates a directory and any missing parents."),
            cwd: fn("cwd()", [], "Returns the current working directory."),
            chdir: fn("chdir(path)", ["path"], "Changes the current working directory."),
            join: fn("join(left, right)", ["left", "right"], "Joins two path segments."),
            name: fn("name(path)", ["path"], "Returns the file or directory name."),
            stem: fn("stem(path)", ["path"], "Returns the file name without its extension."),
            ext: fn("ext(path)", ["path"], "Returns the path extension."),
            parent: fn("parent(path)", ["path"], "Returns the parent directory path."),
            absolute: fn("absolute(path)", ["path"], "Returns the absolute normalized path.")
        },
        math: {
            pi: constant("pi", "double", "The circle constant pi."),
            tau: constant("tau", "double", "Two times pi."),
            e: constant("e", "double", "Euler's number."),
            abs: fn("abs(value)", ["value"], "Returns the absolute value."),
            min: fn("min(left, right)", ["left", "right"], "Returns the smaller numeric value."),
            max: fn("max(left, right)", ["left", "right"], "Returns the larger numeric value."),
            clamp: fn("clamp(value, min, max)", ["value", "min", "max"], "Clamps a value to a numeric range."),
            sqrt: fn("sqrt(value)", ["value"], "Square root."),
            pow: fn("pow(value, power)", ["value", "power"], "Raise a number to a power."),
            sin: fn("sin(value)", ["value"], "Sine."),
            cos: fn("cos(value)", ["value"], "Cosine."),
            tan: fn("tan(value)", ["value"], "Tangent."),
            asin: fn("asin(value)", ["value"], "Inverse sine."),
            acos: fn("acos(value)", ["value"], "Inverse cosine."),
            atan: fn("atan(value)", ["value"], "Inverse tangent."),
            atan2: fn("atan2(y, x)", ["y", "x"], "Two-argument inverse tangent."),
            log: fn("log(value)", ["value"], "Natural logarithm."),
            log10: fn("log10(value)", ["value"], "Base-10 logarithm."),
            log2: fn("log2(value)", ["value"], "Base-2 logarithm."),
            exp: fn("exp(value)", ["value"], "Exponential function."),
            floor: fn("floor(value)", ["value"], "Rounds down."),
            ceil: fn("ceil(value)", ["value"], "Rounds up."),
            round: fn("round(value)", ["value"], "Rounds to the nearest integer."),
            trunc: fn("trunc(value)", ["value"], "Truncates the fractional portion.")
        },
        json: {
            parse: fn("parse(text)", ["text"], "Parses JSON into Pop values."),
            stringify: fn("stringify(value)", ["value"], "Serializes a value to compact JSON."),
            pretty: fn("pretty(value)", ["value"], "Serializes a value to indented JSON.")
        },
        http: {
            get: fn("get(url)", ["url"], "Sends a synchronous HTTP GET request."),
            post: fn("post(url, body)", ["url", "body"], "Sends a synchronous HTTP POST request."),
            put: fn("put(url, body)", ["url", "body"], "Sends a synchronous HTTP PUT request."),
            delete: fn("delete(url)", ["url"], "Sends a synchronous HTTP DELETE request."),
            request: fn("request(method, url, body, headers)", ["method", "url", "body", "headers"], "Sends a synchronous HTTP request and returns a response object.")
        }
    }
};

CORE_LIBS.fs.members = { ...BUILT_INS.modules.fs };

function activate(context) {
    const selector = { language: "pop" };
    const diagnostics = vscode.languages.createDiagnosticCollection("pop");
    outputChannel = vscode.window.createOutputChannel("Pop Language");
    log("Extension activated.");

    context.subscriptions.push(
        vscode.languages.registerCompletionItemProvider(selector, createCompletionProvider(), ".", ...completionTriggerCharacters()),
        vscode.languages.registerHoverProvider(selector, createHoverProvider()),
        vscode.languages.registerDefinitionProvider(selector, createDefinitionProvider()),
        vscode.languages.registerDocumentSymbolProvider(selector, createDocumentSymbolProvider()),
        vscode.languages.registerSignatureHelpProvider(selector, createSignatureHelpProvider(), "(", ","),
        diagnostics,
        outputChannel,
        vscode.workspace.onDidOpenTextDocument((document) => refreshDiagnostics(document, diagnostics)),
        vscode.workspace.onDidChangeTextDocument((event) => refreshDiagnostics(event.document, diagnostics)),
        vscode.workspace.onDidCloseTextDocument((document) => diagnostics.delete(document.uri)),
        {
            dispose() {
                if (analyzerClient) {
                    analyzerClient.dispose();
                    analyzerClient = null;
                }
            }
        }
    );

    for (const document of vscode.workspace.textDocuments) {
        refreshDiagnostics(document, diagnostics);
    }
}

function deactivate() {}

function log(message) {
    if (outputChannel) {
        outputChannel.appendLine(`[Pop] ${message}`);
    }
}

function createCompletionProvider() {
    return {
        provideCompletionItems(document, position) {
            const prefix = getLinePrefix(document, position);
            const completions = [];
            const fallbackContext = getStaticMemberCompletionContext(prefix);

            let scan;
            try {
                scan = scanDocument(document);
            } catch {
                scan = { variables: new Map(), functions: new Map(), diagnostics: [] };
            }

            const memberContext = getMemberCompletionContext(prefix, scan) || fallbackContext;

            if (memberContext) {
                const replaceRange = document.getWordRangeAtPosition(position, /[A-Za-z_][A-Za-z0-9_]*/) ?? new vscode.Range(position, position);
                addEntries(completions, memberContext.entries, memberContext.qualifier, replaceRange);
                return completions;
            }

            const replaceRange = document.getWordRangeAtPosition(position, /[A-Za-z_][A-Za-z0-9_]*/) ?? new vscode.Range(position, position);
            addEntries(completions, KEYWORDS, "", replaceRange);
            addEntries(completions, ROOT_SYMBOLS, "", replaceRange);
            addScannedSymbols(completions, scan);
            return dedupeCompletionItems(completions);
        }
    };
}

function createHoverProvider() {
    return {
        provideHover(document, position) {
            const range = document.getWordRangeAtPosition(position, /[A-Za-z_][A-Za-z0-9_]*/);
            if (!range) {
                return null;
            }

            const scan = scanDocument(document);
            const word = document.getText(range);
            const linePrefix = document.lineAt(position.line).text.slice(0, range.start.character + word.length);
            const memberTarget = getMemberHoverTarget(linePrefix, scan);

            if (memberTarget && memberTarget.entries[word]) {
                return createEntryHover(word, memberTarget.entries[word], memberTarget.qualifier);
            }

            if (KEYWORDS[word]) {
                return createEntryHover(word, KEYWORDS[word]);
            }

            if (ROOT_SYMBOLS[word]) {
                return createEntryHover(word, ROOT_SYMBOLS[word]);
            }

            if (BUILT_INS.root[word]) {
                return createEntryHover(word, BUILT_INS.root[word], "corn");
            }

            const symbol = findScannedSymbol(scan, word);
            if (symbol) {
                return new vscode.Hover(new vscode.MarkdownString(renderSymbolMarkdown(symbol)));
            }

            return null;
        }
    };
}

function createDefinitionProvider() {
    return {
        provideDefinition(document, position) {
            const range = document.getWordRangeAtPosition(position, /[A-Za-z_][A-Za-z0-9_]*/);
            if (!range) {
                return null;
            }

            const scan = scanDocument(document);
            const name = document.getText(range);
            const symbol = findScannedSymbol(scan, name);
            if (!symbol) {
                return null;
            }

            return new vscode.Location(document.uri, symbol.range);
        }
    };
}

function createDocumentSymbolProvider() {
    return {
        provideDocumentSymbols(document) {
            const scan = scanDocument(document);
            const symbols = [];

            for (const fnSymbol of scan.functions.values()) {
                symbols.push(new vscode.DocumentSymbol(
                    fnSymbol.name,
                    fnSymbol.signature,
                    vscode.SymbolKind.Function,
                    fnSymbol.range,
                    fnSymbol.range
                ));
            }

            for (const variableSymbol of scan.variables.values()) {
                symbols.push(new vscode.DocumentSymbol(
                    variableSymbol.name,
                    variableSymbol.kind === "module" ? "module" : "variable",
                    variableSymbol.kind === "module" ? vscode.SymbolKind.Module : vscode.SymbolKind.Variable,
                    variableSymbol.range,
                    variableSymbol.range
                ));
            }

            return symbols.sort((left, right) => left.range.start.compareTo(right.range.start));
        }
    };
}

function createSignatureHelpProvider() {
    return {
        provideSignatureHelp(document, position) {
            const scan = scanDocument(document);
            const context = getCallContext(document, position);
            if (!context) {
                return null;
            }

            const signatureEntry = resolveSignature(scan, context.target);
            if (!signatureEntry || signatureEntry.kind !== "function") {
                return null;
            }

            const information = new vscode.SignatureInformation(signatureEntry.signature, signatureEntry.documentation);
            information.parameters = signatureEntry.params.map((param) => new vscode.ParameterInformation(param));

            const help = new vscode.SignatureHelp();
            help.signatures = [information];
            help.activeSignature = 0;
            help.activeParameter = Math.min(context.argumentIndex, Math.max(signatureEntry.params.length - 1, 0));
            return help;
        }
    };
}

function completionTriggerCharacters() {
    return "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_".split("");
}

function refreshDiagnostics(document, diagnostics) {
    if (document.languageId !== "pop") {
        return;
    }

    const key = document.uri.toString();
    if (diagnosticTimers.has(key)) {
        clearTimeout(diagnosticTimers.get(key));
    }

    const version = document.version;
    diagnosticVersions.set(key, version);

    const timer = setTimeout(async () => {
        diagnosticTimers.delete(key);
        if (diagnosticVersions.get(key) !== version) {
            return;
        }

        let inferredDiagnostics = [];
        try {
            const scan = scanDocument(document);
            inferredDiagnostics = scan.diagnostics;
        } catch {
            inferredDiagnostics = [];
        }

        const compilerDiagnostics = await getCompilerDiagnostics(document);
        if (diagnosticVersions.get(key) !== version) {
            return;
        }

        diagnostics.set(document.uri, compilerDiagnostics ?? inferredDiagnostics);
    }, 250);

    diagnosticTimers.set(key, timer);
}

function scanDocument(document) {
    const text = document.getText();
    const diagnostics = [];
    const variables = new Map();
    const functions = new Map();
    const moduleCache = new Map();

    const functionRegex = /(?:^|\s)(?:public\s+)?fun\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(([^)]*)\)/gm;
    for (const match of text.matchAll(functionRegex)) {
        const name = match[1];
        const params = splitParameters(match[2]);
        const nameStart = match.index + match[0].lastIndexOf(name);
        const range = rangeFromOffsets(document, nameStart, name.length);
        functions.set(name, {
            name,
            params,
            signature: `${name}(${params.join(", ")})`,
            range,
            returnType: TYPE_ANY
        });
    }

    const lines = text.split(/\r?\n/);
    for (let lineIndex = 0; lineIndex < lines.length; lineIndex++) {
        const line = lines[lineIndex];
        const trimmed = line.trim();
        if (!trimmed || trimmed === "{" || trimmed === "}") {
            continue;
        }

        if (/^(?:public\s+)?fun\b/.test(trimmed)) {
            continue;
        }

        const declarationMatch = trimmed.match(/^(?:public\s+)?var\s+([A-Za-z_][A-Za-z0-9_]*)\s*->\s*(.+)$/);
        if (declarationMatch) {
            const name = declarationMatch[1];
            const expressionStart = line.indexOf(declarationMatch[2]);
            const expressionInfo = collectExpressionFromLines(lines, lineIndex, expressionStart);
            const expressionText = expressionInfo.text;
            const nameColumn = line.indexOf(name);
            const range = new vscode.Range(lineIndex, nameColumn, lineIndex, nameColumn + name.length);
            const type = inferExpressionType(expressionText, { variables, functions, sourcePath: document.uri.fsPath, moduleCache }, diagnostics, document, lineIndex, expressionStart);
            variables.set(name, {
                name,
                range,
                kind: isModuleType(type) ? "module" : "variable",
                module: getModuleName(type),
                type
            });
            lineIndex = expressionInfo.endLine;
            continue;
        }

        const memberAssignmentMatch = trimmed.match(/^([A-Za-z_][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)\s*->\s*(.+)$/);
        if (memberAssignmentMatch) {
            const variableName = memberAssignmentMatch[1];
            const memberName = memberAssignmentMatch[2];
            const expressionStart = line.indexOf(memberAssignmentMatch[3]);
            const expressionInfo = collectExpressionFromLines(lines, lineIndex, expressionStart);
            const expressionText = expressionInfo.text;
            const variableSymbol = variables.get(variableName);
            const propertyType = inferExpressionType(expressionText, { variables, functions, sourcePath: document.uri.fsPath, moduleCache }, diagnostics, document, lineIndex, expressionStart);
            if (variableSymbol) {
                variableSymbol.type = assignObjectProperty(variableSymbol.type, memberName, propertyType);
            }
            lineIndex = expressionInfo.endLine;
            continue;
        }

        const assignmentMatch = trimmed.match(/^([A-Za-z_][A-Za-z0-9_]*)\s*->\s*(.+)$/);
        if (assignmentMatch) {
            const variableName = assignmentMatch[1];
            const expressionStart = line.indexOf(assignmentMatch[2]);
            const expressionInfo = collectExpressionFromLines(lines, lineIndex, expressionStart);
            const expressionText = expressionInfo.text;
            const variableSymbol = variables.get(variableName);
            if (variableSymbol) {
                variableSymbol.type = inferExpressionType(expressionText, { variables, functions, sourcePath: document.uri.fsPath, moduleCache }, diagnostics, document, lineIndex, expressionStart);
                variableSymbol.kind = isModuleType(variableSymbol.type) ? "module" : "variable";
                variableSymbol.module = getModuleName(variableSymbol.type);
            }
            lineIndex = expressionInfo.endLine;
            continue;
        }

        if (trimmed.startsWith("if ")) {
            const expressionText = trimmed.slice(3).replace(/\{\s*$/, "").trim();
            const expressionColumn = line.indexOf(expressionText);
            inferExpressionType(expressionText, { variables, functions, sourcePath: document.uri.fsPath, moduleCache }, diagnostics, document, lineIndex, expressionColumn);
            continue;
        }

        if (trimmed.startsWith("while ")) {
            const expressionText = trimmed.slice(6).replace(/\{\s*$/, "").trim();
            const expressionColumn = line.indexOf(expressionText);
            inferExpressionType(expressionText, { variables, functions, sourcePath: document.uri.fsPath, moduleCache }, diagnostics, document, lineIndex, expressionColumn);
            continue;
        }

        if (trimmed.startsWith("ret ")) {
            const expressionText = trimmed.slice(4).trim();
            const expressionColumn = line.indexOf(expressionText);
            inferExpressionType(expressionText, { variables, functions, sourcePath: document.uri.fsPath, moduleCache }, diagnostics, document, lineIndex, expressionColumn);
            continue;
        }

        const expressionColumn = line.indexOf(trimmed);
        inferExpressionType(trimmed, { variables, functions, sourcePath: document.uri.fsPath, moduleCache }, diagnostics, document, lineIndex, expressionColumn);
    }

    return { variables, functions, diagnostics };
}

function splitParameters(parameterText) {
    return parameterText
        .split(",")
        .map((part) => part.trim())
        .filter(Boolean);
}

async function getCompilerDiagnostics(document) {
    const client = await getAnalyzerClient();
    if (!client) {
        log("Bundled analyzer is not available.");
        return null;
    }

    try {
        const payload = await client.analyze(document);
        if (!Array.isArray(payload)) {
            return null;
        }

        return payload.map((item) => {
            const line = Math.max((item.Line || 1) - 1, 0);
            const column = Math.max((item.Column || 1) - 1, 0);
            const length = Math.max(item.Length || 1, 1);
            const range = new vscode.Range(line, column, line, column + length);
            const severity = compilerSeverity(item.Level);
            return new vscode.Diagnostic(range, item.Message || "Compiler error.", severity);
        });
    } catch {
        log(`Analyzer request failed for ${document.uri.fsPath}.`);
        return null;
    }
}

function getBundledLanguageServerDllPath() {
    const dllPath = path.join(__dirname, "server", "Pop.LanguageServer.dll");
    const runtimeConfigPath = path.join(__dirname, "server", "Pop.LanguageServer.runtimeconfig.json");
    const depsPath = path.join(__dirname, "server", "Pop.LanguageServer.deps.json");

    log(`Checking bundled language server at ${dllPath}`);
    log(`DLL exists: ${fs.existsSync(dllPath)}`);
    log(`Runtime config exists: ${fs.existsSync(runtimeConfigPath)}`);
    log(`Deps exists: ${fs.existsSync(depsPath)}`);

    if (!fs.existsSync(dllPath) || !fs.existsSync(runtimeConfigPath) || !fs.existsSync(depsPath)) {
        return null;
    }

    return dllPath;
}

async function getAnalyzerClient() {
    const dllPath = getBundledLanguageServerDllPath();
    if (!dllPath) {
        return null;
    }

    if (!analyzerClient) {
        log(`Creating analyzer client for ${dllPath}`);
        analyzerClient = new AnalyzerClient(dllPath);
    }

    return analyzerClient;
}

function compilerSeverity(level) {
    switch ((level || "").toLowerCase()) {
        case "warning":
            return vscode.DiagnosticSeverity.Warning;
        case "information":
            return vscode.DiagnosticSeverity.Information;
        default:
            return vscode.DiagnosticSeverity.Error;
    }
}

class AnalyzerClient {
    constructor(dllPath) {
        this.dllPath = dllPath;
        this.process = null;
        this.buffer = "";
        this.nextId = 1;
        this.pending = new Map();
        this.ready = this.start();
    }

    async start() {
        log(`Starting analyzer process: dotnet ${this.dllPath}`);
        this.process = cp.spawn("dotnet", [this.dllPath], {
            cwd: path.dirname(this.dllPath),
            stdio: ["pipe", "pipe", "pipe"]
        });

        this.process.stdout.on("data", (chunk) => this.handleStdout(chunk));
        this.process.stderr.on("data", (chunk) => {
            log(`Analyzer stderr: ${chunk.toString().trimEnd()}`);
        });
        this.process.on("error", (error) => {
            log(`Analyzer process error: ${error.message}`);
            this.failPending(error);
        });
        this.process.on("close", (code) => {
            log(`Analyzer process exited with code ${code}.`);
            this.failPending(new Error("Pop language server stopped."));
        });
    }

    async analyze(document) {
        await this.ready;

        return new Promise((resolve, reject) => {
            const id = this.nextId++;
            this.pending.set(id, { resolve, reject });
            log(`Sending analyze request ${id} for ${document.uri.fsPath}`);
            this.process.stdin.write(JSON.stringify({
                Type: "analyze",
                Id: id,
                Path: document.uri.fsPath,
                Text: document.getText()
            }) + "\n");
        });
    }

    handleStdout(chunk) {
        this.buffer += chunk.toString();

        while (true) {
            const newlineIndex = this.buffer.indexOf("\n");
            if (newlineIndex < 0) {
                break;
            }

            const line = this.buffer.slice(0, newlineIndex).trim();
            this.buffer = this.buffer.slice(newlineIndex + 1);
            if (!line) {
                continue;
            }

            let message;
            try {
                message = JSON.parse(line);
            } catch {
                log(`Ignoring non-JSON analyzer output: ${line}`);
                continue;
            }

            const pending = this.pending.get(message.Id);
            if (!pending) {
                continue;
            }

            this.pending.delete(message.Id);
            log(`Received diagnostics response ${message.Id} with ${Array.isArray(message.Diagnostics) ? message.Diagnostics.length : 0} item(s).`);
            pending.resolve(message.Diagnostics || []);
        }
    }

    failPending(error) {
        for (const pending of this.pending.values()) {
            pending.reject(error);
        }

        this.pending.clear();
    }

    dispose() {
        this.failPending(new Error("Analyzer client disposed."));
        if (this.process) {
            this.process.kill();
            this.process = null;
        }
    }
}

function collectExpressionFromLines(lines, startLine, startColumn) {
    let text = lines[startLine].slice(startColumn);
    let endLine = startLine;
    let balance = getDelimiterBalance(text);

    while (endLine + 1 < lines.length && balance > 0) {
        endLine++;
        text += `\n${lines[endLine]}`;
        balance += getDelimiterBalance(lines[endLine]);
    }

    return { text, endLine };
}

function getDelimiterBalance(text) {
    let balance = 0;
    let inString = false;
    let stringQuote = "";
    let escaped = false;

    for (const character of text) {
        if (inString) {
            if (escaped) {
                escaped = false;
                continue;
            }

            if (character === "\\") {
                escaped = true;
                continue;
            }

            if (character === stringQuote) {
                inString = false;
                stringQuote = "";
            }

            continue;
        }

        if (character === "\"" || character === "'") {
            inString = true;
            stringQuote = character;
            continue;
        }

        if (character === "{" || character === "[" || character === "(") {
            balance++;
            continue;
        }

        if (character === "}" || character === "]" || character === ")") {
            balance--;
        }
    }

    return balance;
}

function resolveCoreLibrary(modulePath) {
    const normalized = modulePath.replace(/\\/g, "/").toLowerCase();
    if (normalized.endsWith("/math.pop") || normalized === "math.pop") {
        return "math";
    }

    if (normalized.endsWith("/fs.pop") || normalized === "fs.pop") {
        return "fs";
    }

    if (normalized.endsWith("/errors.pop") || normalized === "errors.pop") {
        return "errors";
    }

    return null;
}

function resolveInjectModuleType(modulePath, state) {
    const library = resolveCoreLibrary(modulePath);
    if (library) {
        return coreLibraryModuleType(library, library);
    }

    const resolvedPath = resolveInjectPath(modulePath, state.sourcePath);
    if (!resolvedPath) {
        return objectType({});
    }

    const exports = scanModuleExportsFromFile(resolvedPath, state.moduleCache || new Map(), new Set());
    return moduleType(path.basename(resolvedPath, path.extname(resolvedPath)), exports);
}

function resolveInjectPath(modulePath, sourcePath) {
    if (!modulePath) {
        return null;
    }

    if (path.isAbsolute(modulePath)) {
        return modulePath;
    }

    if (sourcePath) {
        return path.resolve(path.dirname(sourcePath), modulePath);
    }

    return path.resolve(modulePath);
}

function scanModuleExportsFromFile(filePath, moduleCache, activePaths) {
    if (moduleCache.has(filePath)) {
        return moduleCache.get(filePath);
    }

    if (activePaths.has(filePath)) {
        return {};
    }

    activePaths.add(filePath);

    let exports = {};
    try {
        const text = fs.readFileSync(filePath, "utf8");
        exports = scanModuleExportsText(text, filePath, moduleCache, activePaths);
    } catch {
        exports = {};
    }

    activePaths.delete(filePath);
    moduleCache.set(filePath, exports);
    return exports;
}

function scanModuleExportsText(text, sourcePath, moduleCache, activePaths) {
    const variables = new Map();
    const functions = new Map();
    const exports = {};
    const lines = text.split(/\r?\n/);

    const functionRegex = /(?:^|\s)(?:public\s+)?fun\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(([^)]*)\)/gm;
    for (const match of text.matchAll(functionRegex)) {
        const name = match[1];
        const params = splitParameters(match[2]);
        functions.set(name, {
            name,
            params,
            signature: `${name}(${params.join(", ")})`,
            returnType: TYPE_ANY
        });
    }

    for (let lineIndex = 0; lineIndex < lines.length; lineIndex++) {
        const line = lines[lineIndex];
        const trimmed = line.trim();
        if (!trimmed || trimmed === "{" || trimmed === "}") {
            continue;
        }

        const publicVarMatch = trimmed.match(/^public\s+var\s+([A-Za-z_][A-Za-z0-9_]*)\s*->\s*(.+)$/);
        if (publicVarMatch) {
            const name = publicVarMatch[1];
            const expressionStart = line.indexOf(publicVarMatch[2]);
            const expressionInfo = collectExpressionFromLines(lines, lineIndex, expressionStart);
            const inferredType = inferExpressionType(
                expressionInfo.text,
                { variables, functions, sourcePath, moduleCache, activePaths },
                [],
                null,
                lineIndex,
                expressionStart
            );

            variables.set(name, { name, type: inferredType, kind: isModuleType(inferredType) ? "module" : "variable", module: getModuleName(inferredType) });
            exports[name] = propertyEntry(name, inferredType);
            lineIndex = expressionInfo.endLine;
            continue;
        }

        const publicFunMatch = trimmed.match(/^public\s+fun\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(([^)]*)\)/);
        if (publicFunMatch) {
            const name = publicFunMatch[1];
            const params = splitParameters(publicFunMatch[2]);
            exports[name] = fn(`${name}(${params.join(", ")})`, params, "Public function exported from the injected module.");
        }
    }

    return exports;
}

function getLinePrefix(document, position) {
    return document.lineAt(position.line).text.slice(0, position.character);
}

function getMemberCompletionContext(prefix, scan) {
    const chainMatch = prefix.match(/([A-Za-z_][A-Za-z0-9_\.]*)\.([A-Za-z_][A-Za-z0-9_]*)?$/);
    if (!chainMatch) {
        return null;
    }

    const target = resolveMemberTarget(chainMatch[1], scan);
    if (!target) {
        return null;
    }

    return {
        ...target,
        partial: chainMatch[2] || ""
    };
}

function getStaticMemberCompletionContext(prefix) {
    if (/\bcorn\.[A-Za-z_][A-Za-z0-9_]*?$/.test(prefix)) {
        return null;
    }

    if (/\bcorn\.$/.test(prefix)) {
        return { qualifier: "corn", entries: BUILT_INS.root };
    }

    if (/\bcorn\.fs\.$/.test(prefix)) {
        return { qualifier: "corn.fs", entries: BUILT_INS.modules.fs };
    }

    if (/\bcorn\.math\.$/.test(prefix)) {
        return { qualifier: "corn.math", entries: BUILT_INS.modules.math };
    }

    if (/\bcorn\.json\.$/.test(prefix)) {
        return { qualifier: "corn.json", entries: BUILT_INS.modules.json };
    }

    if (/\bcorn\.http\.$/.test(prefix)) {
        return { qualifier: "corn.http", entries: BUILT_INS.modules.http };
    }

    return null;
}

function getMemberHoverTarget(prefix, scan) {
    const chainMatch = prefix.match(/([A-Za-z_][A-Za-z0-9_\.]*)$/);
    if (!chainMatch) {
        return null;
    }

    const chain = chainMatch[1];
    const lastDot = chain.lastIndexOf(".");
    if (lastDot < 0) {
        return null;
    }

    return resolveMemberTarget(chain.slice(0, lastDot), scan);
}

function resolveMemberTarget(chain, scan) {
    if (chain === "corn") {
        return { qualifier: "corn", entries: BUILT_INS.root };
    }

    if (chain.startsWith("corn.")) {
        const moduleName = chain.slice("corn.".length);
        const entries = BUILT_INS.modules[moduleName];
        if (entries) {
            return { qualifier: `corn.${moduleName}`, entries };
        }
    }

    const inferredType = inferExpressionType(chain, scan, [], null, 0);
    const inferredEntries = entriesForType(inferredType);
    if (inferredEntries) {
        return { qualifier: chain, entries: inferredEntries };
    }

    return null;
}

const TYPE_ANY = { kind: "any" };
const TYPE_BOOL = primitiveType("bool");
const TYPE_INT = primitiveType("int");
const TYPE_DOUBLE = primitiveType("double");
const TYPE_STRING = primitiveType("string");
const TYPE_CHAR = primitiveType("char");
const TYPE_NIL = primitiveType("nil");
const TYPE_ERROR = objectType({
    code: TYPE_STRING,
    message: TYPE_STRING
});
const FS_INFO_TYPE = objectType({
    path: TYPE_STRING,
    name: TYPE_STRING,
    exists: TYPE_BOOL,
    isFile: TYPE_BOOL,
    isDir: TYPE_BOOL,
    size: TYPE_INT,
    created: TYPE_INT,
    modified: TYPE_INT
});
const HTTP_RESPONSE_TYPE = objectType({
    ok: TYPE_BOOL,
    status: TYPE_INT,
    reason: TYPE_STRING,
    body: TYPE_STRING,
    headers: objectType({}),
    url: TYPE_STRING,
    method: TYPE_STRING
});

function primitiveType(name) {
    return { kind: "primitive", name };
}

function objectType(properties) {
    return { kind: "object", properties: { ...properties } };
}

function arrayType(elementType = TYPE_ANY) {
    return { kind: "array", elementType };
}

function functionType(returnType = TYPE_ANY, params = []) {
    return { kind: "function", returnType, params };
}

function moduleType(name, members) {
    return { kind: "module", name, members };
}

function isModuleType(type) {
    return type && type.kind === "module";
}

function getModuleName(type) {
    return isModuleType(type) ? type.name : undefined;
}

function coreLibraryModuleType(name, alias) {
    return moduleType(name, CORE_LIBS[name].members, alias);
}

function assignObjectProperty(type, name, propertyType) {
    if (!type || type.kind === "any") {
        return objectType({ [name]: propertyType });
    }

    if (type.kind !== "object") {
        return type;
    }

    return objectType({ ...type.properties, [name]: propertyType });
}

function inferExpressionType(text, state, diagnostics, document, line, column = 0) {
    const tokenizer = new ExpressionTokenizer(text);
    const parser = new ExpressionParser(tokenizer.tokens, state, diagnostics, document, line, column);
    return parser.parse();
}

function entriesForType(type) {
    if (!type) {
        return null;
    }

    if (type.kind === "module") {
        return type.members;
    }

    if (type.kind === "primitive") {
        switch (type.name) {
            case "int":
            case "double":
                return {
                    min: constant("min", type.name, `The minimum ${type.name} value.`),
                    max: constant("max", type.name, `The maximum ${type.name} value.`)
                };
            case "string":
                return {
                    len: constant("len", "int", "String length."),
                    at: fn("at(index)", ["index"], "Returns the character at an index."),
                    add: fn("add(item)", ["item"], "Appends a value to a string."),
                    contains: fn("contains(item)", ["item"], "Returns true when the string contains a value."),
                    insert: fn("insert(index, item)", ["index", "item"], "Inserts a value into a string."),
                    replace: fn("replace(index, item)", ["index", "item"], "Replaces one character in a string."),
                    remove: fn("remove(index)", ["index"], "Removes one character from a string."),
                    forEach: fn("forEach(callback)", ["callback"], "Iterates the string characters.")
                };
            default:
                return null;
        }
    }

    if (type.kind === "array") {
        return {
            len: constant("len", "int", "Array length."),
            at: fn("at(index)", ["index"], "Returns the element at an index."),
            add: fn("add(item)", ["item"], "Adds an element."),
            insert: fn("insert(index, item)", ["index", "item"], "Inserts an element."),
            replace: fn("replace(index, item)", ["index", "item"], "Replaces an element."),
            remove: fn("remove(index)", ["index"], "Removes an element."),
            forEach: fn("forEach(callback)", ["callback"], "Iterates the array elements.")
        };
    }

    if (type.kind === "object") {
        const entries = {
            len: constant("len", "int", "Object property count."),
            get: fn("get(name)", ["name"], "Gets a property by name."),
            add: fn("add(name, value)", ["name", "value"], "Adds or replaces a property."),
            remove: fn("remove(name)", ["name"], "Removes a property."),
            forEach: fn("forEach(callback)", ["callback"], "Iterates object properties.")
        };

        for (const [name, propertyType] of Object.entries(type.properties)) {
            entries[name] = propertyEntry(name, propertyType);
        }

        return entries;
    }

    return null;
}

function propertyEntry(name, type) {
    return {
        kind: "property",
        signature: name,
        detail: describeType(type),
        documentation: `Property of type \`${describeType(type)}\`.`
    };
}

function describeType(type) {
    if (!type || type.kind === "any") {
        return "any";
    }

    if (type.kind === "primitive") {
        return type.name;
    }

    if (type.kind === "array") {
        return `[${describeType(type.elementType)}]`;
    }

    if (type.kind === "function") {
        return "function";
    }

    if (type.kind === "module") {
        return `module ${type.name}`;
    }

    if (type.kind === "object") {
        return "object";
    }

    return "any";
}

class ExpressionTokenizer {
    constructor(text) {
        this.text = text;
        this.position = 0;
        this.tokens = this.tokenize();
    }

    tokenize() {
        const tokens = [];
        while (this.position < this.text.length) {
            const character = this.text[this.position];
            if (/\s/.test(character)) {
                this.position++;
                continue;
            }

            if ("{}[]().,:".includes(character)) {
                tokens.push({ kind: character, text: character, start: this.position });
                this.position++;
                continue;
            }

            if (character === "-" && this.text[this.position + 1] === ">") {
                tokens.push({ kind: "arrow", text: "->", start: this.position });
                this.position += 2;
                continue;
            }

            if (character === '"' || character === "'") {
                tokens.push(this.readString(character));
                continue;
            }

            if (/[0-9]/.test(character)) {
                tokens.push(this.readNumber());
                continue;
            }

            if (/[A-Za-z_]/.test(character)) {
                tokens.push(this.readIdentifier());
                continue;
            }

            tokens.push({ kind: "unknown", text: character, start: this.position });
            this.position++;
        }

        tokens.push({ kind: "eof", text: "", start: this.position });
        return tokens;
    }

    readString(quote) {
        const start = this.position;
        this.position++;
        while (this.position < this.text.length) {
            const character = this.text[this.position];
            if (character === "\\") {
                this.position += 2;
                continue;
            }

            this.position++;
            if (character === quote) {
                break;
            }
        }

        return {
            kind: quote === '"' ? "string" : "char",
            text: this.text.slice(start, this.position),
            start
        };
    }

    readNumber() {
        const start = this.position;
        while (this.position < this.text.length && /[0-9]/.test(this.text[this.position])) {
            this.position++;
        }

        if (this.text[this.position] === ".") {
            this.position++;
            while (this.position < this.text.length && /[0-9]/.test(this.text[this.position])) {
                this.position++;
            }
        }

        return { kind: "number", text: this.text.slice(start, this.position), start };
    }

    readIdentifier() {
        const start = this.position;
        while (this.position < this.text.length && /[A-Za-z0-9_]/.test(this.text[this.position])) {
            this.position++;
        }

        return { kind: "identifier", text: this.text.slice(start, this.position), start };
    }
}

class ExpressionParser {
    constructor(tokens, state, diagnostics, document, line, column) {
        this.tokens = tokens;
        this.position = 0;
        this.state = state;
        this.diagnostics = diagnostics;
        this.document = document;
        this.line = line;
        this.column = column;
    }

    parse() {
        return this.parsePostfix();
    }

    parsePostfix() {
        let type = this.parsePrimary();
        while (true) {
            if (this.current().kind === ".") {
                this.next();
                const memberToken = this.match("identifier");
                type = this.resolveMember(type, memberToken);
                continue;
            }

            if (this.current().kind === "(") {
                const args = this.parseArguments();
                type = this.resolveCall(type, args);
                continue;
            }

            break;
        }

        return type;
    }

    parseArguments() {
        this.match("(");
        const args = [];
        while (this.current().kind !== ")" && this.current().kind !== "eof") {
            args.push(this.parsePostfix());
            if (this.current().kind === ",") {
                this.next();
                continue;
            }

            break;
        }

        this.match(")");
        return args;
    }

    parsePrimary() {
        const token = this.current();
        switch (token.kind) {
            case "{":
                return this.parseObject();
            case "[":
                return this.parseArray();
            case "string":
                this.next();
                return TYPE_STRING;
            case "char":
                this.next();
                return TYPE_CHAR;
            case "number":
                this.next();
                return token.text.includes(".") ? TYPE_DOUBLE : TYPE_INT;
            case "identifier":
                return this.parseIdentifier();
            case "(":
                this.next();
                const type = this.parsePostfix();
                this.match(")");
                return type;
            default:
                this.next();
                return TYPE_ANY;
        }
    }

    parseObject() {
        this.match("{");
        const properties = {};
        while (this.current().kind !== "}" && this.current().kind !== "eof") {
            const identifier = this.match("identifier");
            if (this.current().kind === ":") {
                this.next();
            }

            properties[identifier.text] = this.parsePostfix();
            if (this.current().kind === ",") {
                this.next();
            }
        }

        this.match("}");
        return objectType(properties);
    }

    parseArray() {
        this.match("[");
        const elements = [];
        while (this.current().kind !== "]" && this.current().kind !== "eof") {
            elements.push(this.parsePostfix());
            if (this.current().kind === ",") {
                this.next();
            }
        }

        this.match("]");
        const elementType = elements.length === 0 ? TYPE_ANY : elements[0];
        return arrayType(elementType);
    }

    parseIdentifier() {
        const token = this.match("identifier");
        switch (token.text) {
            case "true":
            case "false":
                return TYPE_BOOL;
            case "nil":
                return TYPE_NIL;
            case "inject":
                return this.parseInject();
            case "corn":
                return moduleType("corn", BUILT_INS.root);
            default:
                if (this.state.variables.has(token.text)) {
                    return this.state.variables.get(token.text).type || TYPE_ANY;
                }

                if (this.state.functions.has(token.text)) {
                    const symbol = this.state.functions.get(token.text);
                    return functionType(symbol.returnType, symbol.params);
                }

                return TYPE_ANY;
        }
    }

    parseInject() {
        const pathToken = this.match("string");
        const path = stripQuotes(pathToken.text);
        return resolveInjectModuleType(path, this.state);
    }

    resolveMember(targetType, memberToken) {
        const memberName = memberToken.text;
        const entries = entriesForType(targetType);
        if (!entries) {
            return TYPE_ANY;
        }

        const entry = entries[memberName];
        if (!entry) {
            this.report(memberToken.start, memberToken.text.length, `Type '${describeType(targetType)}' does not contain a member named '${memberName}'.`);
            return TYPE_ANY;
        }

        if (targetType.kind === "object" && targetType.properties[memberName]) {
            return targetType.properties[memberName];
        }

        return inferEntryType(targetType, memberName, entry);
    }

    resolveCall(targetType, args) {
        if (!targetType || targetType.kind === "any") {
            return TYPE_ANY;
        }

        if (targetType.kind !== "function") {
            return TYPE_ANY;
        }

        return targetType.returnType || TYPE_ANY;
    }

    current() {
        return this.tokens[this.position];
    }

    next() {
        const token = this.tokens[this.position];
        this.position = Math.min(this.position + 1, this.tokens.length - 1);
        return token;
    }

    match(kind) {
        if (this.current().kind === kind) {
            return this.next();
        }

        return { kind, text: "", start: this.current().start };
    }

    report(start, length, message) {
        if (!this.document) {
            return;
        }

        const range = new vscode.Range(this.line, this.column + start, this.line, this.column + start + length);
        this.diagnostics.push(new vscode.Diagnostic(range, message, vscode.DiagnosticSeverity.Error));
    }
}

function inferEntryType(ownerType, name, entry) {
    if (entry.kind === "module") {
        return moduleType(name, BUILT_INS.modules[name] || entry.members || {});
    }

    if (entry.kind === "constant") {
        switch (entry.detail) {
            case "bool":
                return TYPE_BOOL;
            case "int":
                return TYPE_INT;
            case "double":
                return TYPE_DOUBLE;
            case "string":
                return TYPE_STRING;
            default:
                return TYPE_ANY;
        }
    }

    if (entry.kind === "property") {
        return propertyTypeFromDetail(entry.detail);
    }

    if (entry.kind === "function") {
        return functionType(inferFunctionReturnType(ownerType, name));
    }

    return TYPE_ANY;
}

function inferFunctionReturnType(ownerType, name) {
    if (ownerType.kind === "module") {
        switch (ownerType.name) {
            case "corn":
                return inferRootFunctionReturnType(name);
            case "fs":
                return inferFsReturnType(name);
            case "math":
                return inferMathReturnType(name);
            case "json":
                return inferJsonReturnType(name);
            case "http":
                return inferHttpReturnType(name);
            case "errors":
                return inferErrorsReturnType(name);
            default:
                return TYPE_ANY;
        }
    }

    if (ownerType.kind === "object") {
        switch (name) {
            case "len":
                return TYPE_INT;
            case "get":
            case "remove":
                return commonObjectPropertyType(ownerType);
            case "add":
            case "forEach":
                return TYPE_NIL;
            default:
                return TYPE_ANY;
        }
    }

    if (ownerType.kind === "array") {
        switch (name) {
            case "at":
            case "replace":
            case "remove":
                return ownerType.elementType;
            case "len":
                return TYPE_INT;
            default:
                return TYPE_NIL;
        }
    }

    if (ownerType.kind === "primitive") {
        if (ownerType.name === "string") {
            switch (name) {
                case "len":
                    return TYPE_INT;
                case "at":
                    return TYPE_CHAR;
                case "contains":
                    return TYPE_BOOL;
                default:
                    return TYPE_STRING;
            }
        }

        if (ownerType.name === "int" || ownerType.name === "double") {
            return ownerType;
        }
    }

    return TYPE_ANY;
}

function inferRootFunctionReturnType(name) {
    switch (name) {
        case "type":
        case "str":
        case "input":
            return TYPE_STRING;
        case "int":
            return TYPE_INT;
        case "double":
        case "clock":
            return TYPE_DOUBLE;
        case "bool":
        case "has":
        case "isError":
            return TYPE_BOOL;
        case "error":
            return TYPE_ERROR;
        case "keys":
            return arrayType(TYPE_STRING);
        case "print":
        case "println":
            return TYPE_NIL;
        default:
            return TYPE_ANY;
    }
}

function inferFsReturnType(name) {
    switch (name) {
        case "read":
        case "cwd":
        case "join":
        case "name":
        case "stem":
        case "ext":
        case "parent":
        case "absolute":
            return TYPE_STRING;
        case "exists":
        case "isFile":
        case "isDir":
            return TYPE_BOOL;
        case "info":
            return FS_INFO_TYPE;
        case "size":
        case "created":
        case "modified":
            return TYPE_INT;
        case "list":
        case "files":
        case "dirs":
            return arrayType(TYPE_STRING);
        default:
            return TYPE_NIL;
    }
}

function inferMathReturnType(name) {
    if (name === "pi" || name === "tau" || name === "e") {
        return TYPE_DOUBLE;
    }

    return TYPE_DOUBLE;
}

function inferJsonReturnType(name) {
    switch (name) {
        case "stringify":
        case "pretty":
            return TYPE_STRING;
        default:
            return TYPE_ANY;
    }
}

function inferHttpReturnType() {
    return HTTP_RESPONSE_TYPE;
}

function inferErrorsReturnType(name) {
    switch (name) {
        case "is":
            return TYPE_BOOL;
        case "code":
        case "message":
            return TYPE_STRING;
        case "make":
            return TYPE_ERROR;
        default:
            return TYPE_ANY;
    }
}

function commonObjectPropertyType(type) {
    const values = Object.values(type.properties);
    return values.length === 0 ? TYPE_ANY : values[0];
}

function propertyTypeFromDetail(detail) {
    switch (detail) {
        case "bool":
            return TYPE_BOOL;
        case "int":
            return TYPE_INT;
        case "double":
            return TYPE_DOUBLE;
        case "string":
            return TYPE_STRING;
        default:
            return TYPE_ANY;
    }
}

function stripQuotes(text) {
    return text.length >= 2 ? text.slice(1, -1) : text;
}

function addEntries(target, entries, qualifier = "", replaceRange = null) {
    for (const [name, entry] of Object.entries(entries)) {
        target.push(createCompletionItem(name, entry, qualifier, replaceRange));
    }
}

function addScannedSymbols(target, scan) {
    for (const fnSymbol of scan.functions.values()) {
        const item = new vscode.CompletionItem(fnSymbol.name, vscode.CompletionItemKind.Function);
        item.detail = "function";
        item.documentation = new vscode.MarkdownString(`\`${fnSymbol.signature}\``);
        item.insertText = new vscode.SnippetString(`${fnSymbol.name}($1)`);
        target.push(item);
    }

    for (const variableSymbol of scan.variables.values()) {
        const kind = variableSymbol.kind === "module"
            ? vscode.CompletionItemKind.Module
            : vscode.CompletionItemKind.Variable;
        const item = new vscode.CompletionItem(variableSymbol.name, kind);
        item.detail = variableSymbol.kind === "module"
            ? `module ${variableSymbol.module || ""}`.trim()
            : describeType(variableSymbol.type);
        target.push(item);
    }
}

function createCompletionItem(name, entry, qualifier, replaceRange) {
    const item = new vscode.CompletionItem(name, completionKind(entry.kind));
    item.detail = qualifier ? `${qualifier}.${entry.signature || name}` : (entry.signature || entry.detail || name);
    item.documentation = new vscode.MarkdownString(renderEntryMarkdown(name, entry, qualifier));
    if (replaceRange) {
        item.range = replaceRange;
    }

    if (entry.kind === "function") {
        const insertName = qualifier && qualifier !== "corn" ? name : name;
        item.insertText = entry.params.length === 0
            ? `${insertName}()`
            : new vscode.SnippetString(`${insertName}(${entry.params.map((_, index) => `$${index + 1}`).join(", ")})`);
    }

    return item;
}

function dedupeCompletionItems(items) {
    const seen = new Set();
    const unique = [];
    for (const item of items) {
        const key = `${item.label}:${item.kind}`;
        if (seen.has(key)) {
            continue;
        }

        seen.add(key);
        unique.push(item);
    }

    return unique;
}

function completionKind(kind) {
    switch (kind) {
        case "function":
            return vscode.CompletionItemKind.Function;
        case "module":
            return vscode.CompletionItemKind.Module;
        case "constant":
            return vscode.CompletionItemKind.Constant;
        case "property":
            return vscode.CompletionItemKind.Property;
        case "keyword":
            return vscode.CompletionItemKind.Keyword;
        default:
            return vscode.CompletionItemKind.Text;
    }
}

function createEntryHover(name, entry, qualifier = "") {
    return new vscode.Hover(new vscode.MarkdownString(renderEntryMarkdown(name, entry, qualifier)));
}

function renderEntryMarkdown(name, entry, qualifier) {
    const fullName = qualifier ? `${qualifier}.${name}` : name;
    const signature = entry.signature || name;
    return [`\`${qualifier ? `${qualifier}.${signature}` : signature}\``, "", entry.documentation || ""].join("\n");
}

function renderSymbolMarkdown(symbol) {
    if (symbol.params) {
        return `\`${symbol.signature}\`\n\nUser-defined function.`;
    }

    if (symbol.kind === "module") {
        return `\`${symbol.name}\`\n\nModule variable.`;
    }

    return `\`${symbol.name}\`\n\nType: \`${describeType(symbol.type)}\``;
}

function findScannedSymbol(scan, name) {
    return scan.functions.get(name) || scan.variables.get(name) || null;
}

function rangeFromOffsets(document, start, length) {
    return new vscode.Range(document.positionAt(start), document.positionAt(start + length));
}

function getCallContext(document, position) {
    const offset = document.offsetAt(position);
    const text = document.getText().slice(0, offset);
    let depth = 0;
    let argumentIndex = 0;

    for (let index = text.length - 1; index >= 0; index--) {
        const character = text[index];
        if (character === ")") {
            depth++;
            continue;
        }

        if (character === "(") {
            if (depth === 0) {
                const before = text.slice(0, index);
                const targetMatch = before.match(/([A-Za-z_][A-Za-z0-9_\.]*)\s*$/);
                if (!targetMatch) {
                    return null;
                }

                return {
                    target: targetMatch[1],
                    argumentIndex
                };
            }

            depth--;
            continue;
        }

        if (character === "," && depth === 0) {
            argumentIndex++;
        }
    }

    return null;
}

function resolveSignature(scan, target) {
    if (BUILT_INS.root[target]) {
        return BUILT_INS.root[target];
    }

    if (target.startsWith("corn.")) {
        const segments = target.split(".");
        if (segments.length === 2) {
            return BUILT_INS.root[segments[1]] || null;
        }

        if (segments.length === 3 && BUILT_INS.modules[segments[1]]) {
            return BUILT_INS.modules[segments[1]][segments[2]] || null;
        }
    }

    if (!target.includes(".") && scan.functions.has(target)) {
        const symbol = scan.functions.get(target);
        return {
            kind: "function",
            signature: symbol.signature,
            params: symbol.params,
            documentation: "User-defined function."
        };
    }

    const firstDot = target.indexOf(".");
    if (firstDot > 0) {
        const variableName = target.slice(0, firstDot);
        const memberName = target.slice(firstDot + 1);
        const variableSymbol = scan.variables.get(variableName);
        if (variableSymbol && variableSymbol.module && CORE_LIBS[variableSymbol.module]) {
            return CORE_LIBS[variableSymbol.module].members[memberName] || null;
        }
    }

    return null;
}

function fn(signature, params, documentation) {
    return {
        kind: "function",
        signature,
        params,
        documentation
    };
}

function constant(name, detail, documentation) {
    return {
        kind: "constant",
        detail,
        documentation,
        signature: name
    };
}

function moduleEntry(name, documentation) {
    return {
        kind: "module",
        signature: name,
        documentation
    };
}

module.exports = {
    activate,
    deactivate
};
