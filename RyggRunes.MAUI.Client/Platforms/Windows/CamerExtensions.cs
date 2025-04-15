using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.System;
using WinRT.Interop;

namespace RyggRunes.MAUI.Client.WinUI {
    public class CustomCameraCaptureUI
    {
        private LauncherOptions _launcherOptions;

        public CustomCameraCaptureUI()
        {
            var hndl = ((MauiWinUIWindow)App.Current.Application.Windows[0].Handler.PlatformView).WindowHandle;  //Helper which calls WinRT.Interop.WindowNative.GetWindowHandle on the main window

            _launcherOptions = new LauncherOptions();
            InitializeWithWindow.Initialize(_launcherOptions, hndl);

            _launcherOptions.TreatAsUntrusted = false;
            _launcherOptions.DisplayApplicationPicker  = false;
            _launcherOptions.TargetApplicationPackageFamilyName = "Microsoft.WindowsCamera_8wekyb3d8bbwe";
        }

        public async Task<StorageFile> CaptureFileAsync(CameraCaptureUIMode mode)
        {
            var extension = mode == CameraCaptureUIMode.Photo ? ".jpg" : ".mp4";

            var tempFolder = await StorageFolder.GetFolderFromPathAsync(System.IO.Path.GetTempPath());
            var tempFileName = $"CCapture{extension}";
            var tempFile = await tempFolder.CreateFileAsync(tempFileName, CreationCollisionOption.GenerateUniqueName);
            var token = Windows.ApplicationModel.DataTransfer.SharedStorageAccessManager.AddFile(tempFile);

            var set = new ValueSet();
            if (mode == CameraCaptureUIMode.Photo)
            {
                set.Add("MediaType", "photo");
                set.Add("PhotoFileToken", token);
                set.Add("MaxResolution", (int)MaxResolution);
                set.Add("Format", 0);

            }
            else
            {
                set.Add("MediaType", "video");
                set.Add("VideoFileToken", token);
            }

            var uri = new Uri("microsoft.windows.camera.picker:");
            var result = await Windows.System.Launcher.LaunchUriForResultsAsync(uri, _launcherOptions, set);
            if (result.Status == LaunchUriStatus.Success)
            {
                return tempFile;
            }

            return null;
        }

        public CameraCaptureUIMaxPhotoResolution MaxResolution { get; set; } = CameraCaptureUIMaxPhotoResolution.HighestAvailable;
    }
}