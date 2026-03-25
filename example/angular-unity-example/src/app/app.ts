import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { UnityBridgeService } from './services/unity-bridge.service';
import { UnityControls } from './components/unity-controls';
import { ObjectList } from './components/object-list';
import { createMockUnityInstance } from './services/mock-unity';

@Component({
  selector: 'app-root',
  imports: [UnityControls, ObjectList],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App implements OnInit {
  protected readonly bridge = inject(UnityBridgeService);

  ngOnInit(): void {
    // In a real app, you would get the Unity instance from the WebGL loader:
    //   createUnityInstance(canvas, config).then((instance) => {
    //     this.bridge.setUnityInstance(instance);
    //   });
    //
    // For this demo, we use a mock that simulates Unity behavior:
    const mockInstance = createMockUnityInstance();
    this.bridge.setUnityInstance(mockInstance);
  }
}
