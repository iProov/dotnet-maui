# iProov Face .NET MAUI SDK

## Table of contents

- [Requirements](#requirements)
- [Upgrading from 3.x](#upgrading-from-3x)
- [Introduction](#introduction)
- [Registration](#registration)
- [Installation](#installation)
- [Get a token](#get-a-token)
- [Launch the SDK](#launch-the-sdk)
- [Options](#options)
- [Failures](#failures)
- [Handling Errors](#handling-errors)
- [API Client](#api-client)
- [Example Project](#example-project)

## Requirements

- .NET 10 (net10.0-android & net10.0-ios)
- Android: Microsoft.Android.Sdk 36.1.53 or later
- iOS: Xcode 26 or later
- iOS 16 and above. The build does not check this, so an app with a lower minimum iOS version builds without a warning. Set your app's minimum to iOS 16 or later.
- Android API Level 29 (Android 10) and above

## Upgrading from 3.x

This version uses the iProov iOS SDK 14.0.0 and Android SDK 12.0.0. To upgrade an app from `iProov.NET.MAUI` 3.x to 4.x:

- Your app must target .NET 10 (net10.0-android and net10.0-ios). This version does not support .NET 9.
- Implement `OnSuccess()` without a parameter. The SDKs no longer return a frame of the user, so `IProovFailureResult.frame` is also removed.
- Remove `orientation` from your `IProovOptions`. The `Orientation` enum is removed.
- Rename `GenuinePressenceAssurance` to `GenuinePresenceAssurance`, `genuinePressenceAssurance` to `genuinePresenceAssurance`, the `UiListnener` argument of `LaunchIProov` to `UiListener`, and `enableScreenShots` to `enableScreenshots`.
- Set your iOS app's minimum to iOS 16 or later.
- On Android, we are phasing out support for <Android 10, so even though you can still set `minSdk` to 26, users on devices below Android 10 will be blocked. If you are affected, please contact support.
- If you do not set `filter`, the default is now `NaturalFilter` with the `Clear` style on both platforms.
- Handle the new `UnsupportedOSVersionException` and `UnsupportedSDKVersionException`. See [Handling errors](#handling-errors).
- A consent-required error now arrives as `ConsentRequiredException` instead of `UnexpectedErrorException`. It is for iProov internal use only, and you can safely ignore it.
- `ListenerNotRegisteredException` is removed. The wrapper never throws it, so delete any `catch` for it.
- `iProov.NET.MAUI` no longer depends on `Microsoft.Maui.Controls.Compatibility`. If your app uses its APIs, add a `PackageReference` to it in your own project.
- `iProov.NET.Android`, which this package brings in, no longer exposes the Android SDK's internal classes and fields, such as `EnvelopeOuterClass` and `IProovKt`. None of them is part of the Android SDK's documented API. For the full list, see "Upgrading from 11.x" in the `iProov.NET.Android` package guide.
- Read an error's detail from `message`. On iOS, `title` no longer includes it, and every exception now passes on the native message on both platforms.
- On Android, `GetKeyPair()` can now throw `UnexpectedErrorException`.
- `GenuinePresenceAssurance` has a new optional `accessibility` parameter, and every exception constructor now takes an optional `message`. Neither needs a change in your own code. A library built against 3.x that creates these objects fails with `MissingMethodException` until it is rebuilt against 4.x.

If you use `iProov.NET.iOS` or `iProov.NET.Android` directly, see the native [iOS](https://github.com/iProov/ios/wiki/Upgrade-Guide) and [Android](https://github.com/iProov/android/wiki/Upgrade-Guide) upgrade guides.

## Introduction

The iProov .NET MAUI SDK enables you to integrate iProov into your .NET Android / iOS / MAUI projects. iProov's native SDKs for [iOS](https://github.com/iProov/ios) (Swift) and [Android](https://github.com/iProov/android) (Java / Kotlin) are wrapped behind a .NET interface for their use within .NET apps. Given the differences in the implementation between these .NET interfaces, iProov.NET.MAUI wraps both interfaces into a unified .NET version of the iProov API to ease the integration of the iProov Face SDKs in MAUI developments. 

## Registration

You can obtain API credentials by registering on the [iProov Portal](https://portal.iproov.com/).

## Installation

The **iProov.NET.MAUI** library is available on [nuget.org](https://www.nuget.org/packages/iProov.NET.MAUI/). You can add it to your project either:

1. Using the NuGet Package Manager. For instructions on installing and managing packages, [see here](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio).
2. Or by adding a `<PackageReference>` to your `.csproj` file:

```xml
<ItemGroup>
  <PackageReference Include="iProov.NET.MAUI" Version="4.0.0" />
</ItemGroup>
```

## Get a Token

There are two types of tokens:

- A **verify** token for logging in an existing user
- An **enrol** token for registering a new user

See the [REST API documentation](https://secure.iproov.me/docs.html) for details about how to generate tokens.

> **TIP:** In a production app you typically obtain tokens via a server-to-server back-end call. 

> For demos and testing, iProov provides .NET sample code for obtaining tokens via [iProov API v2](https://eu.rp.secure.iproov.me/docs.html) with our open-source [API Client](https://github.com/iProov/dotnet-maui/tree/master/APIClient).


## Launch the SDK

### 1. Create an instance of IProovWrapper

 ```csharp
	private IProovWrapper wrapper = new IProovWrapper();
 ```

### 2. Listening to IProovStates

To monitor the progress of an iProov claim and receive the result you need an instance of `IProovWrapper.IStateListener` interface. So, you will have to implement this interface in one of your classes, or create a new one, as shown in the following example.

 ```csharp
public class IProovListener: IProovWrapper.IStateListener {

	public void OnConnecting()
	{
		// Called when the SDK is connecting to the server. You could provide an indeterminate
		// progress indication to let the user know that the connection is being established.
	}

	public void OnConnected()
	{
		// The SDK has connected and the iProov user interface is now displayed. You
		// could hide any progress indicator at this point.
	}

	public void OnProcessing(double progress, string? message)
	{
		// The SDK updates your app with the streaming progress to the server and the user authentication.
		// Called multiple times as the progress updates. You could update a determinate progress indicator.
	}

	public void OnCanceled(Canceler canceler)
	{
		// The user canceled iProov, either by pressing the close button at the top of the screen, or sending
		// the app to the background. (canceler == Canceler.User)
		// Or, the claim was canceled by the integrator by calling the Cancel() method from the IProovWrapper instance (canceler == Canceler.Integration)
		// You should use this to determine the next step in your flow.
	}

	public void OnError(IProovException exception)
	{
		// The user was not successfully verified/enrolled due to an error (e.g. lost internet connection).
		// You will be provided with an Exception (see below).
		// It will be called once, or never.
		// An IProovException contains a title that describes the exception, and may also contain a message.
		string title = exception.title;
		string? message = exception.message;
	}

	public void OnFailure(IProovFailureResult failure)
	{
		// The user was not successfully verified/enrolled as their identity could not be verified.
		// Or there was another issue with their verification/enrollment.
		// You might provide feedback to the user as to how to retry. 
		// An IProovFailureResult provides an array of IProovFailureReason as to why the claim failed.
		// Each IProovFailureReason contains:
		//   - FailureReason value to identify a reason 
		//   - Optional description (as a string on iOS and as a resource ID on Android) that can be 
		//     presented to the user to increase their chances of iProoving successfully next time.

		IProovFailureReason[] reasons = failure.reasons;
	}

	public void OnSuccess()
	{
		// The user was successfully verified/enrolled.
		// You must always independently validate the token server-side (using the /validate API call) before performing any authenticated user actions.
	}
}
 ```
#### 2.1 Listening to UIStates (Optional)

To monitor the state of the iProov UI you need an instance of the `IProovWrapper.IUIStateListener` interface. You will have to implement this interface in one of your classes, or create a new one like in the following example.
```csharp
public class IProovUIListener: IProovWrapper.IUIStateListener
{

    public void OnNotStarted()
    {
      // Called when the session has started
      // but the UI hasn't
    }

    public void OnStarted()
    {
      // Called when the UI starts
    }

    public void OnEnded()
    {
      // Called when the UI is dismissed
    }
}
```

Listening to these events is not necessary to use the iProov SDK. Nevertheless, if you want to monitor the UI states, the next section shows how to use it.

### 3. Launch a Claim

To launch a claim you need to provide a `token`, the websocket url of the service provider you are using and an `IStateListener` instance. Additionally you can provide an instance of `IProovOptions` (see [below](#options)) to customize the user experience. Also if you want to monitor the UI states you can provide an instance of `IUIStateListener`.

 ```csharp
IProovWrapper wrapper = new IProovWrapper();
IProovListener listener = new IProovListener();
// Optional: Use it to monitor UI state
IProovUIListener uiListener = new IProovUIListener();

private void launchIProov(string token, string userId)
{
	var options = new IProovOptions();
	// Here you can customize any IProovOption

	wrapper.LaunchIProov(
		Token: token,
		WebsocketURL: "wss://eu.rp.secure.iproov.me/ws",
		Listener: listener,
		Options: options, // optional
		UiListener: uiListener // optional
		);
}
 ```

### _\** Running on iOS_

For iProov to work on iOS devices, you need to add a `Privacy - Camera Usage Description` entry to your Info.plist file (`<Your_App_Folder>/Platforms/iOS/Info.plist`) with the reason why your app requires camera access (e.g. "To iProov you in order to verify your identity.")

## Canceling a Session

Under normal circumstances, the user will be in control of the completion of the iProov scan, i.e. they will either complete the scan, or use the close button to cancel. In some cases, you (the integrator) may wish to cancel the iProov scan programmatically, for example in response to a timeout or change of conditions in your app.

To cancel the iProov SDK, you first need to hold a reference to the `IProovWrapper` object and you can then call `Cancel()` on it.

```csharp
IProovWrapper wrapper = new IProovWrapper();
IProovListener listener = new IProovListener();
var options = new IProovOptions();

wrapper.LaunchIProov(token, "wss://eu.rp.secure.iproov.me/ws", listener, options);

// Example - cancel the wrapper session after 10 sec
System.Timers.Timer timer = new System.Timers.Timer(10000);
timer.Elapsed += (sender, e) => 
{
    wrapper.Cancel();
};
timer.Enabled = true;
```

## Options

The `IProovOptions` class allows iProov to be customized in various ways. These can be specified by passing an instance of `IProovOptions` in the `LaunchIProov()` method.

Most of these options are common to both Android and iOS. However, some options are platform-specific.

For full documentation, please read the respective [iOS](https://github.com/iProov/ios#options) and [Android](https://github.com/iProov/android#customize-the-user-experience) native SDK documentation.

A summary of the support for the various SDK options in **iProov.NET.MAUI** is provided below. All options are nullable and any options not set will default to their platform-defined default value.

| Option | Type | iOS | Android |
|---|---|---|---|
| `filter` | `Filter?` [(See filter options)](#filter-options)| ✅ | ✅ |
| `titleTextColor` | `Color?` | ✅ | ✅ |
| `promptTextColor` | `Color?` | ✅ | ✅ |
| `closeButtonTintColor` | `Color?` | ✅ | ✅ |
| `closeButtonImage` | `byte[]?` | ✅ | ✅ |
| `title` | `string?` | ✅ | ✅ |
| `fontPath` | `string?` | ✅  | ✅ |
| `logoImage` | `byte[]?` | ✅ | ✅ |
| `promptBackgroundColor` | `Color?` | ✅ | ✅ |
| `promptRoundedCorners` | `bool?` | ✅ | ✅ |
| `surroundColor` | `Color?` | ✅ | ✅ |
| `certificates` | `IList<string>?` [(See certificate pinning)](#certificate-pinning) | ✅ | ✅ |
| `timeout` | `int?` | ✅ | ✅ |
| `enableScreenshots` | `bool?` |  | ✅ |
| `headerBackgroundColor` | `Color?` | ✅ | ✅ |
| `disableExteriorEffects` | `bool?` | ✅ | ✅ |
| `stringsTable` | `string?` | ✅ | |
| `stringsBundle` | `string?` | ✅ | |
|**`genuinePresenceAssurance`** | `GenuinePresenceAssurance?` |  |  |
| ↳ `readyOvalStrokeColor` | `Color?` | ✅ | ✅ |
| ↳ `notReadyOvalStrokeColor` | `Color?` | ✅ | ✅ |
| ↳ `controlYPosition` | `bool?` | ✅ | ✅ |
| ↳ `controlXPosition` | `bool?` | ✅ | ✅ |
| ↳ `scanningPrompts` | `bool?` | ✅ | ✅ |
| ↳ `accessibility.isColorEnabled` | `bool?` | ✅ | ✅ |
| ↳ `accessibility.isSoundEnabled` | `bool?` | ✅ | ✅ |
| ↳ `accessibility.isVibrationEnabled` | `bool?` | ✅ | ✅ |
|**`livenessAssurance`** | `LivenessAssurance?` |  |  |
| ↳ `ovalStrokeColor` | `Color?` | ✅ | ✅ |
| ↳ `completedOvalStrokeColor` | `Color?` | ✅ | ✅ |

### Filter Options

The SDK supports two different camera filters:

#### `LineDrawingFilter`

`LineDrawingFilter` is iProov's traditional "canny" filter, which is available in 3 styles: `.Shaded` (default), `.Classic` and `.Vibrant`.

The `foregroundColor` and `backgroundColor` can also be customized.

Example:

```csharp
var options = new IProovOptions();

options.filter = new IProovOptions.LineDrawingFilter(
                style: LineDrawingFilterStyle.Shaded,
                foregroundColor: Colors.Black,
                backgroundColor: Colors.White);
```

#### `NaturalFilter`

`NaturalFilter` provides a more direct visualization of the user's face and is available in 2 styles: `.Clear` (default) and `.Blur`.

Example:

```csharp
var options = new IProovOptions();

options.filter = new IProovOptions.NaturalFilter(style: NaturalFilterStyle.Clear);
```

> **Note**: If `filter` is not set, both platforms use `NaturalFilter` with the `Clear` style, for Genuine Presence Assurance and Liveness Assurance claims.

### Certificate Pinning

If you are using your own reverse-proxy, you will need to use this option to pin to your own certificates.

Certificates should be passed as a list of `string`, where each `string` is the base64-encoded SHA-256 hash of a certificate's Subject Public Key Info. You can load certificates as follows:

```csharp
options.certificates = new List<string>
{
    "O8qZKEXWWkMPISIpvB7DUow++JzIW2g+k9z3U/l5V94=",
    "Sc7wL4FfOw8ued5w3JOSzu5MzB471PfqdyN4hnMYbX4="
};
```

To get Subject Public Key Info from a `.crt` file, use the following command:

```sh
$ openssl x509  -in cert.crt -pubkey -noout | openssl pkey -pubin -outform der | openssl dgst -sha256 -binary | openssl enc -base64
```

To get Subject Public Key Info from a `.der` file, use the following command:

```sh
$ openssl x509 -inform der -in cert.der -pubkey -noout | openssl pkey -pubin -outform der | openssl dgst -sha256 -binary | openssl enc -base64
```

When multiple certificates are passed, as long as the server matches **any** of the certificates, the connection will be allowed. On iOS, pinning is performed against the **public key** of the certificate. On Android, it is performed against the **whole** of the certificate.

## Failures

A **failure** occurs when iProov successfully processes a claim but the user's identity cannot be verified. Crucially, this differs from an error, where the capture itself failed due to a system failure.

The failure results in an `IProovFailureResult`, whose `reasons` property is a list of up to three `IProovFailureReason`. Each `IProovFailureReason` has the following properties:

- `reason` - Of type `FailureReason`, an enum that represents the feedback code.
- `description` - `string?` with a human-readable description of the reason. Only returned when running on iOS.
- `descriptionId` - `int` with the string resource ID for the human-readable description of the reason. Only returned when running on Android.

> You should present the description to the user, as it may provide an informative hint for the user to increase their chances of iProoving successfully next time.

The available failure reasons are as follows:

| `FailureReason` value | `description` (English)                                                   |
|-----------------------|---------------------------------------------------------------------------|
| `SystemError`         | System error.                                                             |
| `LightingIssues`      | Make sure your face is well lit and free from glare.                      |
| `FaceNotFound`        | Align your face in the oval and then try to keep still.                   |
| `Timeout`             | System timeout.                                                           |
| `Eyewear`             | Remove your eyewear.                                                      |
| `DeviceIssue`         | Try a different device.                                                   |
| `Rejected`            | Transaction could not be completed.                                       |
| `BackgroundIssue`     | Move to a different location with a neutral background.                   |
| `MotionIssue`         | Try a different device.                                                   |
| `DeviceRestart`       | Please restart your device and try again.                                 |
| `FramesBlurry`        | Align your face in the oval and then try to keep still. Please try again. |
| `UserNotFound`        | Transaction could not be completed.                                       |
| `ProcessingFault`     | Please try again.                                                         |

The legacy codes below may still be returned in some scenarios (for example, older SDKs, or features not yet enabled for your service provider):

| `FailureReason` value | `description` (English)                                                                      | GPA | LA |
|-----------------------|----------------------------------------------------------------------------------------------|-----|----|
| `Unknown`             | Try again                                                                                    | ✅  | ❌ |
| `TooMuchMovement`     | Keep still                                                                                   | ✅  | ❌ |
| `TooBright`           | Move somewhere darker                                                                        | ✅  | ❌ |
| `TooDark`             | Move somewhere brighter                                                                      | ✅  | ❌ |
| `MisalignedFace`      | Keep your face in the oval                                                                   | ✅  | ❌ |
| `FaceTooFar`          | Move your face closer to the screen                                                          | ✅  | ❌ |
| `FaceTooClose`        | Move your face farther from the screen                                                       | ✅  | ❌ |
| `Sunglasses`          | Remove sunglasses                                                                            | ✅  | ❌ |
| `ObscuredFace`        | Make sure your whole face is visible and remove any accessories that might cover your face. | ✅  | ✅ |
| `EyesClosed`          | Make sure your eyes are open.                                                                | ✅  | ✅ |
| `MultipleFaces`       | Ensure only one person is visible.                                                           | ✅  | ✅ |

Key: ✅ = will be returned, ❌ = will not be returned

These are not an indication of tests being performed but only whether reasons of failure are reported.

## Handling errors

All errors from the native SDKs are re-mapped to `IProovException` exceptions:

| Exception                         | iOS | Android | Description                                                                                                                      |
| --------------------------------- | --- | ------- | -------------------------------------------------------------------------------------------------------------------------------- |
| `CaptureAlreadyActiveException`   | ✅   | ✅       | An existing iProov capture is already in progress. Wait until the current capture completes before starting a new one.           |
| `ConsentRequiredException`        | ✅   | ✅       | The user has not granted consent. For iProov internal use only. You can safely ignore. |
| `NetworkException`                    | ✅   | ✅       | An error occurred with the video streaming process. Consult the `message` value for more information.                            |
| `CameraPermissionException`           | ✅   | ✅       | The user disallowed access to the camera when prompted. You should direct the user to re-enable camera access.                   |
| `ServerException`                 | ✅   | ✅       | A server-side error/token invalidation occurred. The associated `message` will contain further information about the error.      |
| `UnexpectedErrorException`        | ✅   | ✅       | An unexpected and unrecoverable error has occurred. These errors should be reported to iProov for further investigation.         |
| `UnsupportedDeviceException`      | ✅   | ✅       | The device is not supported. Do not retry.|
| `UnsupportedOSVersionException`   | ✅   | ✅       | The operating system version is not supported. Do not retry.|
| `UnsupportedSDKVersionException`  | ✅   | ✅       | The SDK version is not supported. Do not retry.|
| `MultiWindowUnsupportedException` |     | ✅       | The user attempted to iProov in split-screen/multi-screen mode, which is not supported.                                          |
| `CameraException`                 |     | ✅       | An error occurred acquiring or using the camera. Applicable when using the external camera support.|
| `InvalidOptionsException`         |     | ✅       | An error occurred when trying to apply your options.|
| `UserTimeoutException`         |✅   |          | The user has taken too long to complete the claim.|

On Android, `GetKeyPair()` throws `UnexpectedErrorException` when the device cannot create or load its key pair.

## API Client

The [iProov .NET API Client](https://github.com/iProov/dotnet-maui/tree/master/APIClient) is a simple wrapper for the [iProov REST API v2](https://secure.iproov.me/docs.html) written in C#, for use in demo/PoC apps. For further details, see the [API Client documentation](https://github.com/iProov/dotnet-maui#api-client).

## Example Project

For a simple iProov experience that is ready to run out-of-the-box and uses the iProov API client, check out the [Example project](https://github.com/iProov/dotnet-maui/tree/master/ExampleAppMAUI). See [Example Project](https://github.com/iProov/dotnet-maui#example-project) in the repository's README for how to run it.

> **Warning**: The Example project uses the API client to directly fetch tokens on-device and this is inherently insecure. Production implementations of iProov should always obtain tokens securely from a server-to-server call.
