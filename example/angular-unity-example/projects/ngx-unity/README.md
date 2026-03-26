# ngx-unity

Reusable Angular library for embedding Unity WebGL builds.

## Exports

- **`NgxUnityViewport`** — Component that loads a Unity WebGL build into a `<canvas>`, with automatic mock fallback for development.
- **`IUnityInstance`** — TypeScript interface matching the object returned by Unity's `createUnityInstance()`.
- **`createMockUnityInstance()`** — Testing utility for creating a mock `IUnityInstance`.

## Usage

```html
<ngx-unity-viewport
  buildPath="unity"
  height="400px"
  [mockFactory]="myMockFactory"
  (instanceReady)="onReady($event)" />
```

## Building

```bash
ng build ngx-unity
```

## Publishing

```bash
cd dist/ngx-unity
npm publish
```

See the root [README](../../../../README.md) for full documentation.
