import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NgxUnityViewport, type IUnityInstance } from 'ngx-unity';
import { UnityBridgeService } from '../services/unity-bridge.service';
import { createProjectMockUnityInstance } from '../services/mock-unity';

/**
 * Wraps the library's `<ngx-unity-viewport>` with project-specific wiring:
 * connects the Unity instance to the bridge service and provides the project mock.
 */
@Component({
  selector: 'app-unity-viewport',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgxUnityViewport],
  template: `
    <ngx-unity-viewport
      buildPath="unity"
      height="400px"
      [mockFactory]="mockFactory"
      (instanceReady)="onInstanceReady($event)" />
  `,
})
export class UnityViewport {
  private readonly bridge = inject(UnityBridgeService);

  protected readonly mockFactory = () => createProjectMockUnityInstance();

  protected onInstanceReady(instance: IUnityInstance): void {
    this.bridge.setUnityInstance(instance);
  }
}
