import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app';
import { provideHttpClient } from '@angular/common/http';

// Обновленный appConfig
const updatedConfig = {
  ...appConfig,
  providers: [
    ...(appConfig.providers || []),
    provideHttpClient()
  ]
};

bootstrapApplication(AppComponent, updatedConfig)
  .catch((err) => console.error(err));