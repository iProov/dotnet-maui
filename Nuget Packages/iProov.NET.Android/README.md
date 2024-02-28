# iProov.NET.Android NuGet

## Introduction

The iProov.NET.Android NuGet enables you to integrate iProov's SDK into your .NET Android projects. 

This NuGet wraps iProov's existing native [Android](https://github.com/iProov/android) SDK behind a .NET interface for use from within your .NET app.

### Requirements

- NET 8 (net8-android)
- Android API Level 21 (Android 5 Lollipop) and above

## How to use it

1. Using the NuGet Package Manager, add the [iProov.NET.Android](https://www.nuget.org/packages/iProov.NET.Android/) package to your project. For further instructions on how to install and manage packages, [see here](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio).

2. Import the package into your project with `using iProov.NET.Android;`

3. Create an instance of IProovCallbackLauncher

	```csharp
	IProovCallbackLauncher iProovLauncher = new IProovCallbackLauncher();
	```

4. Create a private class which implements `IProovCallbackLauncher.IListener` to handle the callbacks from the Android SDK:

	```csharp
	private IProovListener listener = new IProovListener();
	
	private class IProovListener : Java.Lang.Object, IProov.IListener
	{
	
		public void OnConnected()
		{
	   		// Called when the SDK is connecting to the server. You should provide an indeterminate
	   		// progress indication to let the user know that the connection is being established.
		}

		public void OnConnecting()
		{
	   		// The SDK has connected, and the iProov user interface will now be displayed. You
	   		// should hide any progress indication at this point.
		}
            
		public void OnCanceled(IProov.Canceler canceler)
		{
	   		// Either the user canceled iProov by pressing the Close button at the top right or
			// the Home button (canceler == USER)
			// Or the app canceled using Session.cancel() (canceler = APP).
			// You should use this to determine the next step in your flow.
		}
		
		public void OnError(IProovException error)
		{
			// The user was not successfully verified/enrolled due to an error (e.g. lost internet connection)
			// along with an IProovException.
			// It will be called once, or never.
		}
		
		public void OnFailure(IProov.FailureResult result)
		{
			// The user was not successfully verified/enrolled, as their identity could not be verified,
			// or there was another issue with their verification/enrollment. A reason (as a string resource id)
			// is provided as to why the claim failed, along with a feedback code from the back-end.
			
			var feedbackCode = result.FeedbackCode;
			var reason = result.Reason.Description;
		}
		
		public void OnProcessing(double progress, string message)
		{
			// The SDK will update your app with the progress of streaming to the server and authenticating
			// the user. This will be called multiple time as the progress updates.
		}
		
		public void OnSuccess(IProov.SuccessResult result)
		{
			// The user was successfully verified/enrolled and the token has been validated.
			// You must always independently validate the token server-side (using the /validate API call) 
			// before performing any authenticated user actions.
		}
	
	}
	
	```
	
	> Alternatively you could just implement `IProov.IListener` on your `Activity` class.
	
5. You must register the iProov listener when your Activity is created:

	```csharp

	IProovCallbackLauncher iProovLauncher = new IProovCallbackLauncher();
	IProovListener listener = new IProovListener();

	protected override void OnCreate(Bundle savedInstanceState)
	{
		base.OnCreate(savedInstanceState);
		iProovLauncher.Listener = listener;
		
		// ...continue your activity setup ...
	}
	```
	
	...and unregister it when destroyed:
	
	```csharp
	protected override void OnDestroy()
	{
		iProovLauncher.Listener = null;
		base.OnDestroy();
	}
	```

5. You can now launch iProov by calling:

	```csharp
	iProovLauncher.Launch(this, "wss://eu.rp.secure.iproov.me/ws", token, new IProov.Options()); // Substitute the streaming URL as appropriate
	```
	
👉 You should now familiarise yourself with the [iProov Android SDK documentation](https://github.com/iProov/android) which provides comprehensive details about the available customization options and other important details regarding the Android SDK usage.
