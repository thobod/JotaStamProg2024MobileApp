using Android.App;
using Android.OS;
using Android.Widget;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JotaStamProg2024
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private EditText ipAddressInput;
        private SeekBar frequencySlider, durationSlider, offDurationSlider;
        private EditText connectVolumeInput, cutVolumeInput, cutSuccessVolumeInput, cutFailedVolumeInput, volumeInput;
        private Button updateSoundButton, updateGameStateButton, resetSoundButton;
        private Switch enableSoundSwitch;

        private CancellationTokenSource _cancellationTokenSource;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            // Initialize UI elements
            ipAddressInput = FindViewById<EditText>(Resource.Id.ipAddress);
            frequencySlider = FindViewById<SeekBar>(Resource.Id.frequencySlider);
            durationSlider = FindViewById<SeekBar>(Resource.Id.durationSlider);
            offDurationSlider = FindViewById<SeekBar>(Resource.Id.offDurationSlider);
            connectVolumeInput = FindViewById<EditText>(Resource.Id.connectVolumeInput);
            cutVolumeInput = FindViewById<EditText>(Resource.Id.cutVolumeInput);
            cutSuccessVolumeInput = FindViewById<EditText>(Resource.Id.cutSuccessVolumeInput);
            volumeInput = FindViewById<EditText>(Resource.Id.volumeInput);
            cutFailedVolumeInput = FindViewById<EditText>(Resource.Id.cutFailedVolumeInput);
            updateSoundButton = FindViewById<Button>(Resource.Id.updateSoundButton);
            resetSoundButton = FindViewById<Button>(Resource.Id.resetSoundButton);
            updateGameStateButton = FindViewById<Button>(Resource.Id.updateGameStateButton);
            enableSoundSwitch = FindViewById<Switch>(Resource.Id.enableSoundSwitch);

            // Set default IP address
            ipAddressInput.Text = "http://192.168.1.112";

            // Set event handlers
            updateSoundButton.Click += OnUpdateSoundButtonClick;
            updateGameStateButton.Click += OnUpdateGameStateButtonClick;
            resetSoundButton.Click += OnResetSoundButtonClick;

            // Initialize sliders
            frequencySlider.Max = 4800; // Max value for frequency slider
            durationSlider.Max = 2000;   // Max value for duration slider
            offDurationSlider.Max = 2000; // Max value for off-duration slider

            // Optional: Set initial values
            ResetSound();
        }

        private async void OnUpdateSoundButtonClick(object sender, EventArgs e)
        {
            await SendSoundRequest(); // Call the request method
        }
        private async void OnResetSoundButtonClick(object sender, EventArgs e)
        {
            ResetSound(); // Call the request method
        }


        private async Task SendSoundRequest()
        {
            // Gather values from the UI elements
            int frequency = frequencySlider.Progress + 200; // Frequency from 200 Hz to 5200 Hz
            int duration = durationSlider.Progress; // Duration in ms
            int offDuration = offDurationSlider.Progress; // Off Duration in ms
            bool enable = enableSoundSwitch.Checked; // Get enable status

            // Create the sound data JObject
            var soundData = new JObject();
            soundData.Add("enable", enable);
            soundData.Add("freq", frequency);
            soundData.Add("duration", duration);
            soundData.Add("offDuration", offDuration);

            if (int.TryParse(volumeInput.Text, out int volumeInputValue))
            {
                soundData.Add("volume", volumeInputValue);
            }
            // Check and add volume fields only if they are filled
            if (int.TryParse(connectVolumeInput.Text, out int connectVolumeInputValue))
            {
                soundData.Add("connectVolume", connectVolumeInputValue);
            }

            if (int.TryParse(cutVolumeInput.Text, out int cutVolumeInputValue))
            {
                soundData.Add("cutVolume", cutVolumeInputValue);
            }

            if (int.TryParse(cutSuccessVolumeInput.Text, out int cutSuccessVolumeInputValue))
            {
                soundData.Add("cutSuccessVolume", cutSuccessVolumeInputValue);
            }

            if (int.TryParse(cutFailedVolumeInput.Text, out int cutFailedVolumeInputValue))
            {
                soundData.Add("cutFailedVolume", cutFailedVolumeInputValue);
            }

            string jsonSoundData = soundData.ToString();
            await SendPostRequest($"{ipAddressInput.Text}/sound", jsonSoundData, "Sound Updated!", "Sound Update Failed!");
        }



        private async void OnUpdateGameStateButtonClick(object sender, EventArgs e)
        {
            await SendGameStateRequest(); // Call the request method
        }

        private async Task SendGameStateRequest()
        {
            var gameStateData = new JObject();

            if (int.TryParse(FindViewById<EditText>(Resource.Id.bombCountInput).Text, out int bombCountValue))
            {
                gameStateData.Add("bombCount", bombCountValue);
            }
            if (int.TryParse(FindViewById<EditText>(Resource.Id.minutesRemainingInput).Text, out int minutesRemainingValue))
            {
                gameStateData.Add("minutesRemaining", minutesRemainingValue);
            }

            string jsonGameStateData = JsonConvert.SerializeObject(gameStateData);
            await SendPostRequest($"{ipAddressInput.Text}/gamestate", jsonGameStateData, "Game State Updated!", "Game State Update Failed!");
        }

        private async Task SendPostRequest(string url, string jsonData, string successMessage, string errorMessage)
        {
            using (var client = new HttpClient())
            {
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    Toast.MakeText(this, successMessage, ToastLength.Short).Show();
                }
                else
                {
                    Toast.MakeText(this, errorMessage, ToastLength.Short).Show();
                }
            }
        }

        private void ResetSound()
        {
            frequencySlider.Progress = 1000;
            durationSlider.Progress = 300;
            offDurationSlider.Progress = 700;
            volumeInput.Text = "1";
            connectVolumeInput.Text = "1";
            cutVolumeInput.Text = "15";
            cutSuccessVolumeInput.Text = "1";
            cutFailedVolumeInput.Text = "100";
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
