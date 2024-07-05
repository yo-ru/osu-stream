using System;
#if !iOS
using System.Windows.Forms;
#endif
using OpenTK;
using OpenTK.Graphics;
using osum.Audio;
using osum.GameModes.SongSelect;
using osum.Graphics;
using osum.Graphics.Renderers;
using osum.Graphics.Sprites;
using osum.Helpers;
using osum.Libraries.NetLib;
using osum.Localisation;
using osum.UI;

#if iOS
using Accounts;
using Foundation;
using osum.Support.iPhone;
using UIKit;
#endif

namespace osum.GameModes.Options
{
    public class Options : GameMode
    {
        private BackButton s_ButtonBack;

        private readonly SpriteManagerDraggable smd = new SpriteManagerDraggable
        {
            Scrollbar = true
        };

        private SliderControl soundEffectSlider;
        private SliderControl universalOffsetSlider;

        private readonly SpriteManager topMostSpriteManager = new SpriteManager();

        internal static float ScrollPosition;

        public override void Initialize()
        {
            s_Header = new pSprite(TextureManager.Load(OsuTexture.options_header), new Vector2(0, 0));
            s_Header.OnClick += delegate { };
            topMostSpriteManager.Add(s_Header);

            pDrawable background =
                new pSprite(TextureManager.Load(OsuTexture.songselect_background), FieldTypes.StandardSnapCentre, OriginTypes.Centre,
                    ClockTypes.Mode, Vector2.Zero, 0, true, new Color4(56, 56, 56, 255));
            background.AlphaBlend = false;
            spriteManager.Add(background);

            s_ButtonBack = new BackButton(delegate { Director.ChangeMode(OsuMode.MainMenu); }, Director.LastOsuMode == OsuMode.MainMenu);
            smd.AddNonDraggable(s_ButtonBack);

            if (MainMenu.MainMenu.InitializeBgm())
                AudioEngine.Music.Play();

            const int header_x_offset = 60;

            float button_x_offset = GameBase.BaseSize.X / 2;

            int vPos = 70;

            pText text = new pText(LocalisationManager.GetString(OsuString.About), 36, new Vector2(header_x_offset, vPos), 1, true, Color4.White) { Bold = true, TextShadow = true };
            smd.Add(text);

            vPos += 90;

            pButton button = new pButton(LocalisationManager.GetString(OsuString.Credits), new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate { Director.ChangeMode(OsuMode.Credits); });
            smd.Add(button);

            vPos += 70;

            button = new pButton(LocalisationManager.GetString(OsuString.OnlineHelp), new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate { GameBase.Instance.ShowWebView("https://www.osustream.com/help/", "Online Help"); });

            smd.Add(button);

            vPos += 60;

            text = new pText(LocalisationManager.GetString(OsuString.DifficultySettings), 36, new Vector2(header_x_offset, vPos), 1, true, Color4.White) { Bold = true, TextShadow = true };
            smd.Add(text);

            vPos += 90;

            buttonFingerGuides = new pButton(LocalisationManager.GetString(OsuString.UseFingerGuides), new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate { DisplayFingerGuideDialog(); });
            smd.Add(buttonFingerGuides);

            vPos += 70;

            buttonEasyMode = new pButton(LocalisationManager.GetString(OsuString.DefaultToEasyMode), new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate { DisplayEasyModeDialog(); });
            smd.Add(buttonEasyMode);

            vPos += 60;

            text = new pText(LocalisationManager.GetString(OsuString.Audio), 36, new Vector2(header_x_offset, vPos), 1, true, Color4.White) { Bold = true, TextShadow = true };
            smd.Add(text);

            vPos += 80;

            soundEffectSlider = new SliderControl(LocalisationManager.GetString(OsuString.EffectVolume), AudioEngine.Effect.Volume, new Vector2(button_x_offset - 30, vPos),
                delegate(float v)
                {
                    AudioEngine.Effect.Volume = v;
                    if (Clock.ModeTime / 200 != lastEffectSound)
                    {
                        lastEffectSound = Clock.ModeTime / 200;
                        switch (lastEffectSound % 4)
                        {
                            case 0:
                                AudioEngine.PlaySample(OsuSamples.HitNormal);
                                break;
                            case 1:
                            case 3:
                                AudioEngine.PlaySample(OsuSamples.HitWhistle);
                                break;
                            case 2:
                                AudioEngine.PlaySample(OsuSamples.HitFinish);
                                break;
                        }
                    }
                });
            smd.Add(soundEffectSlider);

            vPos += 60;

            soundEffectSlider = new SliderControl(LocalisationManager.GetString(OsuString.MusicVolume), AudioEngine.Music.MaxVolume, new Vector2(button_x_offset - 30, vPos),
                delegate(float v) { AudioEngine.Music.MaxVolume = v; });
            smd.Add(soundEffectSlider);

            vPos += 60;

            const int offset_range = 200;

            universalOffsetSlider = new SliderControl(LocalisationManager.GetString(OsuString.UniversalOffset), (float)(Clock.USER_OFFSET + offset_range) / (offset_range * 2), new Vector2(button_x_offset - 30, vPos),
                delegate(float v)
                {
                    GameBase.Config.SetValue("offset", (Clock.USER_OFFSET = (int)((v - 0.5f) * offset_range * 2)));
                    if (universalOffsetSlider != null) //will be null on first run.
                        universalOffsetSlider.Text.Text = Clock.USER_OFFSET + "ms";
                });
            smd.Add(universalOffsetSlider);

            vPos += 40;

            text = new pText(LocalisationManager.GetString(OsuString.UniversalOffsetDetails), 24, new Vector2(0, vPos), 1, true, Color4.LightGray) { TextShadow = true };
            text.Field = FieldTypes.StandardSnapTopCentre;
            text.Origin = OriginTypes.TopCentre;
            text.TextAlignment = TextAlignment.Centre;
            text.MeasureText(); //force a measure as this is the last sprite to be added to the draggable area (need height to be precalculated)
            text.TextBounds.X = 600;
            smd.Add(text);

            vPos += (int)text.MeasureText().Y + 50;

            text = new pText(LocalisationManager.GetString(OsuString.OnlineOptions), 36, new Vector2(header_x_offset, vPos), 1, true, Color4.White) { Bold = true, TextShadow = true };
            smd.Add(text);

            vPos += 80;

            if (!GameBase.HasAuth)
            {
                button = new pButton("Connect", new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, HandlePlayerConnect);
                smd.Add(button);

                vPos += 40;

                text = new pText("Please connect for full online functionality, including avatars and the ability to submit scores and view rankings.", 24, new Vector2(0, vPos), 1, true, Color4.LightGray) { TextShadow = true };

                text.Field = FieldTypes.StandardSnapTopCentre;
                text.Origin = OriginTypes.TopCentre;
                text.TextAlignment = TextAlignment.Centre;
                text.MeasureText(); //force a measure as this is the last sprite to be added to the draggable area (need height to be precalculated)
                text.TextBounds.X = 600;

                smd.Add(text);
            }
            else
            {
                button = new pButton($"Disconnect ({GameBase.Config.GetValue<string>("username", null)})", new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, HandlePlayerDisconnect);
                smd.Add(button);
            }

            UpdateButtons();

            vPos += 50;

            smd.ScrollTo(ScrollPosition);
        }

        private void HandlePlayerConnect(object sender, EventArgs args)
        {
#if iOS
            new ConnectInputNotification((bool isOk, string username, string password) =>
            {
                if (isOk)
                {
                    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                    {
                        GameBase.Notify("Failed to Connect!\nPlease enter a Username and Password.");
                        return;
                    }
                    PlayerConnect(username, password);
                }
                else
                {
                    return;
                }
            });

#elif ANDROID

#else
            GameBase.GloballyDisableInput = true;

            string username = "Username";
            string password = "Password";
            if (ShowLoginInputDialog(ref username, ref password) != DialogResult.OK)
            {
                GameBase.GloballyDisableInput = false;
                return;
            }
            PlayerLogin(username, password);
#endif
        }

        private void PlayerConnect(string username, string password)
        {
            string hash = CryptoHelper.GetMd5String(password);
            StringNetRequest nr = new StringNetRequest("https://osustream.its.moe/auth/connect?udid="
                + GameBase.Instance.DeviceIdentifier + "&username=" + username + "&cc=" + hash);
            nr.onFinish += delegate (string _result, Exception e)
            {
#if !(iOS || ANDROID)
                GameBase.GloballyDisableInput = false;
#endif

                if (e == null && !_result.Contains("success") && _result.Contains("hash"))
                {
                    GameBase.Notify("Failed to Connect!\nUsername or Password is incorrect.");
                }
                else if (e == null && !_result.Contains("success") && _result.Contains("link"))
                {
                    GameBase.Notify("Failed to Connect!\nAlready linked to another device.");
                }
                else if (e != null || !_result.Contains("success"))
                {
                    GameBase.Notify("Failed to Connect!\nPlease check you are connected to the internet and try again.");
                }
                else
                {
                    GameBase.Config.SetValue<string>("username", username);
                    GameBase.Config.SetValue<string>("hash", hash);
                    GameBase.Config.SaveConfig();

                    Director.ChangeMode(Director.CurrentOsuMode);
                }
            };

            NetManager.AddRequest(nr);
        }

        private void HandlePlayerDisconnect(object sender, EventArgs args)
        {
                StringNetRequest nr = new StringNetRequest("https://osustream.its.moe/auth/disconnect?username="
                        + GameBase.Config.GetValue<string>("username", null) + "&cc=" + GameBase.Config.GetValue<string>("hash", null));
                nr.onFinish += delegate (string _result, Exception e)
                {
                    GameBase.GloballyDisableInput = false;

                    if (e == null && !_result.Contains("success") && _result.Contains("hash"))
                    {
                        GameBase.Notify("Failed to Disconnect!\nHow'd you get here?\nContact Yoru.");
                    }
                    else if (e != null || !_result.Contains("success"))
                    {
                        GameBase.Notify("Failed to Disconnect!\nPlease check you are connected to the internet and try again.");
                    }
                    else
                    {
                        GameBase.Config.SetValue<string>("username", null);
                        GameBase.Config.SetValue<string>("hash", null);
                        GameBase.Config.SaveConfig();

                        Director.ChangeMode(Director.CurrentOsuMode);
                    }
                };

                GameBase.GloballyDisableInput = true;

                NetManager.AddRequest(nr);
        }

        private int lastEffectSound;
        private pButton buttonFingerGuides;
        private pButton buttonEasyMode;
        private pSprite s_Header;

#if !iOS
        private static DialogResult ShowLoginInputDialog(ref string username, ref string password)
        {
            System.Drawing.Size size = new System.Drawing.Size(200, 78);
            Form inputBox = new Form
            {
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                ControlBox = false,
                ClientSize = size,
                Text = "Login"
            };

            bool firstTime = true;
            TextBox textBox = new TextBox
            {
                Size = new System.Drawing.Size(size.Width - 10, 23),
                Location = new System.Drawing.Point(5, 5),
                Text = username
            };
            textBox.TextChanged += (sender, e) => 
            {
                if (firstTime)
                {
                    textBox.Clear();
                    firstTime = false;
                }
            };
            textBox.LostFocus += (sender, e) =>
            {
                firstTime = true;
            };
            inputBox.Controls.Add(textBox);

            bool firstTime1 = true;
            TextBox textBox1 = new TextBox
            {
                Size = new System.Drawing.Size(size.Width - 10, 23),
                Location = new System.Drawing.Point(5, 28),
                Text = password
            };
            textBox1.TextChanged += (sender, e) =>
            {
                if (firstTime1)
                {
                    textBox1.Clear();
                    firstTime1 = false;
                }
            };
            textBox1.LostFocus += (sender, e) =>
            {
                firstTime1 = true;
            };
            inputBox.Controls.Add(textBox1);

            Button okButton = new Button
            {
                DialogResult = DialogResult.OK,
                Name = "okButton",
                Size = new System.Drawing.Size(75, 23),
                Text = "&OK",
                Location = new System.Drawing.Point(size.Width - 80 - 80, 50)
            };
            okButton.Click += (sender, e) => inputBox.DialogResult = DialogResult.OK;
            inputBox.Controls.Add(okButton);

            Button cancelButton = new Button
            {
                DialogResult = DialogResult.Cancel,
                Name = "cancelButton",
                Size = new System.Drawing.Size(75, 23),
                Text = "&Cancel",
                Location = new System.Drawing.Point(size.Width - 80, 50)
            };
            cancelButton.Click += (sender, e) => inputBox.DialogResult = DialogResult.Cancel;
            inputBox.Controls.Add(cancelButton);

            inputBox.AcceptButton = okButton;
            inputBox.CancelButton = cancelButton;

            DialogResult result = inputBox.ShowDialog();
            username = textBox.Text;
            password = textBox1.Text;
            return result;
        }

        private static void TextBox_TextChanged(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
#endif
        internal static void DisplayFingerGuideDialog()
        {
            Notification notification = new Notification(LocalisationManager.GetString(OsuString.UseFingerGuides), LocalisationManager.GetString(OsuString.UseGuideFingers_Explanation),
                NotificationStyle.YesNo,
                delegate(bool yes)
                {
                    GameBase.Config.SetValue(@"GuideFingers", yes);

                    if (Director.CurrentMode is Options o) o.UpdateButtons();
                });
            GameBase.Notify(notification);
        }

        private void UpdateButtons()
        {
            buttonEasyMode.SetStatus(GameBase.Config.GetValue(@"EasyMode", false));
            buttonFingerGuides.SetStatus(GameBase.Config.GetValue(@"GuideFingers", false));
        }

        internal static void DisplayEasyModeDialog()
        {
            Notification notification = new Notification(LocalisationManager.GetString(OsuString.DefaultToEasyMode), LocalisationManager.GetString(OsuString.DefaultToEasyMode_Explanation),
                NotificationStyle.YesNo,
                delegate(bool yes)
                {
                    GameBase.Config.SetValue(@"EasyMode", yes);

                    if (Director.CurrentMode is Options o) o.UpdateButtons();
                });
            GameBase.Notify(notification);
        }

        public override void Dispose()
        {
            ScrollPosition = smd.ScrollPosition;

            GameBase.Config.SetValue("VolumeEffect", (int)(AudioEngine.Effect.Volume * 100));
            GameBase.Config.SetValue("VolumeMusic", (int)(AudioEngine.Music.MaxVolume * 100));
            GameBase.Config.SaveConfig();

            topMostSpriteManager.Dispose();

            smd.Dispose();
            base.Dispose();
        }

        public override bool Draw()
        {
            base.Draw();
            smd.Draw();
            topMostSpriteManager.Draw();
            return true;
        }

        public override void Update()
        {
            s_Header.Position.Y = Math.Min(0, -smd.ScrollPercentage * 20);

            smd.Update();
            base.Update();
            topMostSpriteManager.Update();
        }
    }
}