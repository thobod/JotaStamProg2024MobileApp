using Android.App;
using Android.Content;
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

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            try
            {
                InitializeUI(); // Extract UI initialization to a method with a try-catch
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void InitializeUI()
        {
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
            ipAddressInput.Text = "http://192.168.127.137";

            // Set event handlers
            updateSoundButton.Click += OnUpdateSoundButtonClick;
            updateGameStateButton.Click += OnUpdateGameStateButtonClick;
            resetSoundButton.Click += OnResetSoundButtonClick;

            // Initialize sliders
            frequencySlider.Max = 4800;
            durationSlider.Max = 2000;
            offDurationSlider.Max = 2000;

            ResetSound();
        }

        private async void OnUpdateSoundButtonClick(object sender, EventArgs e)
        {
            try
            {
                await SendSoundRequest();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private async void OnResetSoundButtonClick(object sender, EventArgs e)
        {
            try
            {
                ResetSound();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private async Task SendSoundRequest()
        {
            try
            {
                int frequency = frequencySlider.Progress + 200;
                int duration = durationSlider.Progress;
                int offDuration = offDurationSlider.Progress;
                bool enable = enableSoundSwitch.Checked;

                var soundData = new JObject();
                soundData.Add("enable", enable);
                soundData.Add("freq", frequency);
                soundData.Add("duration", duration);
                soundData.Add("offDuration", offDuration);

                if (int.TryParse(volumeInput.Text, out int volumeInputValue))
                {
                    soundData.Add("volume", volumeInputValue);
                }
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
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private async void OnUpdateGameStateButtonClick(object sender, EventArgs e)
        {
            try
            {
                await SendGameStateRequest();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private async Task SendGameStateRequest()
        {
            try
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
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private async Task SendPostRequest(string url, string jsonData, string successMessage, string errorMessage)
        {
            try
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
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void ResetSound()
        {
            try
            {
                frequencySlider.Progress = 800;
                durationSlider.Progress = 300;
                offDurationSlider.Progress = 700;
                volumeInput.Text = "1";
                connectVolumeInput.Text = "1";
                cutVolumeInput.Text = "15";
                cutSuccessVolumeInput.Text = "1";
                cutFailedVolumeInput.Text = "100";
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void HandleException(Exception ex)
        {
            // Log the exception (optional)
            // Restart the activity
            Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
            Intent intent = Intent;
            Finish();
            StartActivity(intent);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
