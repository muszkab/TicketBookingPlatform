import { APP_BUILD_DATE, APP_COMMIT, APP_VERSION } from './version.generated';

export const environment = {
  production: false,
  apiBasePath: 'https://localhost:5001',
  appVersion: APP_VERSION,
  appCommit: APP_COMMIT,
  appBuildDate: APP_BUILD_DATE
};
