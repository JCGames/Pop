# Pop VS Code Extension

This extension adds Pop language support to VS Code:

- syntax highlighting
- completion items
- hover docs
- go to definition for same-file symbols
- signature help
- document symbols
- inferred member completion for known object shapes
- invalid-member diagnostics when a member access can be disproven locally

## Development

1. Open the `vscode-pop` folder in VS Code.
2. Press `F5` to launch an Extension Development Host.
3. Open a `.pop` file in the new window.

## Packaging

Bundle the language server into the extension first:

```powershell
npm run bundle-server
```

Then package the extension with `vsce`:

```powershell
npm install -g @vscode/vsce
vsce package
```

Then install the generated `.vsix` file from VS Code.
