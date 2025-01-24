# iProov.NET.iOS NuGet

## Introduction

The iProov.NET.iOS NuGet enables you to integrate iProov's SDK into your .NET iOS projects. 

This NuGet wraps iProov's existing native [iOS](https://github.com/iProov/ios) SDK behind a .NET interface for use from within your .NET app.

For a more extensive documentation on iProov's .NET packages check the public GitHub repository [here](https://github.com/iProov/dotnet-maui).

## Requirements

- NET 8 (net8-ios)
- iOS 13 and above


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
		success: (result) =>
		{
			// The user was successfully verified/enrolled and the token has been validated.
			// You can access the following properties:
			var token = result.Token; // The token passed back will be the same as the one passed in to the original call
			var frame = result.Frame; // An optional image containing a single frame of the user, if enabled for your service provider
		},
		canceled: (canceler) =>
		{
			// Either the user canceled iProov by pressing the Close button at the top left or sending
			// the app to the background. (canceler == USER)
			// Or the app canceled using Session.cancel() (canceler == INTEGRATION).
			// You should use this to determine the next step in your flow.
		},
		failure: (result) =>
		{
			// The user was not successfully verified/enrolled, as their identity could not be verified,
			// or there was another issue with their verification/enrollment. A reason (as a string)
			// is provided as to why the claim failed, along with a feedback code from the back-end.
			var reason = result.Reason
			var description = result.LocalizedDescription;
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
> Note that the launch method requires the url to be passed as a **NSURL** 

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