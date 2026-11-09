# iProov.NET.Android NuGet

## Introduction

The iProov.NET.Android NuGet enables you to integrate iProov's SDK into your .NET Android projects. 

This NuGet wraps iProov's existing native [Android](https://github.com/iProov/android) SDK behind a .NET interface for use from within your .NET app.

For more extensive documentation on iProov's .NET packages, see the public GitHub repository [here](https://github.com/iProov/dotnet-maui).

## Use Flows and Coroutines

Since version `10.0.0` of the [Android](https://github.com/iProov/android) SDK the use of Flows is necessary to collect information from a claim. 

When using iProov.NET.Android integrators need to use the .NET libraries for Flows and Coroutines and create the necessary classes to handle the values returned by the SDK. This is all explained in the [How to use it](#how-to-use-it) section and the code for the necessary classes can be found in [Additional code](#additional-code).

As an alternative solution, integrators can use the iProov.NET.MAUI package instead ([here](https://github.com/iProov/dotnet-maui)), which already wraps the `iProov.NET.Android` package into a callback based API.

## Requirements

- .NET 10 (net10.0-android)
- Microsoft.Android.Sdk 36.1.53 or later
- Android API Level 29 (Android 10) and above

## Upgrading from 11.x

This version uses the iProov Android SDK 12.0.0. For the SDK's own changes, see the native [Android upgrade guide](https://github.com/iProov/android/wiki/Upgrade-Guide). To upgrade an app from `iProov.NET.Android` 11.x:

- Your app must target .NET 10 (net10.0-android) and build with Microsoft.Android.Sdk 36.1.53 or later.
- This package no longer exposes parts of the Android SDK that are not in its documented API. Remove any use of:
  - the generated protobuf classes, such as `EnvelopeOuterClass`
  - the obfuscated fields on `IProov.Options.Icon`
  - `IProovKeep` and `IProovKeepAttribute`
  - `IProovKt`, including `IProovKt.ToLoggable()`

## How to use it

> The approach presented next assumes the following classes are used:
> * [IStateListener](#istatelistener) interface to be implemented to receive `iProov.State` updates.
> * [FlowCollector](#flowcollector) to collect the states from a `Session` and notify those changes to the `IStateListener`
> * [FlowContinuation](#flowcontinuation) necessary to collect values from a flow.

1. Using the NuGet Package Manager, add the [iProov.NET.Android](https://www.nuget.org/packages/iProov.NET.Android/) package to your project.

2. Import the package into your project with `using iProov.NET.Android;`

3. Create a class which implements `IStateListener` to handle the callbacks from the Android SDK:

	```csharp	
	public class IProovListener : IStateListener
	{
	
		public void OnConnecting()
		{
	   		// Called when the SDK is connecting to the server. You should provide an indeterminate
	   		// progress indication to let the user know that the connection is being established.
		}

		public void OnConnected()
		{
	   		// The SDK has connected, and the iProov user interface will now be displayed. You
	   		// should hide any progress indication at this point.
		}
            
		public void OnCanceled(IProov.Canceler canceler)
		{
	   		// Either the user canceled iProov by pressing the Close button at the top right or
			// the Home button (canceler == USER)
			// The session was canceled by the integrator using Session.cancel() (canceler == INTEGRATION).
			// You should use this to determine the next step in your flow.
		}
		
		public void OnError(IProovException error)
		{
			// The user was not successfully verified/enrolled due to an error (e.g. lost internet connection)
			// along with an IProovException.
			// It will be called once, or never.
		}
		
		public void OnFailure(IList<IProov.FailureReason> reasons)
		{
			// The user was not successfully verified/enrolled, as their identity could not be verified,
			// or there was another issue with their verification/enrollment.
			// An IProov.FailureReason is an Enum type that has two properties:
			// - FeedbackCode: (string) Represents the feedback code associated with a FailureReason.
			// - Description: (int) String resource ID that contains the human readable text as to why the claim failed.
			
			foreach (IProov.FailureReason reason in reasons)
			{
				var feedbackCode = reason.FeedbackCode;
				var description = reason.Description;
			}
		}
		
		public void OnProcessing(double progress, string message)
		{
			// The SDK will update your app with the progress of streaming to the server and authenticating
			// the user. This will be called multiple times as the progress updates.
		}
		
		public void OnSuccess()
		{
			// The user was successfully verified/enrolled.
			// You must always independently validate the token server-side (using the /validate API call) 
			// before performing any authenticated user actions.
		}
	
	}
	
	```
	
	> Alternatively you could just implement `IStateListener` on your `Activity` class.
	
4. Create an instance of `FlowCollector` passing your `IStateListener` in the constructor.

	```csharp
	private void launchIProov() {
		...
		IProovListener listener = new IProovListener();
		FlowCollector flowCollector = new FlowCollector(listener);
		...
	}
	```
	
5. Once you have obtained a token (either via the [.NET API Client](https://github.com/iProov/dotnet-maui/tree/master/APIClient) or other means), you can now launch iProov. To do so, first you need to create a `Session` and set up the collection of `State` values for it (use `FlowContinuation` here). After that, you can `start` the session:

	```csharp
	private void launchIProov() {
		...
		IProovListener listener = new IProovListener();
		FlowCollector flowCollector = new FlowCollector(listener);
		FlowContinuation continuation = new FlowContinuation();

		IProov.Options options = new IProov.Options();

        IProov.ISession session = IProov.Instance.CreateSession(this, "wss://eu.rp.secure.iproov.me/ws", token, options);
        session.State.Collect(flowCollector, continuation);
        session.Start();
	}
	```
	
👉 You should now familiarise yourself with the [iProov Android SDK documentation](https://github.com/iProov/android) which provides comprehensive details about the available customization options and other important details regarding the Android SDK usage.

### Canceling the session

Under normal circumstances, a session will be canceled by the user (Canceler.USER) by pressing the back button or the close button. However, there may be situations where an integrator may need to cancel an ongoing session. For those situations, integrators can invoke the `Cancel()` method from the `ISession` created to perform a scan.

```csharp
IProov.ISession session = IProov.Instance.CreateSession(context, wss_url, token, options);

session.Cancel();
```

## Additional code

### IStateListener
```csharp
namespace <YOUR_NAMESPACE>;

using iProov.NET.Android;

public interface IStateListener
    {
        void OnCanceled(IProov.Canceler canceler);
        void OnConnected();
        void OnConnecting();
        void OnError(IProovException exception);
        void OnFailure(IList<IProov.FailureReason> reasons);
        void OnProcessing(double progress, string? message);
        void OnSuccess();
    }
```

### FlowCollector
```csharp
namespace <YOUR_NAMESPACE>;

using iProov.NET.Android;
using Xamarin.KotlinX.Coroutines.Flow;
using Kotlin.Coroutines;

public class FlowCollector : Java.Lang.Object, IFlowCollector
    {
        private IStateListener listener;
        public FlowCollector(IStateListener callbackListener) {
            listener = callbackListener;
        }
        
        public Java.Lang.Object? Emit(Java.Lang.Object? value, IContinuation p1)
        {
            if (value != null && value is IProov.State state) {
                notifyListener(state);
                p1.ResumeWith(value);
            }

            return value;
        }

        private void notifyListener(IProov.State state) {
            switch (state)
            {
                case IProov.State.Connected:
                    listener.OnConnected();
                    break;
                case IProov.State.Connecting:
                    listener.OnConnecting();
                    break;
                case IProov.State.Processing:
                    if (state is IProov.State.Processing processing)
                        listener.OnProcessing(processing.Progress, processing.Message);
                    break;
                case IProov.State.Canceled:
                    if (state is IProov.State.Canceled canceled)
                        listener.OnCanceled(canceled.Canceler);
                    break;
                case IProov.State.Error:
                    if (state is IProov.State.Error error)
                        listener.OnError(error.Exception);
                    break;
                case IProov.State.Failure:
                    if (state is IProov.State.Failure failure)
                        listener.OnFailure(failure.Reasons);
                    break;
                case IProov.State.Success:
                    listener.OnSuccess();
                    break;
            }
        }
    }
```

### FlowContinuation
```csharp
namespace <YOUR_NAMESPACE>;

using Kotlin.Coroutines;
public class FlowContinuation : Java.Lang.Object, IContinuation
    {
        public ICoroutineContext Context => EmptyCoroutineContext.Instance;
        public void ResumeWith(Java.Lang.Object result){}
    }
```