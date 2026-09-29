import { bootstrapApplication } from '@angular/platform-browser';
import { createAppConfig } from './app/app.config';
import { loadAppConfig } from './app/core/config/app-config';
import { App } from './app/app';

loadAppConfig()
  .then(() => bootstrapApplication(App, createAppConfig()))
  .catch((err) => console.error(err));
