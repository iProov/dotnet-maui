![iProov: Flexible authentication for identity assurance](images/banner.jpg)

# iProov Biometrics .NET SDK

## Table of contents

- [Introduction](#introduction)
- [Repository contents](#repository-contents)
- [Upgrading from earlier versions](#upgrading-from-earlier-versions)
- [Registration](#registration)
- [Xamarin.iOS](#xamarin--ios)
- [Xamarin.Android](#xamarin--android)
- [API Client](#api-client)
- [Sample code](#sample-code)

## Introduction

The iProov .NET SDK enables you to integrate iProov into your .NET Android / iOS / MAUI projects. The iProov's native SDK for [iOS](https://github.com/iProov/ios) (Swift) and [Android](https://github.com/iProov/android) (Java / Kotlin) are wrapped behind a .NET interface for their use within .NET apps. Given the differences in the implementation between these .NET interfaces, the iProov.NET.MAUI wraps both interfaces into a unified .NET version of the iProov API to ease the integration of the iProov Biometric SDKs in MAUI developments. 

We also provide a .NET API Client written in C# to call our [REST API v2](https://eu.rp.secure.iproov.me/docs.html) from a .NET Standard Library, which can be used from your Xamarin app to request tokens directly from the iProov API (note that this is not a secure way of getting tokens, and should only be used for demo/debugging purposes).

This documentation is focused on the **iProov.NET.MAUI** package. There's nuget-specific documentation available for [iProov.NET.Android](https://github.com/iProov/dotnet-maui/tree/master/Nuget%20Packages/iProov.NET.Android/) and [iProov.NET.iOS](https://github.com/iProov/dotnet-maui/tree/master/Nuget%20Packages/iProov.NET.iOS/) packages.

## iProov.NET.MAUI



## Repository contents

The iProov Xamarin SDK is provided via this repository, which contains the following:

- **README.md** - This document
- **NuGet Packages** - Directory containing the NuGet packages for Xamarin.iOS & Xamarin.Android
- **APIClient** - C# project with the source code for the .NET API Client
- **Example** - Sample code demonstrating use of the Xamarin.iOS & Xamarin.Android bindings together with the .NET API Client

## Upgrading from earlier versions

If you're already using an older version of the Xamarin SDK, consult the [Upgrade Guide](https://github.com/iProov/xamarin/wiki/Upgrade-Guide) for detailed information about how to upgrade your app.

## Registration

You can obtain API credentials by registering on the [iProov Partner Portal](https://www.iproov.net).


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