#if ANDROID
using osum.GameModes.SongSelect;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace osum.UI
{
    public sealed class BeatmapImportAndroid
    {
        private const long MIN_VALID_PACKAGE_SIZE = 4096; // osu!stream packages are never tiny

        private readonly Action<bool> completion;

        public BeatmapImportAndroid(Action<bool> completion)
        {
            this.completion = completion;
            _ = RunAsync();
        }

        private async Task RunAsync()
        {
            try
            {
                var results = await FilePicker.PickMultipleAsync();
                if (results == null || !results.Any())
                {
                    completion?.Invoke(false);
                    return;
                }

                Directory.CreateDirectory(SongSelectMode.BeatmapPath);

                bool importedAny = false;

                foreach (var file in results)
                {
                    if (!IsSupported(file.FileName))
                        continue;

                    string destination = Path.Combine(SongSelectMode.BeatmapPath, file.FileName);

                    if (!PrepareDestination(destination))
                        continue;

                    if (await ImportFileAsync(file, destination))
                        importedAny = true;
                }

                SongSelectMode.ForceBeatmapRefresh = importedAny;
                completion?.Invoke(importedAny);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[IMPORT] Fatal error: " + ex);
                completion?.Invoke(false);
            }
        }

        private static bool IsSupported(string fileName)
        {
            string ext = Path.GetExtension(fileName)?.ToLowerInvariant();
            return ext == ".osz2" || ext == ".osf2";
        }

        private static bool PrepareDestination(string path)
        {
            if (!File.Exists(path))
                return true;

            try
            {
                if (new FileInfo(path).Length >= MIN_VALID_PACKAGE_SIZE)
                    return false;

                File.Delete(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<bool> ImportFileAsync(FileResult file, string destination)
        {
            string tempPath = destination + ".importing";

            try
            {
                using (var input = await file.OpenReadAsync())
                using (var output = File.Create(tempPath))
                {
                    await input.CopyToAsync(output);
                }

                if (new FileInfo(tempPath).Length < MIN_VALID_PACKAGE_SIZE)
                {
                    File.Delete(tempPath);
                    return false;
                }

                File.Move(tempPath, destination);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[IMPORT] Failed: " + ex);

                try { File.Delete(tempPath); } catch { }
                return false;
            }
        }
    }
}
#endif
