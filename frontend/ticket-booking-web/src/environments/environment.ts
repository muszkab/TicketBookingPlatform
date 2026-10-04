import { APP_BUILD_DATE, APP_COMMIT, APP_VERSION } from './version.generated';

export const environment = {
  production: true,
  apiBasePath: '',
  appVersion: APP_VERSION,
  appCommit: APP_COMMIT,
  appBuildDate: APP_BUILD_DATE
};
