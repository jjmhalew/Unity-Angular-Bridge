import { IUnityInstance } from '../models/unity-instance';

/** Options for customizing the mock Unity instance behavior. */
export interface MockUnityOptions {
  /**
   * Called whenever `SendMessage` is invoked on the mock instance.
   * Use this to simulate Unity responses by calling window callbacks, etc.
   */
  onSendMessage?: (gameObjectName: string, methodName: string, data?: unknown) => void;
}

/**
 * Creates a basic mock `IUnityInstance` for development without a Unity WebGL build.
 *
 * By default, it logs all `SendMessage` calls to the console.
 * Pass `options.onSendMessage` to add project-specific behavior (e.g., simulating
 * window callbacks that Unity would normally trigger via jslib).
 *
 * @example
 * ```ts
 * const mock = createMockUnityInstance({
 *   onSendMessage: (obj, method, data) => {
 *     if (method === 'LoadObject') {
 *       (window as any)['sendSelectedObjectFromUnity']?.(data);
 *     }
 *   },
 * });
 * ```
 */
export function createMockUnityInstance(options?: MockUnityOptions): IUnityInstance {
  return {
    SendMessage(gameObjectName: string, methodName: string, data?: unknown): void {
      console.log(`[Unity Mock] ${gameObjectName}.${methodName}(${data ?? ''})`);
      options?.onSendMessage?.(gameObjectName, methodName, data);
    },

    SetFullscreen(value: number): void {
      console.log(`[Unity Mock] SetFullscreen(${value})`);
    },

    Quit(): Promise<unknown> {
      console.log('[Unity Mock] Quit');
      return Promise.resolve();
    },
  };
}
