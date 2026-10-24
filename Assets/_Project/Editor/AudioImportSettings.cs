using UnityEditor;
using UnityEngine;

namespace DBP.Editor
{
    public sealed class AudioImportSettings : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/_Project/Resources/Audio/", System.StringComparison.Ordinal)) return;
            var importer = (AudioImporter)assetImporter;
            bool music = System.IO.Path.GetFileName(assetPath).StartsWith("mission_", System.StringComparison.Ordinal);
            importer.forceToMono = !music;
            var settings = importer.defaultSampleSettings;
            settings.preloadAudioData = !music;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.quality = .7f;
            importer.defaultSampleSettings = settings;
        }
    }
}
