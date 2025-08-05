using TapSDK.Core;

namespace TapSDK.CloudSave.Internal
{
    public interface ITapCloudSaveBridge
    {
        void Init(TapTapSdkOptions options);

        void RegisterCloudSaveCallback(ITapCloudSaveCallback callback);

        void CreateArchive(ArchiveMetadata metadata, string archiveFilePath, string archiveCoverPath,
            ITapCloudSaveRequestCallback callback);

        void UpdateArchive(string archiveUuid, ArchiveMetadata metadata, string archiveFilePath, string archiveCoverPath,
            ITapCloudSaveRequestCallback callback);

        void DeleteArchive(string archiveUuid, ITapCloudSaveRequestCallback callback);
        void GetArchiveList(ITapCloudSaveRequestCallback callback);
        void GetArchiveData(string archiveUuid, string archiveFileId, ITapCloudSaveRequestCallback callback);
        void GetArchiveCover(string archiveUuid, string archiveFileId, ITapCloudSaveRequestCallback callback);
    }
}