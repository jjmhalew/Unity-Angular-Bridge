import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { IUnityInstance } from '../models/unity-instance';
import { createMockUnityInstance } from '../testing/mock-unity';

/** Track which Unity loader scripts have already been loaded (or are loading). */
const loaderPromises = new Map<string, Promise<void>>();

/** Auto-incrementing counter for generating unique canvas IDs. */
let nextCanvasId = 0;

/**
 * Embeds a Unity WebGL player in a `<canvas>` element.
 *
 * Each instance of this component creates its own canvas with a unique DOM ID,
 * so multiple viewports can coexist on the same page.
 * When two viewports share the same `buildPath`, the Unity loader script is
 * loaded only once and reused.
 *
 * If a real Unity build exists at the given `buildPath`, it is loaded automatically.
 * Otherwise a mock Unity instance is created so the rest of the app still works.
 *
 * @example
 * ```html
 * <ngx-unity-viewport
 *   buildPath="unity"
 *   height="500px"
 *   (instanceReady)="onUnityReady($event)" />
 * ```
 *
 * @example Multiple instances
 * ```html
 * <ngx-unity-viewport
 *   buildPath="unity"
 *   height="300px"
 *   (instanceReady)="onFirstReady($event)" />
 * <ngx-unity-viewport
 *   buildPath="unity"
 *   height="300px"
 *   (instanceReady)="onSecondReady($event)" />
 * ```
 */
@Component({
  selector: 'ngx-unity-viewport',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    :host { display: block; }
    .viewport {
      width: 100%;
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      overflow: hidden;
      background: #1a1a2e;
      position: relative;
    }
    canvas {
      display: block;
      width: 100%;
    }
    .overlay {
      position: absolute;
      inset: 0;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      color: #aaa;
      font-size: 0.95rem;
      text-align: center;
      padding: 1rem;
      pointer-events: none;
    }
    .overlay.hidden { display: none; }
    .overlay h3 { color: #ddd; margin: 0 0 0.5rem; }
    .overlay code {
      background: rgba(255,255,255,0.1);
      padding: 2px 8px;
      border-radius: 4px;
      font-size: 0.85em;
    }
    .status-bar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.4rem 0.75rem;
      background: #f5f5f5;
      border-top: 1px solid #e0e0e0;
      font-size: 0.8rem;
      color: #666;
    }
    .badge {
      padding: 2px 8px;
      border-radius: 10px;
      font-size: 0.75rem;
    }
    .badge.mock { background: #fff3e0; color: #e65100; }
    .badge.webgl { background: #e8f5e9; color: #2e7d32; }
    .progress { color: #1976d2; }
  `,
  template: `
    <div class="viewport">
      <canvas #unityCanvas [id]="canvasId" [style.height]="height()"></canvas>

      <div class="overlay" [class.hidden]="isLoaded()">
        @if (isLoading()) {
          <p class="progress">{{ loadingProgress() }}</p>
        } @else if (useMock()) {
          <h3>Unity Viewport</h3>
          <p>No WebGL build found at <code>public/{{ buildPath() }}/</code></p>
          <p>Using mock instance for development.</p>
        }
      </div>
    </div>

    <div class="status-bar">
      <span>
        @if (useMock()) {
          <span class="badge mock">Mock</span>
        } @else {
          <span class="badge webgl">WebGL</span>
        }
        Unity Viewport
      </span>
      <span>{{ isLoaded() ? 'Connected' : 'Disconnected' }}</span>
    </div>
  `,
})
export class NgxUnityViewport implements OnInit, OnDestroy {
  /** Path to the Unity WebGL build folder, relative to Angular's public/assets directory. */
  readonly buildPath = input('unity');

  /** CSS height of the canvas element. */
  readonly height = input('400px');

  /**
   * Optional factory function that creates a mock `IUnityInstance`.
   * Called when no real Unity build is found.
   * If not provided, a default logging-only mock is used.
   */
  readonly mockFactory = input<(() => IUnityInstance) | undefined>(undefined);

  /** Emitted when a Unity instance (real or mock) is ready. */
  readonly instanceReady = output<IUnityInstance>();

  /** Whether the Unity loader is currently loading. */
  readonly isLoading = signal(false);

  /** Whether a Unity instance (real or mock) has been created. */
  readonly isLoaded = signal(false);

  /** Whether the instance is a mock (no real WebGL build found). */
  readonly useMock = signal(false);

  /** Current loading progress message. */
  readonly loadingProgress = signal('Loading Unity...');

  /** Unique DOM ID for this viewport's canvas element. */
  readonly canvasId = `ngx-unity-canvas-${nextCanvasId++}`;

  private readonly canvasRef = viewChild.required<ElementRef<HTMLCanvasElement>>('unityCanvas');
  private unityInstance: IUnityInstance | null = null;

  async ngOnInit(): Promise<void> {
    const path = this.buildPath();
    const loaderUrl = `${path}/Build/${path}.loader.js`;
    const buildExists = await this.checkBuildExists(loaderUrl);

    if (buildExists) {
      await this.loadRealUnity(loaderUrl);
    } else {
      this.useMock.set(true);
      const factory = this.mockFactory();
      const mock = factory ? factory() : createMockUnityInstance();
      this.unityInstance = mock;
      this.isLoaded.set(true);
      this.instanceReady.emit(mock);
    }
  }

  ngOnDestroy(): void {
    this.unityInstance?.Quit();
  }

  private async checkBuildExists(loaderUrl: string): Promise<boolean> {
    try {
      const resp = await fetch(loaderUrl, { method: 'HEAD' });
      return resp.ok;
    } catch {
      return false;
    }
  }

  private async loadRealUnity(loaderUrl: string): Promise<void> {
    this.isLoading.set(true);

    // Reuse the existing loader promise if the same script was already requested.
    if (!loaderPromises.has(loaderUrl)) {
      const loaderPromise = new Promise<void>((resolve, reject) => {
        const script = document.createElement('script');
        script.src = loaderUrl;
        script.onload = () => resolve();
        script.onerror = () => reject(new Error('Failed to load Unity loader'));
        document.body.appendChild(script);
      });
      loaderPromises.set(loaderUrl, loaderPromise);
    }
    await loaderPromises.get(loaderUrl)!;

    const path = this.buildPath();
    const canvas = this.canvasRef().nativeElement;
    const buildPath = `${path}/Build`;

    const dataExt = await this.detectExtension(`${buildPath}/${path}.data`);
    const frameworkExt = await this.detectExtension(`${buildPath}/${path}.framework.js`);
    const wasmExt = await this.detectExtension(`${buildPath}/${path}.wasm`);

    const config = {
      dataUrl: `${buildPath}/${path}.data${dataExt}`,
      frameworkUrl: `${buildPath}/${path}.framework.js${frameworkExt}`,
      codeUrl: `${buildPath}/${path}.wasm${wasmExt}`,
      streamingAssetsUrl: `${path}/StreamingAssets`,
      companyName: 'DefaultCompany',
      productName: 'UnityApp',
      productVersion: '1.0.0',
      autoSyncPersistentDataPath: true,
    };

    try {
      /* eslint-disable @typescript-eslint/no-explicit-any */
      const createUnityInstance = (window as any).createUnityInstance;
      const instance: IUnityInstance = await createUnityInstance(
        canvas,
        config,
        (progress: number) => {
          this.loadingProgress.set(`Loading Unity... ${Math.round(progress * 100)}%`);
        },
      );

      this.unityInstance = instance;
      this.isLoaded.set(true);
      this.instanceReady.emit(instance);
    } catch (err) {
      console.error('Unity WebGL failed to load, falling back to mock:', err);
      this.useMock.set(true);
      const factory = this.mockFactory();
      const mock = factory ? factory() : createMockUnityInstance();
      this.unityInstance = mock;
      this.isLoaded.set(true);
      this.instanceReady.emit(mock);
    } finally {
      this.isLoading.set(false);
    }
  }

  /** Detect which compression extension a file has (.br, .gz, or none). */
  private async detectExtension(basePath: string): Promise<string> {
    for (const ext of ['', '.br', '.gz']) {
      try {
        const resp = await fetch(basePath + ext, { method: 'HEAD' });
        if (resp.ok) return ext;
      } catch {
        // try next
      }
    }
    return '';
  }
}
