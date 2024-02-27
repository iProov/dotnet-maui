![iProov: Flexible authentication for identity assurance](images/banner.jpg)

# iProov Biometrics .NET SDK

## Table of contents

- [Introduction](#introduction)
- [Requirements](#requirements)
- [Repository contents](#repository-contents)
- [Registration](#registration)
- [Installation](#installation)
- [Get Started](#get-started)
- [Xamarin.Android](#xamarin--android)
- [API Client](#api-client)
- [Sample code](#sample-code)

## Introduction

The iProov .NET SDK enables you to integrate iProov into your .NET Android / iOS / MAUI projects. The iProov's native SDK for [iOS](https://github.com/iProov/ios) (Swift) and [Android](https://github.com/iProov/android) (Java / Kotlin) are wrapped behind a .NET interface for their use within .NET apps. Given the differences in the implementation between these .NET interfaces, the iProov.NET.MAUI wraps both interfaces into a unified .NET version of the iProov API to ease the integration of the iProov Biometric SDKs in MAUI developments. 

We also provide a .NET API Client written in C# to call our [REST API v2](https://eu.rp.secure.iproov.me/docs.html) from a .NET Standard Library, which can be used from your Xamarin app to request tokens directly from the iProov API (note that this is not a secure way of getting tokens, and should only be used for demo/debugging purposes).

This documentation is focused on the **iProov.NET.MAUI** package. There's nuget-specific documentation available for [iProov.NET.Android](https://github.com/iProov/dotnet-maui/tree/master/Nuget%20Packages/iProov.NET.Android/) and [iProov.NET.iOS](https://github.com/iProov/dotnet-maui/tree/master/Nuget%20Packages/iProov.NET.iOS/) packages.

## Requirements

- NET 8 (net8-android;net8-ios)
- iOS 12 and above
- Android API Level 21 (Android 5 Lollipop) and above


## Repository contents

The iProov Xamarin SDK is provided via this repository, which contains the following:

- **README.md** - This document
- **NuGet Packages** - Directory containing the NuGet packages for iProov.NET.Android, iProov.NET.iOS & iProov.NET.MAUI
- **APIClient** - C# project with the source code for the .NET API Client
- **ExampleAppMAUI** - Sample code demonstrating use of the iProov.NET.MAUI together with the .NET API Client

## Registration

You can obtain API credentials by registering on the [iProov Partner Portal](https://www.iproov.net).

## Installation

The **iProov.NET.MAUI** library is available at [nugets.org](https://www.nuget.org/packages/iProov.NET.MAUI/). Hence, you can add the package to your project either:

1. Using the NuGet Package Manager, and adding the iProov.NET.MAUI package to your project from there. For further instructions on how to install and manage packages, [see here](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio).

> If you want to use the nuget package from a local source, make sure to add the folder where you store the nuget packages as a Nuget source in Visual Studio > Preferences

2. Edit your `.csproj` file and add the `<PackageReference>` to the nuget package

 ```
 <ItemGroup>
   <PackageReference Include="iProov.NET.MAUI" Version="1.0.0" />
 </ItemGroup>
 ```

## Get Started

### Get a Token

Obtain these tokens:

- A **verify** token for logging in an existing user
- An **enrol** token for registering a new user

See the [REST API documentation](https://secure.iproov.me/docs.html) for details about how to generate tokens.

> **TIP:** In a production app you typically obtain tokens via a server-to-server back-end call. For demos and testing, iProov provides .NET sample code for obtaining tokens via [iProov API v2](https://eu.rp.secure.iproov.me/docs.html) with our open-source [API Client](https://github.com/iProov/dotnet-maui/tree/master/APIClient).


### Launch the SDK

#### 1. Create and instance of IProovWrapper

 ```csharp
	private IProovWrapper wrapper = new IProovWrapper();
 ```

#### 2. Listening to IProovStates

To monitor the progress of an iProov claim and receive the result you need an instance of `IProovWrapper.IStateListener` interface. So, you can either create a private class which implements this interface to handle the callbacks from the SDK, or you can implement the interface in your `Activity` class.

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
		// Called multiple time as the progress updates. You could update a determinate progress indicator.
	}

	public void OnCanceled(Canceler canceler)
	{
		// The user canceled iProov, either by pressing the close button at the top of the screen, or sending
		// the app to the background. (canceler == Canceler.User)
		// Or, the app canceled (event.canceler == Canceler.App) by canceling the subscription to the 
		// Stream returned from IProov.launch().
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
		// A FailureReason value is provided to identify the reason as to why the claim failed. Some reasons also provide an optional description (as a string).
		// When enabled for your service provider, failure.frame contains the bytes of an image containing a single frame of the user

		FailureReason reason = failure.reason;
		string? description = failure.description;
		byte[]? frame = failure.frame;
	}

	public void OnSuccess(byte[]? frame)
	{
		// The user was successfully verified/enrolled.
		// You must always independently validate the token server-side (using the /validate API call) before performing any authenticated user actions.
		// When enabled for your service provider, frame contains the bytes of an image containing a single frame of the user
	}
}
 ```

#### 3. Launch a Claim

To launch a Claim you need to provide a `token`, a `userId`, the websocket url of the service provider you are using and an `IStateListener`. Additionally you can provide an instance of `IProovOptions` (see [below](#options)) to customize the user experience.

 ```csharp
	IProovWrapper wrapper = new IProovWrapper();
	IProovListener listener = new IProovListener();

	private void launchIProov(string token, string userId)
	{
		var options = new IProovOptions();
		// Here you can customize any IProovOption

		wrapper.LaunchIProov(token, userId, "wss://eu.rp.secure.iproov.me/ws", listener, options);
    }
 ```



# \<Temporary\> From flutter doc
## Options

The `Options` class allows iProov to be customized in various ways. These can be specified by passing the optional `options:` named parameter in `IProov.launch()`.

Most of these options are common to both Android and iOS, however, some are Android-only.

For full documentation, please read the respective [iOS](https://github.com/iProov/ios#options) and [Android](https://github.com/iProov/android#customize-the-user-experience) native SDK documentation.

A summary of the support for the various SDK options in Flutter is provided below. All options are nullable and any options not set will default to their platform-defined default value.

| Option | Type | iOS | Android |
|---|---|---|---|
| `filter` | `Filter?` [(See filter options)](#filter-options)| ✅ | ✅ |
| `titleTextColor` | `Color?` | ✅ | ✅ |
| `promptTextColor` | `Color?` | ✅ | ✅ |
| `closeButtonTintColor` | `Color?` | ✅ | ✅ |
| `closeButtonImage` | `Image?` | ✅ | ✅ |
| `title` | `String?` | ✅ | ✅ |
| `fontPath` (*)| `String?` | ✅  | ✅ |
| `logoImage` | `Image?` | ✅ | ✅ |
| `promptBackgroundColor` | `Color?` | ✅ | ✅ |
| `promptRoundedCorners` | `bool?` | ✅ | ✅ |
| `surroundColor` | `Color?` | ✅ | ✅ |
| `certificates` | `List<String>?` | ✅ | ✅ |
| `timeout` | `Duration?` | ✅ | ✅ |
| `enableScreenshots` | `bool?` |  | ✅ |
| `orientation` | `Orientation?` |  | ✅ |
| `camera` | `Camera?` |  | ✅ |
| `headerBackgroundColor` | `Color?` | ✅ | ✅ |
| `disableExteriorEffects` | `bool?` | ✅ | ✅ |
|**`genuinePresenceAssurance`** | `GenuinePresenceAssuranceOptions?` |  |  |
| ↳ `readyOvalStrokeColor` | `Color?` | ✅ | ✅ |
| ↳ `notReadyOvalStrokeColor` | `Color?` | ✅ | ✅ |
|**`livenessAssurance`** | `LivenessAssuranceOptions?` |  |  |
| ↳ `ovalStrokeColor` | `Color?` | ✅ | ✅ |
| ↳ `completedOvalStrokeColor` | `Color?` | ✅ | ✅ |

(*) Fonts should be added to your Flutter app (TTF or OTF formats are supported). Note that the font filename must match the font name.

Example:
```dart
const options = Options(fontPath: 'fonts/Lobster-Regula.ttf');
```

### Filter Options

The SDK supports two different camera filters:

#### `LineDrawingFilter`

`LineDrawingFilter` is iProov's traditional "canny" filter, which is available in 3 styles: `.shaded` (default), `.classic` and `.vibrant`.

The `foregroundColor` and `backgroundColor` can also be customized.

Example:

```dart
const options = Options(
      filter: LineDrawingFilter(
          style: LineDrawingFilterStyle.vibrant,
          foregroundColor: Colors.black,
          backgroundColor: Colors.white
      ),
    );
```

#### `NaturalFilter`

`NaturalFilter` provides a more direct visualization of the user's face and is available in 2 styles: `.clear` (default) and `.blur`.

Example:

```dart
const options = Options(
      filter: NaturalFilter(
          style: NaturalFilterStyle.clear
      ),
    );
```

> **Note**: `NaturalFilter` is available for Liveness Assurance claims only. Attempts to use `NaturalFilter` for Genuine Presence Assurance claims will result in an error.

## Handling errors

All errors from the native SDKs are re-mapped to Flutter exceptions:

| Exception                         | iOS | Android | Description                                                                                                                      |
| --------------------------------- | --- | ------- | -------------------------------------------------------------------------------------------------------------------------------- |
| `CaptureAlreadyActiveException`   | ✅   | ✅       | An existing iProov capture is already in progress. Wait until the current capture completes before starting a new one.           |
| `NetworkException`                    | ✅   | ✅       | An error occurred with the video streaming process. Consult the `message` value for more information.                            |
| `CameraPermissionException`           | ✅   | ✅       | The user disallowed access to the camera when prompted. You should direct the user to re-enable camera access.                   |
| `ServerException`                 | ✅   | ✅       | A server-side error/token invalidation occurred. The associated `message` will contain further information about the error.      |
| `UnexpectedErrorException`        | ✅   | ✅       | An unexpected and unrecoverable error has occurred. These errors should be reported to iProov for further investigation.         |
| `UnsupportedDeviceException`         |✅   | ✅         | Device is not supported.|
| `ListenerNotRegisteredException`  |     | ✅       | The SDK was launched before a listener was registered.                                                                           |
| `MultiWindowUnsupportedException` |     | ✅       | The user attempted to iProov in split-screen/multi-screen mode, which is not supported.                                          |
| `CameraException`                 |     | ✅       | An error occurred acquiring or using the camera. This could happen when a non-phone is used with/without an external/USB camera. |
| `FaceDetectorException`           |     | ✅       | An error occurred with the face detector.                                                                                        |
| `InvalidOptionsException`         |     | ✅       | An error occurred when trying to apply your options.|
| `UserTimeoutException`         |✅   |          | The user has taken too long to complete the claim.|

# \<\/Temporary\>

## API Client

The .NET API client provides a convenient wrapper to call iProov's REST API v2 from a .NET Standard Library. It is a useful tool to assist with testing, debugging and demos, but should not be used in production mobile apps. You could also adapt this code to run on your back-end to perform server-to-server calls.

> ⚠️ **SECURITY NOTICE:** Use of the .NET API Client requires providing it with your API secret. **You should never embed your API secret within a production app.**

### Functionality

The .NET API Client supports the following functionality:

- `GetToken()` - Get an enrol/verify token
- `EnrolPhoto()` - Perform a photo enrolment (either from an electronic or optical image). The image must be provided in JPEG format.
- `Validate()` - Validate an existing token against the provided User ID.
- `EnrolPhotoAndGetVerifyToken()` - A convenience method which first gets an enrolment token, then enrols the photo against that token, and then gets a verify token for the user to iProov against.

### Installation

To add the .NET API Client to your project, add it as a sub-project to your solution, and then [add a reference](https://docs.microsoft.com/en-us/visualstudio/mac/managing-references-in-a-project?view=vsmac-2019) to the **APIClient** project from your app project.

You will also need to [add](https://docs.microsoft.com/en-us/visualstudio/mac/nuget-walkthrough?view=vsmac-2019) the **Newtonsoft.Json** NuGet package to your project as well.

You can now import the API Client with `using iProov.APIClient;`.

### Usage examples

We will now run through a couple of common use-cases with the API Client. All the API Client source code is provided, so you can understand how it works and adapt it accordingly.

#### Getting a token

The most basic thing you can do with the API Client is get a token to either enrol or verify a user, using either iProov's Genuine Presence Assurance or Liveness Assurance.

This is achieved as follows:

```csharp
var token = await apiClient.GetToken(AssuranceType.GenuinePresence, ClaimType.Enrol, "{{ user id }}");
```

You can then launch the iProov SDK with this token.

#### Performing a photo enrol (on iOS)

To photo enrol a user, you would first generate an enrolment token, then enrol the photo against the user, then generate a verification token.

Fortunately the .NET API Client provides a helper method which wraps all three calls into one convenience method.

The first thing you will need to do is convert your iOS native `UIImage` into a .NET `byte[]` which can be handled by the cross-platform API Client:

```csharp
var uiImage = UIImage.FromBundle("image.png");  // (For example)
var jpegData = uiImage.AsJPEG();
byte[] jpegBytes = new byte[jpegData.Length];
Marshal.Copy(jpegData.Bytes, jpegBytes, 0, Convert.ToInt32(jpegData.Length));
```

You can now pass the `jpegBytes` to the `EnrolPhotoAndGetVerifyToken()` method:

```csharp
string token = await apiClient.EnrolPhotoAndGetVerifyToken(guid, jpegBytes, PhotoSource.oid);
```

You can now launch the iProov SDK with this token to complete the photo enrolment.

## Sample code

For a simple iProov experience that is ready to run out-of-the-box, check out the [Example  project](https://github.com/iProov/xamarin/tree/master/Example) for Xamarin.iOS and Xamarin.Android which also makes use of the .NET API Client.

### Usage

1. Copy the file _Shared/Credentials.example.cs_ to _Shared/Credentials.cs_ and provide your API key & secret.
2. Open the Example solution in Visual Studio.
3. Right click the root project and "Restore NuGet Packages" to ensure all NuGet packages are ready for usage.
4. Run the iOSExample or AndroidExample project on a supported iOS or Android device respectively.

> NOTE: iProov is not supported on the iOS or Android simulator, you must use a physical device in order to iProov.