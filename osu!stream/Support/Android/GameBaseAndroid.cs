using Android.App;
using IntelliJ.Lang.Annotations;
using osum.AssetManager;
using osum.Audio;
using osum.GameModes;
using osum.GameModes.SongSelect;
using osum.Helpers;
using osum.Input;
using osum.Input.Sources;
using System;
using System.IO;
using Xamarin.Essentials;

namespace osum.Support.Android
{
    internal class GameBaseAndroid : GameBase
    {
        private readonly Activity activity;

        public Activity Activity => activity;

        public static bool IsInitialized;
        public GameWindowAndroid Window;

        public GameBaseAndroid(Activity activity, OsuMode mode = OsuMode.Unknown) : base(mode)
        {
            this.activity = activity;

            NativeAssetManagerAndroid.Manager = activity.Assets;
        }

        public override void Run()
        {
            // Before we run anything, let's check if this is the first run...
            // If this is the first run, let's import the packaged beatmaps from our assets!
            // This is unfortunately required because Android sucks, thanks Google.
            if (Config.GetValue("firstrun", true))
            {
                string[] beatmapPaths = activity.Assets?.List("Beatmaps/");

                Directory.CreateDirectory(SongSelectMode.BeatmapPath); // Create BeatmapPath, if it doesn't exist.

                if (beatmapPaths != null)
                {
                    foreach (string beatmapPath in beatmapPaths)
                    {
                        using (var fs = File.Create(SongSelectMode.BeatmapPath + "/" + beatmapPath))
                        {
                            activity.Assets?.Open("Beatmaps/" + beatmapPath)?.CopyTo(fs);
                        }
                    }
                }
            }

            if (this.Window == null)
                Window = new GameWindowAndroid(activity);
            Window.Run();

            activity.SetContentView(Window);
        }

        public override void Initialize()
        {
            IsInitialized = true;

            base.Initialize();
        }

        protected override BackgroundAudioPlayer InitializeBackgroundAudio()
        {
            return new BackgroundAudioPlayerAndroid();
        }

        protected override SoundEffectPlayer InitializeSoundEffects()
        {
            return new SoundEffectPlayerBass();
        }

        protected override NativeAssetManager InitializeAssetManager()
        {
            return new NativeAssetManagerAndroid();
        }

        protected override void InitializeInput()
        {
            InputSource source = new InputSourceAndroid();
            InputManager.AddSource(source);
        }

        public override void SetupScreen()
        {
            // Because this is always landscape, we'll use the safe width and real height of the display.
            NativeSize = new System.Drawing.Size((int)DeviceDisplay.MainDisplayInfo.Width, (int)DeviceDisplay.MainDisplayInfo.Height);

            this.DisableDimming = true;

            base.SetupScreen();
        }

        public override string PathConfig => $"{Environment.GetFolderPath(Environment.SpecialFolder.Personal)}/";

        string identifier;
        public override string DeviceIdentifier
        {
            get
            {
                if (identifier == null)
                {
#if SIMULATOR
            identifier = base.DeviceIdentifier;
#else
                    try
                    {
                        const string prefsName = "osustream_prefs";
                        const string keyName = "device_identifier";

                        var context = global::Android.App.Application.Context;
                        var prefs = context.GetSharedPreferences(
                            prefsName,
                            global::Android.Content.FileCreationMode.Private
                        );

                        identifier = prefs.GetString(keyName, null);

                        if (string.IsNullOrEmpty(identifier))
                        {
                            // Generate once
                            identifier = Guid.NewGuid().ToString();

                            var editor = prefs.Edit();
                            editor.PutString(keyName, identifier);
                            editor.Commit();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        identifier = Guid.NewGuid().ToString();
                    }
#endif
                }

                return CryptoHelper.GetMd5String(identifier);
            }
        }
    }
}
