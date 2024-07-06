using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using OpenTK;
using OpenTK.Graphics;
using osum.Audio;
using osum.GameModes.SongSelect;
using osum.GameplayElements;
using osum.GameplayElements.Beatmaps;
using osum.Graphics;
using osum.Graphics.Sprites;
using osum.Helpers;
using osum.Libraries.NetLib;
using osum.Localisation;

namespace osum.GameModes.Store
{
    public class PackPanel : pSpriteCollection
    {
        internal const string RESTORE_PACK_ID = "restore";

        internal pDrawable s_BackingPlate;
        internal pDrawable s_BackingPlate2;
        internal pText s_Text;
        internal pText s_Price;
        internal pSprite s_PriceBackground;
        internal pSprite s_Thumbnail;

        private readonly float base_depth = 0.6f;

        private static readonly Color4 colourHover = new Color4(28, 139, 242, 255);
        private static readonly Color4 colourVideoPreviewNormal = new Color4(255, 255, 255, 40);

        internal const int PANEL_HEIGHT = 60;
        internal const int ITEM_HEIGHT = 40;

        internal float CondensedHeight = PANEL_HEIGHT + 10;
        internal float ExpandedHeight = PANEL_HEIGHT + 4;

        internal List<pDrawable> PackItemSprites = new List<pDrawable>();

        private bool expanded;

        internal bool Expanded
        {
            get => expanded;
            set
            {
                if (value == expanded || Downloading) return;

                expanded = value;

                if (expanded)
                {
                    s_BackingPlate.HandleInput = false;

                    s_PriceBackground.FadeIn(100);
                    s_PriceBackground.Transform(new TransformationF(TransformationType.Fade, 1, 0.6f, 100, 1500, EasingTypes.Out) { Looping = true, LoopDelay = 300 });
                    s_PriceBackground.Transform(new TransformationF(TransformationType.Fade, 0.6f, 1, 1500, 1800, EasingTypes.In) { Looping = true, LoopDelay = 1400 });

                    s_Price.FadeColour(Color4.White, 100);
                    s_PriceBackground.HandleInput = true;

                    s_BackingPlate2.FadeColour(Color4.White, 0);

                    PackItemSprites.ForEach(s => s.FadeIn(300));
                }
                else
                {
                    s_BackingPlate.HandleInput = true;

                    s_BackingPlate.FadeIn(100);
                    s_BackingPlate2.FadeColour(Color4.Transparent, 100);

                    s_PriceBackground.Transformations.Clear();
                    s_PriceBackground.FadeOut(100);

                    s_Price.FadeColour(new Color4(255, 255, 255, 128), 100);
                    s_PriceBackground.HandleInput = false;

                    PackItemSprites.ForEach(s => s.FadeOut(100));
                }
            }
        }

        internal float Height => Expanded ? ExpandedHeight : CondensedHeight;

        private readonly List<pDrawable> songPreviewBacks = new List<pDrawable>();
        private readonly List<pSprite> songPreviewButtons = new List<pSprite>();
        internal List<PackItem> PackItems = new List<PackItem>();

        private bool isPreviewing;
        private DataNetRequest previewRequest;

        private readonly pSprite s_LoadingPrice;

        internal int BeatmapCount => PackItems.Count;

        private int currentDownload;

#if iOS || ANDROID
        const string PREFERRED_FORMAT = "m4a";
#else
        private const string PREFERRED_FORMAT = "mp3";
#endif

        public string PackId;
        public bool IsFree;
        public bool Ready;
        public byte[] Receipt;

        public PackPanel(string packTitle, string packId, bool free)
        {
            PackId = packId;
            IsFree = true;

            Ready = IsFree;

            Sprites.Add(s_BackingPlate = new pSprite(TextureManager.Load(OsuTexture.songselect_panel), Vector2.Zero)
            {
                DrawDepth = base_depth,
                Colour = new Color4(255, 255, 255, 170),
                HandleClickOnUp = true
            });

            s_BackingPlate.OnClick += delegate
            {
                if (!Downloading)
                    StoreMode.ShowPack(this);
            };

            s_BackingPlate.OnHover += delegate
            {
                if (Downloading) return;

                s_BackingPlate.FadeOut(100, 0.01f);
                s_BackingPlate2.FadeColour(BeatmapPanel.BACKGROUND_COLOUR, 80);
            };

            s_BackingPlate.OnHoverLost += delegate
            {
                if (Downloading || isPreviewing) return;

                s_BackingPlate.FadeIn(60);
                s_BackingPlate2.FadeColour(Color4.Transparent, 100);
            };

            Sprites.Add(s_BackingPlate2 = new pSprite(TextureManager.Load(OsuTexture.songselect_panel_selected), Vector2.Zero)
            {
                DrawDepth = base_depth + 0.01f,
                Colour = new Color4(255, 255, 255, 0)
            });

            Sprites.Add(s_Text = new pText(packTitle, 32, Vector2.Zero, Vector2.Zero, base_depth + 0.02f, true, Color4.White, false)
            {
                Bold = true,
                Offset = new Vector2(130, 14)
            });

            Sprites.Add(s_PriceBackground = new pSprite(TextureManager.Load(OsuTexture.songselect_store_buy_background), FieldTypes.StandardSnapRight, OriginTypes.TopRight, ClockTypes.Mode, Vector2.Zero, base_depth + 0.02f, true, Color4.White)
            {
                Alpha = 0,
                Offset = new Vector2(1, 1)
            });
            s_PriceBackground.OnClick += onPurchase;

            Sprites.Add(s_Price = new pText(IsFree ? LocalisationManager.GetString(OsuString.Free) : null, 40, Vector2.Zero, Vector2.Zero, base_depth + 0.03f, true, new Color4(255, 255, 255, 128), false)
            {
                Origin = OriginTypes.Centre,
                Field = FieldTypes.StandardSnapRight,
                Offset = new Vector2(75 + GameBase.SuperWidePadding, 30)
            });

            if (!IsFree && PackId != RESTORE_PACK_ID)
            {
                s_LoadingPrice = new pSprite(TextureManager.Load(OsuTexture.songselect_audio_preview), FieldTypes.StandardSnapRight, OriginTypes.Centre, ClockTypes.Mode, Vector2.Zero, base_depth + 0.04f, true, Color4.White)
                {
                    Offset = new Vector2(75 + GameBase.SuperWidePadding, 30),
                    ExactCoordinates = false
                };
                s_LoadingPrice.Transform(new TransformationF(TransformationType.Rotation, 0, MathHelper.Pi * 2, Clock.ModeTime, Clock.ModeTime + 2000) { Looping = true });
                Sprites.Add(s_LoadingPrice);
            }

            if (PackId == RESTORE_PACK_ID)
            {
                Sprites.Add(s_Thumbnail = new pSprite(TextureManager.Load(OsuTexture.songselect_thumb_restore), Vector2.Zero)
                {
                    DrawDepth = base_depth + 0.02f,
                    Offset = new Vector2(38.5f, 3.8f)
                });
            }
            else
            {
                Sprites.Add(s_Thumbnail = new pSpriteWeb("https://osustream.its.moe/dl/preview?filename=" + PackId + "&format=jpg")
                {
                    DrawDepth = base_depth + 0.02f,
                    Offset = new Vector2(38.5f, 3.8f)
                });
            }
        }

        public void SetPrice(string price, bool isFree = false)
        {
            if (s_LoadingPrice != null)
            {
                s_LoadingPrice.FadeOut(100);
                s_LoadingPrice.AlwaysDraw = false;
            }

            Ready = true;
            s_Price.Text = price;
            IsFree = isFree;
        }

        private void onPurchase(object sender, EventArgs e)
        {
            StoreMode.PurchaseInitiated(this);
        }

        public void Download()
        {
            Downloading = true;

            if (isPreviewing)
                StoreMode.ResetAllPreviews(true);

            startNextDownload();

            s_PriceBackground.Transformations.Clear();
            s_PriceBackground.FadeOut(100);
            s_Price.FadeOut(100);

            s_BackingPlate.HandleInput = false;

            songPreviewButtons.ForEach(b => b.FadeOut(100));
            songPreviewBacks.ForEach(b =>
            {
                b.FadeOut(0);
                b.HandleInput = false;
                b.Colour = Color4.OrangeRed;
            });
        }

        private void startNextDownload()
        {
            Downloading = true;

            lock (PackItems)
            {
                if (currentDownload >= PackItems.Count)
                {
                    Downloading = false;
                    return;
                }

                PackItem item = PackItems[currentDownload];
                pDrawable back = songPreviewBacks[currentDownload];

                string path = SongSelectMode.BeatmapPath + "/" + item.Filename;

                string receipt64 = Receipt != null ? Convert.ToBase64String(Receipt) : "";

                string downloadPath = "https://osustream.its.moe/dl/download";
                string param = "pack=" + PackId + "&filename=" + NetRequest.UrlEncode(item.Filename) + "&id=" + GameBase.Instance.DeviceIdentifier + "&recp=" + receipt64;
                if (item.UpdateChecksum != null)
                    param += "&update=" + item.UpdateChecksum;
#if DEBUG
                Console.WriteLine("Downloading " + downloadPath);
                Console.WriteLine("param " + param);
#endif

                FileNetRequest fnr = new FileNetRequest(path, downloadPath, "POST", param);
                fnr.onFinish += delegate
                {
                    BeatmapDatabase.PopulateBeatmap(new Beatmap(path)); //record the new download in our local database.
                    BeatmapDatabase.Write();

#if iOS
                    if (UIKit.UIDevice.CurrentDevice.SystemVersion.StartsWith("5."))
                        Foundation.NSFileManager.SetSkipBackupAttribute(downloadPath,true);
#endif

                    SongSelectMode.ForceBeatmapRefresh = true; //can optimise this away in the future.

                    back.FadeColour(Color4.LimeGreen, 500);

                    lock (PackItems)
                    {
                        currentDownload++;
                        if (currentDownload < PackItems.Count)
                            startNextDownload();
                        else
                        {
                            Downloading = false;
                            GameBase.Scheduler.Add(delegate { StoreMode.DownloadComplete(this); });
                        }
                    }
                };

                back.Transform(new TransformationF(TransformationType.Fade, 1, 0, Clock.ModeTime, Clock.ModeTime + 700) { Looping = true });

                fnr.onUpdate += delegate(object sender, long current, long total)
                {
                    if (back.Alpha != 1)
                    {
                        GameBase.Scheduler.Add(delegate
                        {
                            back.Transformations.Clear();
                            back.Alpha = 1;
                        }, true);
                    }

                    back.Scale.X = GameBase.BaseSize.X * ((float)current / total);
                };

                NetManager.AddRequest(fnr);
            }
        }

        internal void ResetPreviews()
        {
            if (previewRequest != null)
            {
                previewRequest.Abort();
                previewRequest = null;
            }

            if (!isPreviewing) return;

            isPreviewing = false;

            foreach (pSprite p in songPreviewButtons)
            {
                switch (p.Texture.OsuTextureInfo)
                {
                    case OsuTexture.songselect_audio_pause:
                    case OsuTexture.songselect_audio_preview:
                        p.ExactCoordinates = true;
                        p.Texture = TextureManager.Load(OsuTexture.songselect_audio_play);
                        p.Transformations.RemoveAll(t => t.Looping);
                        p.Rotation = 0;
                        break;
                    case OsuTexture.songselect_video:
                        p.FadeColour(colourVideoPreviewNormal, 200);
                        break;
                }
            }

            if (Downloading) return;

            foreach (pDrawable p in songPreviewBacks)
            {
                p.FadeColour(new Color4(40, 40, 40, 0), 200);
                p.TagNumeric = 0;
            }
        }

        internal void AddItem(PackItem item)
        {
            pSprite preview = new pSprite(TextureManager.Load(OsuTexture.songselect_audio_play), Vector2.Zero) { DrawDepth = base_depth + 0.02f, Origin = OriginTypes.Centre };
            preview.Offset = new Vector2(GameBase.SuperWidePadding + 28, ExpandedHeight + 20);

            Sprites.Add(preview);
            PackItemSprites.Add(preview);
            songPreviewButtons.Add(preview);

            pSprite videoPreview = null;

            pRectangle back = new pRectangle(Vector2.Zero, new Vector2(GameBase.BaseSizeFixedWidth.X, 40), true, base_depth, new Color4(40, 40, 40, 0));
            PackItemSprites.Add(back);
            back.HandleClickOnUp = true;

            back.OnHover += delegate
            {
                if (back.TagNumeric != 1) back.FadeColour(new Color4(40, 40, 40, 255), 200);
            };
            back.OnHoverLost += delegate
            {
                if (back.TagNumeric != 1) back.FadeColour(new Color4(40, 40, 40, 0), 200);
            };

            back.OnClick += delegate
            {
                bool isPausing = back.TagNumeric == 1;

                StoreMode.ResetAllPreviews(isPausing);

                if (isPausing) return;

                AudioEngine.Music.Stop();

                AudioEngine.PlaySample(OsuSamples.MenuClick);

                previewRequest?.Abort();

                string downloadPath = "https://osustream.its.moe/dl/preview";
                string param = "pack=" + PackId + "&filename=" + NetRequest.UrlEncode(item.Filename) + "&format=" + PREFERRED_FORMAT;
                previewRequest = new DataNetRequest(downloadPath, "POST", param);
                previewRequest.onFinish += delegate(byte[] data, Exception ex)
                {
                    if (previewRequest.AbortRequested) return;

                    GameBase.Scheduler.Add(delegate
                    {
                        if (ex != null || data == null || data.Length < 10000)
                        {
                            StoreMode.ResetAllPreviews(true);
                            GameBase.Notify(LocalisationManager.GetString(OsuString.InternetFailed));
                            return;
                        }

                        preview.Transformations.RemoveAll(t => t.Type == TransformationType.Rotation);
                        preview.Rotation = 0;

                        StoreMode.PlayPreview(data);
                        preview.ExactCoordinates = true;
                        preview.Texture = TextureManager.Load(OsuTexture.songselect_audio_pause);
                    });
                };
                NetManager.AddRequest(previewRequest);

                back.FadeColour(colourHover, 0);
                back.Transform(new TransformationV(new Vector2(back.Scale.X, 0), back.Scale, Clock.ModeTime, Clock.ModeTime + 200, EasingTypes.In) { Type = TransformationType.VectorScale });
                back.TagNumeric = 1;

                preview.Texture = TextureManager.Load(OsuTexture.songselect_audio_preview);
                preview.ExactCoordinates = false;
                preview.Transform(new TransformationF(TransformationType.Rotation, 0, MathHelper.Pi * 2, Clock.ModeTime, Clock.ModeTime + 1000) { Looping = true });
                isPreviewing = true;

                videoPreview.FadeColour(Color4.White, 200);
            };

            songPreviewBacks.Add(back);

            back.Origin = OriginTypes.CentreLeft;
            back.Offset = new Vector2(0, ExpandedHeight + 20);
            Sprites.Add(back);

            PackItems.Add(item);

            string artistString;
            string titleString;
            float textOffset = 50 + GameBase.SuperWidePadding;

            if (item.Title == null)
            {
                //ooold fallback; probably not needed anymore
                Regex r = new Regex(@"(.*) - (.*) \((.*)\)");
                Match m = r.Match(Path.GetFileNameWithoutExtension(item.Filename));

                artistString = m.Groups[1].Value;
                titleString = m.Groups[2].Value;
            }
            else
            {
                artistString = item.Title.Substring(0, item.Title.IndexOf('-')).Trim();
                titleString = item.Title.Substring(item.Title.IndexOf('-') + 1).Trim();
            }

            pText artist = new pText(artistString, 26, Vector2.Zero, Vector2.Zero, base_depth + 0.01f, true, Color4.SkyBlue, false);
            artist.Bold = true;
            artist.Offset = new Vector2(textOffset, ExpandedHeight + 4);
            PackItemSprites.Add(artist);
            Sprites.Add(artist);

            pText title = new pText(titleString, 26, Vector2.Zero, Vector2.Zero, base_depth + 0.01f, true, Color4.White, false);

            title.Offset = new Vector2(textOffset + 15 + artist.MeasureText().X / GameBase.BaseToNativeRatioAligned, ExpandedHeight + 4);
            PackItemSprites.Add(title);
            Sprites.Add(title);

            videoPreview = new pSprite(TextureManager.Load(OsuTexture.songselect_video), Vector2.Zero)
            {
                DrawDepth = base_depth + 0.02f,
                Field = FieldTypes.StandardSnapRight,
                Origin = OriginTypes.CentreRight,
                Colour = colourVideoPreviewNormal
            };
            videoPreview.Offset = new Vector2(10 + GameBase.SuperWidePadding, ExpandedHeight + 20);
            videoPreview.OnClick += delegate
            {
                if (back.TagNumeric != 1)
                {
                    //make sure the line is selected first.
                    back.Click();
                    return;
                }

                AudioEngine.PlaySample(OsuSamples.MenuHit);
                StoreMode.ResetAllPreviews(true);
                VideoPreview.DownloadLink = "https://osustream.its.moe/dl/download?pack=" + PackId + "&filename=" + NetRequest.UrlEncode(item.Filename) + "&id=" + GameBase.Instance.DeviceIdentifier + "&preview=1";
                Director.ChangeMode(OsuMode.VideoPreview, true);
            };

            Sprites.Add(videoPreview);
            PackItemSprites.Add(videoPreview);
            songPreviewButtons.Add(videoPreview);

            ExpandedHeight += ITEM_HEIGHT;
        }

        public bool Downloading { get; private set; }

        internal void StartPreviewing()
        {
            if (!isPreviewing)
                songPreviewBacks[0].Click();
        }
    }

    public class PackItem
    {
        public string Filename;
        public string UpdateChecksum;
        public string Title;

        public PackItem(string filename, string title, string updateChecksum = null)
        {
            Filename = filename;
            UpdateChecksum = updateChecksum;
            Title = title;
        }
    }
}