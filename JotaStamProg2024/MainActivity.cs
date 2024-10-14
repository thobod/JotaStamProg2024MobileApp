using Android.App;
using Android.OS;
using Android.Widget;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JotaStamProg2024
{
    [Activity(Label = "MainActivity", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private SeekBar frequencySlider, durationSlider, offDurationSlider;
        private EditText connectVolumeInput, cutVolumeInput, cutSuccessVolumeInput, cutFailedVolumeInput;
        private EditText bombCountInput, minutesRemainingInput, ipAddressInput;
        private TextView frequencyLabel, durationLabel, offDurationLabel;
        private Button resetButton, updateSoundButton;

        // Default IP Address
        private string defaultIpAddress = "http://192.168.1.112";
        private CancellationTokenSource _cancellationTokenSource;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            InitializeViews();
            SetDefaultValues();
        }

        // Initialize views and set up event listeners
        private void InitializeViews()
        {
            ipAddressInput = FindViewById<EditText>(Resource.Id.ipAddress);
            frequencySlider = FindViewById<SeekBar>(Resource.Id.frequencySlider);
            durationSlider = FindViewById<SeekBar>(Resource.Id.durationSlider);
            offDurationSlider = FindViewById<SeekBar>(Resource.Id.offDurationSlider);
            connectVolumeInput = FindViewById<EditText>(Resource.Id.connectVolumeInput);
            cutVolumeInput = FindViewById<EditText>(Resource.Id.cutVolumeInput);
            cutSuccessVolumeInput = FindViewById<EditText>(Resource.Id.cutSuccessVolumeInput);
            cutFailedVolumeInput = FindViewById<EditText>(Resource.Id.cutFailedVolumeInput);
            bombCountInput = FindViewById<EditText>(Resource.Id.bombCountInput);
            minutesRemainingInput = FindViewById<EditText>(Resource.Id.minutesRemainingInput);
            resetButton = FindViewById<Button>(Resource.Id.resetButton);
            updateSoundButton = FindViewById<Button>(Resource.Id.updateSoundButton);

            frequencyLabel = FindViewById<TextView>(Resource.Id.frequencyLabel);
            durationLabel = FindViewById<TextView>(Resource.Id.durationLabel);
            offDurationLabel = FindViewById<TextView>(Resource.Id.offDurationLabel);

            // Set up slider and button event handlers
            frequencySlider.ProgressChanged += (s, e) =>
            {
                frequencyLabel.Text = $"Frequency: {e.Progress + 200} Hz";
                StartContinuousUpdates();
            };

            durationSlider.ProgressChanged += (s, e) =>
            {
                durationLabel.Text = $"Duration: {e.Progress} ms";
                StartContinuousUpdates();
            };

            offDurationSlider.ProgressChanged += (s, e) =>
            {
                offDurationLabel.Text = $"Off Duration: {e.Progress} ms";
                StartContinuousUpdates();
            };

            resetButton.Click += async (s, e) => await SendResetRequest();
            updateSoundButton.Click += async (s, e) => await SendSoundRequest();
        }

        // Set default values for initial labels and inputs
        private void SetDefaultValues()
        {
            ipAddressInput.Text = defaultIpAddress;
            frequencyLabel.Text = "Frequency: 200 Hz";
            durationLabel.Text = "Duration: 0 ms";
            offDurationLabel.Text = "Off Duration: 0 ms";
        }

        // Send reset game request
        private async Task SendResetRequest()
        {
            // Ensure bomb count and minutes remaining are valid integers
            if (!int.TryParse(bombCountInput.Text, out int bombCount) || !int.TryParse(minutesRemainingInput.Text, out int minutesRemaining))
            {
                Toast.MakeText(this, "Please enter valid bomb count and minutes remaining.", ToastLength.Short).Show();
                return; // Exit the method early if invalid input
            }

            var resetData = new
            {
                bombCount,
                minutesRemaining
            };

            string jsonResetData = JsonConvert.SerializeObject(resetData);
            await SendPostRequest($"{ipAddressInput.Text}/gamestate", jsonResetData, "Gamestate updated!", "Gamestate update Failed!");
        }

        // Start continuous updates for sound parameters
        private void StartContinuousUpdates()
        {
            // Cancel any ongoing updates
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            // Start the update task
            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await SendSoundRequest();
                    await Task.Delay(1000); // Delay for 1 second between requests
                }
            });
        }

        // Send sound update request
        private async Task SendSoundRequest()
        {
            // Ensure all volume fields are valid integers
            if (!int.TryParse(connectVolumeInput.Text, out int connectVolume) ||
                !int.TryParse(cutVolumeInput.Text, out int cutVolume) ||
                !int.TryParse(cutSuccessVolumeInput.Text, out int cutSuccessVolume) ||
                !int.TryParse(cutFailedVolumeInput.Text, out int cutFailedVolume))
            {
                Toast.MakeText(this, "Please fill in all volume fields with valid values.", ToastLength.Short).Show();
                return; // Exit the method early if any volume input is invalid
            }

            int frequency = frequencySlider.Progress + 200; // Frequency from 200 Hz to 5200 Hz
            int duration = durationSlider.Progress; // Duration in ms
            int offDuration = offDurationSlider.Progress; // Off Duration in ms

            // Create the sound data object with the expected keys
            var soundData = new
            {
                connectVolume,
                cutVolume,
                cutSuccessVolume,
                cutFailedVolume,
                enable = true,         // Assuming we want to enable sound when sending
                freq = frequency,      // Frequency
                duration = duration,   // Duration
                offDuration = offDuration // Off Duration
            };

            string jsonSoundData = JsonConvert.SerializeObject(soundData);
            await SendPostRequest($"{ipAddressInput.Text}/sound", jsonSoundData, "Sound Updated!", "Sound Update Failed!");
        }

        // Generalized POST request handler
        private async Task SendPostRequest(string url, string jsonData, string successMessage, string failureMessage)
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(url, content);

                string toastMessage = response.IsSuccessStatusCode ? successMessage : failureMessage;
                Toast.MakeText(this, toastMessage, ToastLength.Short).Show();
            }
        }

        // Cleanup when activity is destroyed
        protected override void OnDestroy()
        {
            base.OnDestroy();
            _cancellationTokenSource?.Cancel(); // Cancel ongoing updates
        }
    }
}
