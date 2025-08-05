using TapSDK.Core;
using TapSDK.Core.Internal.Utils;

namespace TapSDK.CloudSave.Internal
{
    internal static class TapTapCloudSaveInternal
    {
        private static readonly ITapCloudSaveBridge Bridge;

        static TapTapCloudSaveInternal()
        {
            Bridge = BridgeUtils.CreateBridgeImplementation(typeof(ITapCloudSaveBridge), "TapSDK.CloudSave")
                as ITapCloudSaveBridge;
        }

        internal static void Init(TapTapSdkOptions options)
        {
            Bridge?.Init(options);
        }

        internal static void RegisterCloudSaveCallback(ITapCloudSaveCallback callback)
        {
            Bridge?.RegisterCloudSaveCallback(callback);
        }

        internal static void CreateArchive(ArchiveMetadata metadata, string archiveFilePath, string archiveCoverPath,
            ITapCloudSaveRequestCallback callback)
        {
            Bridge?.CreateArchive(metadata, archiveFilePath, archiveCoverPath, callback);
        }

        internal static void UpdateArchive(string archiveUuid, ArchiveMetadata metadata, string archiveFilePath,
            string archiveCoverPath, ITapCloudSaveRequestCallback callback)
        {
            Bridge?.UpdateArchive(archiveUuid, metadata, archiveFilePath, archiveCoverPath, callback);
        }

        internal static void DeleteArchive(string archiveUuid, ITapCloudSaveRequestCallback callback)
        {
            Bridge?.DeleteArchive(archiveUuid, callback);
        }

        internal static void GetArchiveList(ITapCloudSaveRequestCallback callback)
        {
            Bridge?.GetArchiveList(callback);
        }

        internal static void GetArchiveData(string archiveUuid, string archiveFileId,
            ITapCloudSaveRequestCallback callback)
        {
            Bridge?.GetArchiveData(archiveUuid, archiveFileId, callback);
        }

        internal static void GetArchiveCover(string archiveUuid, string archiveFileId,
            ITapCloudSaveRequestCallback callback)
        {
            Bridge?.GetArchiveCover(archiveUuid, archiveFileId, callback);
        }
    }
}