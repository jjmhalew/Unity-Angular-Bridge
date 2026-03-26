/**
 * Represents a Unity WebGL instance created by `createUnityInstance()`.
 *
 * This interface matches the object returned by the Unity WebGL loader.
 * It is used by both real Unity builds and mock instances for development.
 */
export interface IUnityInstance {
  /**
   * Send a message to a Unity GameObject method.
   *
   * @param gameObjectName - Name of the target GameObject in the Unity scene.
   * @param methodName - Name of the public method on the attached MonoBehaviour.
   * @param data - Optional string or number parameter (Unity SendMessage limitation).
   */
  SendMessage(gameObjectName: string, methodName: string, data?: unknown): void;

  /** Enter or exit fullscreen mode (1 = enter, 0 = exit). */
  SetFullscreen(value: number): void;

  /** Quit the Unity instance and release resources. */
  Quit(): Promise<unknown>;
}
