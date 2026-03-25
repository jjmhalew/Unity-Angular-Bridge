import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  OnDestroy,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';
import { UnityBridgeService } from '../services/unity-bridge.service';
import { IUnityInstance } from '../generated/unity-client';
import { createMockUnityInstance } from '../services/mock-unity';

/** Path where a real Unity WebGL build is expected inside the Angular public folder. */
const UNITY_BUILD_PATH = 'unity-build';

/**
 * Embeds the Unity WebGL player in a <canvas> element.
 *
 * If a real build exists at `public/unity-build/` it is loaded automatically.
 * Otherwise a mock Unity instance is used so the rest of the demo still works.
 */
@Component({
  selector: 'app-unity-viewport',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
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
      height: 400px;
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
      <canvas #unityCanvas id="unity-canvas"></canvas>

      <div class="overlay" [class.hidden]="isLoaded()">
        @if (isLoading()) {
          <p class="progress">{{ loadingProgress() }}</p>
        } @else if (useMock()) {
          <h3>Unity Viewport</h3>
          <p>No WebGL build found at <code>public/unity-build/</code></p>
          <p>Using mock instance — see the Unity project README for build instructions.</p>
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
      <span>{{ bridge.isConnected() ? 'Connected' : 'Disconnected' }}</span>
    </div>
  `,
})
export class UnityViewport implements OnInit, OnDestroy {
  protected readonly bridge = inject(UnityBridgeService);

  private readonly canvasRef = viewChild.required<ElementRef<HTMLCanvasElement>>('unityCanvas');

  protected readonly isLoading = signal(false);
  protected readonly isLoaded = signal(false);
  protected readonly useMock = signal(false);
  protected readonly loadingProgress = signal('Loading Unity...');

  private unityInstance: IUnityInstance | null = null;

  async ngOnInit(): Promise<void> {
    const loaderUrl = `${UNITY_BUILD_PATH}/Build/${UNITY_BUILD_PATH}.loader.js`;
    const buildExists = await this.checkBuildExists(loaderUrl);

    if (buildExists) {
      await this.loadRealUnity(loaderUrl);
    } else {
      this.useMock.set(true);
      const mock = createMockUnityInstance();
      this.unityInstance = mock;
      this.bridge.setUnityInstance(mock);
      this.isLoaded.set(true);
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

    // Dynamically load the Unity loader script
    await new Promise<void>((resolve, reject) => {
      const script = document.createElement('script');
      script.src = loaderUrl;
      script.onload = () => resolve();
      script.onerror = () => reject(new Error('Failed to load Unity loader'));
      document.body.appendChild(script);
    });

    const canvas = this.canvasRef().nativeElement;
    const buildPath = `${UNITY_BUILD_PATH}/Build`;
    const buildName = UNITY_BUILD_PATH;

    // Detect compression format
    const dataExt = await this.detectExtension(`${buildPath}/${buildName}.data`);
    const frameworkExt = await this.detectExtension(`${buildPath}/${buildName}.framework.js`);
    const wasmExt = await this.detectExtension(`${buildPath}/${buildName}.wasm`);

    const config = {
      dataUrl: `${buildPath}/${buildName}.data${dataExt}`,
      frameworkUrl: `${buildPath}/${buildName}.framework.js${frameworkExt}`,
      codeUrl: `${buildPath}/${buildName}.wasm${wasmExt}`,
      streamingAssetsUrl: `${UNITY_BUILD_PATH}/StreamingAssets`,
      companyName: 'UnityAngularBridge',
      productName: 'Example',
      productVersion: '1.0.0',
    };

    try {
      /* eslint-disable @typescript-eslint/no-explicit-any */
      const createUnityInstance = (window as any).createUnityInstance;
      const instance = await createUnityInstance(canvas, config, (progress: number) => {
        this.loadingProgress.set(`Loading Unity... ${Math.round(progress * 100)}%`);
      });

      this.unityInstance = instance;
      this.bridge.setUnityInstance(instance);
      this.isLoaded.set(true);
    } catch (err) {
      console.error('Unity WebGL failed to load, falling back to mock:', err);
      this.useMock.set(true);
      const mock = createMockUnityInstance();
      this.unityInstance = mock;
      this.bridge.setUnityInstance(mock);
    } finally {
      this.isLoading.set(false);
      this.isLoaded.set(true);
    }
  }

  /** Check which compression extension a file has (.br, .gz, or none). */
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
