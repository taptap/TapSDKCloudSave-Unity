using TapSDK.CloudSave.Internal;

namespace TapSDK.CloudSave
{
    public class TapTapCloudSave
    {
        public static void RegisterCloudSaveCallback(ITapCloudSaveCallback callback)
        {
            TapTapCloudSaveInternal.RegisterCloudSaveCallback(callback);
        }

        public static void CreateArchive(string metadata, string archiveFilePath, string archiveCoverPath,
            ITapCloudSaveRequestCallback callback)
        {
            TapTapCloudSaveInternal.CreateArchive(metadata, archiveFilePath, archiveCoverPath, callback);
        }

        public static void UpdateArchive(string archiveUuid, string metadata, string archiveFilePath,
            string archiveCoverPath, ITapCloudSaveRequestCallback callback)
        {
            TapTapCloudSaveInternal.UpdateArchive(archiveUuid, metadata, archiveFilePath, archiveCoverPath, callback);
        }

        public static void DeleteArchive(string archiveUuid, ITapCloudSaveRequestCallback callback)
        {
            TapTapCloudSaveInternal.DeleteArchive(archiveUuid, callback);
        }

        public static void GetArchiveList(ITapCloudSaveRequestCallback callback)
        {
            TapTapCloudSaveInternal.GetArchiveList(callback);
        }

        public static void GetArchiveData(string archiveUuid, string archiveFileId,
            ITapCloudSaveRequestCallback callback)
        {
            TapTapCloudSaveInternal.GetArchiveData(archiveUuid, archiveFileId, callback);
        }

        public static void GetArchiveCover(string archiveUuid, string archiveFileId,
            ITapCloudSaveRequestCallback callback)
        {
            TapTapCloudSaveInternal.GetArchiveCover(archiveUuid, archiveFileId, callback);
        }
    }
}