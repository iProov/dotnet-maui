# iProov.NET.iOS NuGet

## Introduction

The iProov.NET.iOS NuGet enables you to integrate iProov's SDK into your .NET iOS projects. 

This NuGet wraps iProov's existing native [iOS](https://github.com/iProov/ios) SDK behind a .NET interface for use from within your .NET app.

For more extensive documentation on iProov's .NET packages, see the public GitHub repository [here](https://github.com/iProov/dotnet-maui).

## Requirements

- .NET 10 (net10.0-ios)
- Xcode 26 or later
- iOS 16 and above. The build does not check this, so an app with a lower minimum iOS version builds without a warning. Set your app's minimum to iOS 16 or later.


## How to use it

1. Using the NuGet Package Manager, add the [iProov.NET.iOS](https://www.nuget.org/packages/iProov.NET.iOS/) package to your project.

2. Add a "Privacy - Camera Usage Description" entry to your Info.plist file with the reason why your app requires camera access (e.g. "To iProov you in order to verify your identity.")

3. Import the package into your project with `using iProov.NET.iOS;`

4. Once you have obtained a token (either via the [.NET API Client](https://github.com/iProov/dotnet-maui/tree/master/APIClient) or other means), you can launch the iProov iOS SDK as follows:

	```csharp
	IProov.LaunchWithStreamingURL(new NSUrl("wss://eu.rp.secure.iproov.me/ws"), token, new IPOptions(), // Substitute streaming URL as appropriate
		connecting: () =>
		{
			// The SDK is connecting to the server. You should provide an indeterminate progress indicator
			// to let the user know that the connection is taking place.
		},
		connected: () =>
		{
			// The SDK has connected, and the iProov user interface will now be displayed. You should hide
			// any progress indication at this point.
		},
		processing: (progress, message) =>
		{
			// The SDK will update your app with the progress of streaming to the server and authenticating
			// the user. This will be called multiple times as the progress updates.
		},
		success: () =>
		{
			// The user was successfully verified/enrolled.
			// You must always independently validate the token server-side (using the /validate API call)
			// before performing any authenticated user actions.
		},
		canceled: (canceler) =>
		{
			// Either the user canceled iProov by pressing the close button or sending the app
			// to the background (canceler == IPCanceler.User), or you canceled it by calling
			// Cancel() on the session (canceler == IPCanceler.Integration).
			// You should use this to determine the next step in your flow.
		},
		failure: (feedbackCodes, localizedDescriptions) =>
		{
			// The user was not successfully verified/enrolled, as their identity could not be verified,
			// or there was another issue with their verification/enrollment. A set of reasons is returned to 
			// understand why the claim failed. Each reason is composed of:
			// - feedbackCode: String representation of the feedback code
			// - localizedDescription: Human readable explanation for the end user to improve their chances to iProov next time

			string codes = string.Join(",", feedbackCodes); // Codes referring to the failure reasons
			string descriptions = string.Join(" ", localizedDescriptions); // Human-readable suggestions about what to do to get to a successful claim
		},
		error: (error) =>
		{
			// The user was not successfully verified/enrolled due to an error (e.g. lost internet connection).
			// You will be provided with an NSError. You can check the error code against the IPErrorCode constants
			// to determine the type of error.
			// It will be called once, or never.
		}
	);
	```
> Note that the launch method requires the URL to be passed as an **NSUrl** 

### Canceling the session

Under normal circumstances, the user will be in control of the completion of the iProov scan, i.e. they will either complete the scan, or use the close button to cancel. In some cases, you (the integrator) may wish to cancel the iProov scan programmatically, for example in response to a timeout or change of conditions in your app.

To cancel the session, you first need to hold a reference to the `IPSession` object (returned from `IProov.LaunchWithStreamingURL`) and you can then call `Cancel()` on it.

```csharp
IPSession session = IProov.LaunchWithStreamingURL(...)

// Example - cancel the session after 10 sec
System.Timers.Timer timer = new System.Timers.Timer(10000);
timer.Elapsed += (sender, e) => 
{
    session.Cancel(); // Will return true if the session was successfully canceled
};
timer.Enabled = true;
```
	
👉 You should now familiarise yourself with the [iProov iOS SDK documentation](https://github.com/iProov/ios) which provides comprehensive details about the available customization options and other important details regarding the iOS SDK usage.