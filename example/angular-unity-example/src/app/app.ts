import { ChangeDetectionStrategy, Component } from '@angular/core';
import { UnityViewport } from './components/unity-viewport';
import { UnityControls } from './components/unity-controls';
import { ObjectList } from './components/object-list';

@Component({
  selector: 'app-root',
  imports: [UnityViewport, UnityControls, ObjectList],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {}
