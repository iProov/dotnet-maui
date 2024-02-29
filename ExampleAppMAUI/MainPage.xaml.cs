using System.Reflection;
using iProov.APIClient;
using iProov.NET.MAUI;

namespace ExampleAppMAUI;

public partial class MainPage : ContentPage, IProovWrapper.IStateListener
{
    IProovWrapper wrapper = new IProovWrapper();
    AssuranceType assuranceType = AssuranceType.GenuinePresence;
    ClaimType claimType = ClaimType.Enrol;

    public MainPage()
	{
		InitializeComponent();
	}

    private bool AreCredentialsSet()
    {
        return Credentials.API_KEY.Length > 0 && Credentials.SECRET.Length > 0;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SDKVersionLabel.Text = "SDK Version: " + wrapper.GetSdkVersion();
    }

    private async void OnLaunchIProov(object sender, EventArgs e)
	{
        if(!AreCredentialsSet())
        {
            await DisplayAlert("Error", "Set your API_KEY and SECRET in the Credentials file", "Understood");
            return;
        }

		string userId = UserIdEntry.Text;

		if (userId != null && userId.Length > 0)
		{
            ApiClient apiClient = new ApiClient(
            Credentials.API_CLIENT_URL,
            Credentials.API_KEY,
            Credentials.SECRET,
            "com.iproov.MAUIExample");

			try
			{
                var token = await apiClient.GetToken(assuranceType, claimType, userId);
                launchIProov(token, userId);
            }
            catch (Exception exception)
			{
                await DisplayAlert("Failed to get token", $"{exception.Message}", "OK");
            }

        } else
		{
            await DisplayAlert("Error", "You need to provide a user id", "OK");
		}
    }

	private void launchIProov(string token, string userId)
	{
        ClaimFrame.Source = null;

        var options = new IProovOptions();

        // Options can be customised as shown in the CustomizeOptions method
        // CustomizeOptions(options)

        Console.WriteLine("Launching iPROOV --- ");
        wrapper.LaunchIProov(token, userId, Credentials.BASE_URL, this, options);
    }

    private void CustomizeOptions(IProovOptions options)
    {
        // Set your desired options here

        options.enableScreenShots = false;
        options.closeButtonTintColor = Colors.Red;
        options.closeButtonImage = ImageToByteArrayAsync("ExampleAppMAUI.Resources.Images.custom_back.png");
        options.disableExteriorEffects = false;
        options.filter = new IProovOptions.LineDrawingFilter(LineDrawingFilterStyle.Vibrant);
        options.headerBackgroundColor = Colors.Yellow;
        options.livenessAssurance = new IProovOptions.LivenessAssurance(ovalStrokeColor: Colors.Beige, completedOvalStrokeColor: Colors.DarkGreen);
        options.promptBackgroundColor = Colors.White;
        options.promptRoundedCorners = true;
        options.promptTextColor = Colors.DarkSlateGray;
        options.title = "Example MAUI";
        options.titleTextColor = Colors.DarkViolet;
        options.surroundColor = Colors.SteelBlue;
    }

	void OnGenerateUUID(object sender, EventArgs e)
	{
		UserIdEntry.Text = Guid.NewGuid().ToString();
    }

    void OnGPAButtonClicked(object sender, EventArgs e)
    {
        assuranceType = AssuranceType.GenuinePresence;
        UpdateUI();
    }

    void OnLAButtonClicked(object sender, EventArgs e)
    {
        assuranceType = AssuranceType.Liveness;
        UpdateUI();
    }

    void OnEnrolButtonClicked(object sender, EventArgs e)
    {
        claimType = ClaimType.Enrol;
        UpdateUI();
    }

    void OnVerifyButtonClicked(object sender, EventArgs e)
    {
        claimType = ClaimType.Verify;
        UpdateUI();
    }

    public void OnConnected()
    {
        Console.WriteLine("iPROOV --- Connected");
    }

    public void OnConnecting()
    {
        Console.WriteLine("iPROOV --- OnConnecting");
    }

    public void OnCanceled(Canceler canceler)
    {
        DisplayAlert("OnCanceled", $"Canceled by {canceler}", "OK");
        ClaimEnded();
    }

    public void OnError(IProovException exception)
    {
        DisplayAlert("OnError", $"Exception: {exception.GetType}\nTitle: {exception.title} // Message: {exception.message}", "OK");
        ClaimEnded();
    }

    public void OnFailure(IProovFailureResult failure)
    {
        DisplayAlert("OnFailure", $"Reason: {failure.reason}\nDescription: {failure.description}", "OK");
        if (failure.frame != null)
            LoadFrameResult(failure.frame);

        ClaimEnded();
    }

    public void OnProcessing(double progress, string? message)
    {
        Console.WriteLine($"iPROOV --- OnProcessing(progres: {progress}, message: {message})");
        UpdateProgress(progress);
    }

    public void OnSuccess(byte[]? frame)
    {
        var frameMssg = frame == null ? "No" : "A";
        DisplayAlert("OnSuccess", $"{frameMssg} frame was returned", "OK");

        if (frame != null)
            LoadFrameResult(frame);

        ClaimEnded();
    }

    // Utils

    private void UpdateUI()
    {
        bool isGPA = assuranceType == AssuranceType.GenuinePresence;
        bool isEnrol = claimType == ClaimType.Enrol;
        GPAButton.BackgroundColor = isGPA ? Colors.RoyalBlue : Colors.DarkGray;
        LAButton.BackgroundColor = !isGPA ? Colors.RoyalBlue : Colors.DarkGray;
        EnrolButton.BackgroundColor = isEnrol ? Colors.RoyalBlue : Colors.DarkGray;
        VerifyButton.BackgroundColor = !isEnrol ? Colors.RoyalBlue : Colors.DarkGray;
        LaunchButton.Text = (isEnrol ? "Enrol" : "Verify") + " with " + (isGPA ? "GPA" : "LA");
    }

    private void UpdateProgress(double progress)
    {
        if (progress == 1 || progress == 0)
        {
            ClaimProgress.IsVisible = false;
            ClaimProgress.Progress = 0;
            return;
        }

        if (!ClaimProgress.IsVisible)
            ClaimProgress.IsVisible = true;

        ClaimProgress.Progress = progress;
    }

    private void ClaimEnded()
    {
        UpdateProgress(1);
    }

    private void LoadFrameResult(byte[] frame)
    {
        MemoryStream stream = new MemoryStream(frame);
        ClaimFrame.Source = ImageSource.FromStream(() => stream);
    }

    private static byte[] ImageToByteArrayAsync(string path)
    {
        var assem = Assembly.GetExecutingAssembly();
        using var stream = assem.GetManifestResourceStream(path);
        byte[] bytesAvailable = new byte[stream.Length];
        stream.Read(bytesAvailable, 0, bytesAvailable.Length);
        return bytesAvailable;
    }


}


