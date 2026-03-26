import { createMockUnityInstance, type IUnityInstance } from 'ngx-unity';

const MOCK_OBJECTS = ['Cube-001', 'Sphere-002', 'Cylinder-003', 'Plane-004'];

/**
 * Creates a project-specific mock Unity instance.
 *
 * Extends the library's base mock with simulated window callbacks that
 * mimic what the Unity WebGL build would do via BrowserInteractions.jslib.
 */
export function createProjectMockUnityInstance(): IUnityInstance {
  return createMockUnityInstance({
    onSendMessage(_gameObjectName, methodName, data) {
      /* eslint-disable @typescript-eslint/no-explicit-any */
      const win = window as any;

      if (methodName === 'LoadObject') {
        setTimeout(() => {
          const cb = win['sendSelectedObjectFromUnity'] as ((id: string) => void) | undefined;
          cb?.(data as string);
        }, 300);
      }

      if (methodName === 'ResetScene') {
        setTimeout(() => {
          const listCb = win['sendObjectsListFromUnity'] as ((ids: string) => void) | undefined;
          listCb?.(MOCK_OBJECTS.join('|'));
        }, 300);
        setTimeout(() => {
          const readyCb = win['sendSceneReadyFromUnity'] as (() => void) | undefined;
          readyCb?.();
        }, 500);
        setTimeout(() => {
          const reqCb = win['requestDataFromWebFromUnity'] as
            | ((query: string, respond: (result: string) => void) => void)
            | undefined;
          reqCb?.('scene-reset', (result: string) => {
            console.log(`[Unity Mock] Received callback response: ${result}`);
          });
        }, 600);
      }
    },
  });
}
