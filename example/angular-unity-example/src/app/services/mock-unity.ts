import { IUnityInstance } from '../generated/unity-client';

/**
 * Creates a mock Unity instance for demo purposes.
 *
 * In a real application, you would get the IUnityInstance from the Unity WebGL loader:
 *   createUnityInstance(canvas, config).then((instance) => { ... });
 *
 * This mock simulates Unity behavior by calling window callbacks
 * (the same mechanism Unity WebGL uses via BrowserInteractions.jslib).
 */
export function createMockUnityInstance(): IUnityInstance {
  const objects = ['Cube-001', 'Sphere-002', 'Cylinder-003', 'Plane-004'];

  return {
    SendMessage(gameObjectName: string, methodName: string, data?: unknown): void {
      console.log(`[Unity Mock] ${gameObjectName}.${methodName}(${data ?? ''})`);

      /* eslint-disable @typescript-eslint/no-explicit-any */
      const win = window as any;

      // Simulate Unity responding to commands by calling window callbacks
      if (methodName === 'LoadObject') {
        setTimeout(() => {
          const cb = win['sendSelectedObjectFromUnity'] as ((id: string) => void) | undefined;
          cb?.(data as string);
        }, 300);
      }

      if (methodName === 'ResetScene') {
        setTimeout(() => {
          const listCb = win['sendObjectsListFromUnity'] as ((ids: string) => void) | undefined;
          listCb?.(objects.join('|'));
        }, 300);
        setTimeout(() => {
          const readyCb = win['sendSceneReadyFromUnity'] as (() => void) | undefined;
          readyCb?.();
        }, 500);
      }
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
