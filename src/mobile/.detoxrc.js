/** @type {Detox.DetoxConfig} */
module.exports = {
  testRunner: {
    args: { '$0': 'jest', config: 'e2e/jest.config.js' },
    jest: { setupTimeout: 120000 },
  },
  apps: {
    'ios.debug': {
      type: 'ios.app',
      binaryPath: 'ios/build/Build/Products/Debug-iphonesimulator/Upkilo.app',
      build: 'xcodebuild -workspace ios/Upkilo.xcworkspace -scheme Upkilo -configuration Debug -sdk iphonesimulator -derivedDataPath ios/build',
    },
    'android.debug': {
      type: 'android.apk',
      binaryPath: 'android/app/build/outputs/apk/debug/app-debug.apk',
      build: 'cd android && ./gradlew assembleDebug assembleAndroidTest -DtestBuildType=debug',
      reversePorts: [8081],
    },
  },
  devices: {
    'ios.sim': {
      type: 'ios.simulator',
      device: { type: 'iPhone 15' },
    },
    'android.emu': {
      type: 'android.emulator',
      device: { avdName: 'Pixel_5_API_33' },
    },
  },
  configurations: {
    'ios.sim.debug': {
      device: 'ios.sim',
      app: 'ios.debug',
    },
    'android.emu.debug': {
      device: 'android.emu',
      app: 'android.debug',
    },
  },
};
