using System.Collections.Generic;
using TapSDK.Core;
using System;
using Newtonsoft.Json;
using TapSDK.CloudSave.Internal;

namespace TapSDK.CloudSave.Mobile
{
    public class ErrorResponse
    {
        [JsonProperty("errorCode")] public int ErrorCode { get; set; }
        [JsonProperty("errorMessage")] public string ErrorMessage { get; set; }
    }

    public class TapCloudSaveBridge : ITapCloudSaveBridge
    {
        public static string TAP_CLOUDSAVE_SERVICE = "BridgeCloudSaveService";

        public static string TDS_CLOUDSAVE_SERVICE_CLZ = "com.taptap.sdk.cloudsave.unity.BridgeCloudSaveService";

        public static string TDS_CLOUDSAVE_SERVICE_IMPL = "com.taptap.sdk.cloudsave.unity.BridgeCloudSaveServiceImpl";

        public TapCloudSaveBridge()
        {
            EngineBridge.GetInstance().Register(TDS_CLOUDSAVE_SERVICE_CLZ, TDS_CLOUDSAVE_SERVICE_IMPL);
        }

        public void Init(TapTapSdkOptions options)
        {
            // 原生由原生内部实现
        }

        public void RegisterCloudSaveCallback(ITapCloudSaveCallback callback)
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(TAP_CLOUDSAVE_SERVICE)
                .Method("registerCloudSaveCallback")
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (response) =>
            {
                if (callback == null) return;

                try
                {
                    if (response.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(response.content))
                    {
                        callback.OnResult(-1);
                        return;
                    }

                    var result = JsonConvert.DeserializeObject<TapEngineBridgeResult>(response.content);
                    if (result != null && result.code == TapEngineBridgeResult.RESULT_SUCCESS)
                    {
                        var resultCode = JsonConvert.DeserializeObject<int>(result.content);
                        if (resultCode != null)
                        {
                            callback.OnResult(resultCode);
                        }
                        else
                        {
                            callback.OnResult(-1);
                        }
                    }
                    else
                    {
                        callback.OnResult(-1);
                    }
                }
                catch (Exception e)
                {
                    callback.OnResult(-1);
                }
            });
        }

        public void CreateArchive(ArchiveMetadata metadata, string archiveFilePath, string archiveCoverPath, ITapCloudSaveRequestCallback callback)
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(TAP_CLOUDSAVE_SERVICE)
                .Method("createArchive")
                .Args("archiveMetadata",  JsonConvert.SerializeObject(metadata))
                .Args("archiveFilePath",  archiveFilePath)
                .Args("archiveCoverPath",  archiveCoverPath)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (response) =>
            {
                if (callback == null) return;

                try
                {
                    if (response.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(response.content))
                    {
                        callback.OnRequestError(-1, "Failed to create archive: code="+response.code + "content="+response.content);
                        return;
                    }
                    
                    var result = JsonConvert.DeserializeObject<TapEngineBridgeResult>(response.content);
                    if (result != null && result.code == TapEngineBridgeResult.RESULT_SUCCESS)
                    {
                        var archive = JsonConvert.DeserializeObject<ArchiveData>(result.content);
                        if (archive != null)
                        {
                            callback.OnArchiveCreated(archive);
                        }
                        else
                        {
                            callback.OnRequestError(-1, "json convert failed: content="+result.content);
                        }
                    }
                    else
                    {
                        try
                        {
                            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(result.content);
                            if (errorResponse != null)
                            {
                                callback.OnRequestError(errorResponse.ErrorCode, errorResponse.ErrorMessage);
                            }
                            else
                            {
                                callback.OnRequestError(-1, "Failed to create archive: content="+response.content);
                            }
                        }
                        catch (Exception e)
                        {
                            callback.OnRequestError(-1, "Failed to create archive: content="+response.content);
                        }
                    }
                }
                catch (Exception e)
                {
                    callback.OnRequestError(-1, "Failed to create archive: error=" + e.Message + ", content=" + response.content);
                }
            });
        }
        
        public void UpdateArchive(string archiveUuid, ArchiveMetadata metadata, string archiveFilePath, string archiveCoverPath, ITapCloudSaveRequestCallback callback)
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(TAP_CLOUDSAVE_SERVICE)
                .Method("updateArchive")
                .Args("archiveUUIDForUpdate",  archiveUuid)
                .Args("archiveMetadataForUpdate",  JsonConvert.SerializeObject(metadata))
                .Args("archiveFilePathForUpdate",  archiveFilePath)
                .Args("archiveCoverPathForUpdate",  archiveCoverPath)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (response) =>
            {
                if (callback == null) return;

                try
                {
                    if (response.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(response.content))
                    {
                        callback.OnRequestError(-1, "Failed to update archive: code="+response.code + "content="+response.content);
                        return;
                    }
                    
                    var result = JsonConvert.DeserializeObject<TapEngineBridgeResult>(response.content);
                    if (result != null && result.code == TapEngineBridgeResult.RESULT_SUCCESS)
                    {
                        var archive = JsonConvert.DeserializeObject<ArchiveData>(result.content);
                        if (archive != null)
                        {
                            callback.OnArchiveUpdated(archive);
                        }
                        else
                        {
                            callback.OnRequestError(-1, "json convert failed: content="+result.content);
                        }
                    }
                    else
                    {
                        try
                        {
                            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(result.content);
                            if (errorResponse != null)
                            {
                                callback.OnRequestError(errorResponse.ErrorCode, errorResponse.ErrorMessage);
                            }
                            else
                            {
                                callback.OnRequestError(-1, "Failed to update archive: content="+response.content);
                            }
                        }
                        catch (Exception e)
                        {
                            callback.OnRequestError(-1, "Failed to update archive: content="+response.content);
                        }
                    }
                }
                catch (Exception e)
                {
                    callback.OnRequestError(-1, "Failed to update archive: error=" + e.Message + ", content=" + response.content);
                }
            });
        }
        
        public void DeleteArchive(string archiveUuid, ITapCloudSaveRequestCallback callback)
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(TAP_CLOUDSAVE_SERVICE)
                .Method("deleteArchive")
                .Args("archiveUUID",  archiveUuid)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (response) =>
            {
                if (callback == null) return;

                try
                {
                    if (response.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(response.content))
                    {
                        callback.OnRequestError(-1, "Failed to delete archive: code="+response.code + "content="+response.content);
                        return;
                    }

                    var result = JsonConvert.DeserializeObject<TapEngineBridgeResult>(response.content);
                    if (result != null && result.code == TapEngineBridgeResult.RESULT_SUCCESS)
                    {
                        var archive = JsonConvert.DeserializeObject<ArchiveData>(result.content);
                        if (archive != null)
                        {
                            callback.OnArchiveDeleted(archive);
                        }
                        else
                        {
                            callback.OnRequestError(-1, "json convert failed: content="+response.content);
                        }
                    }
                    else
                    {
                        try
                        {
                            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(result.content);
                            if (errorResponse != null)
                            {
                                callback.OnRequestError(errorResponse.ErrorCode, errorResponse.ErrorMessage);
                            }
                            else
                            {
                                callback.OnRequestError(-1, "Failed to delete archive: content="+response.content);
                            }
                        }
                        catch (Exception e)
                        {
                            callback.OnRequestError(-1, "Failed to delete archive: content="+response.content);
                        }
                    }
                }
                catch (Exception e)
                {
                    callback.OnRequestError(-1, "Failed to delete archive: error=" + e.Message + ", content=" + response.content);
                }
            });
        }

        public void GetArchiveList(ITapCloudSaveRequestCallback callback)
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(TAP_CLOUDSAVE_SERVICE)
                .Method("getArchiveList")
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (response) =>
            {
                if (callback == null) return;

                try
                {
                    if (response.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(response.content))
                    {
                        callback.OnRequestError(-1, "Failed to get archive list: code="+response.code + "content="+response.content);
                        return;
                    }
                    
                    var result = JsonConvert.DeserializeObject<TapEngineBridgeResult>(response.content);
                    if (result != null && result.code == TapEngineBridgeResult.RESULT_SUCCESS)
                    {
                        var archiveList = JsonConvert.DeserializeObject<List<ArchiveData>>(result.content);
                        if (archiveList != null)
                        {
                            callback.OnArchiveListResult(archiveList);
                        }
                        else
                        {
                            callback.OnRequestError(-1, "json convert failed: content="+response.content);
                        }
                    }
                    else
                    {
                        try
                        {
                            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(result.content);
                            if (errorResponse != null)
                            {
                                callback.OnRequestError(errorResponse.ErrorCode, errorResponse.ErrorMessage);
                            }
                            else
                            {
                                callback.OnRequestError(-1, "Failed to get archive list: content="+response.content);
                            }
                        }
                        catch (Exception e)
                        {
                            callback.OnRequestError(-1, "Failed to get archive list: content="+response.content);
                        }
                    }
                }
                catch (Exception e)
                {
                    callback.OnRequestError(-1, "Failed to get archive list: error=" + e.Message  + ", content=" + response.content);
                }
            });
        }
        
        public void GetArchiveData(string archiveUuid, string archiveFileId, ITapCloudSaveRequestCallback callback)
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(TAP_CLOUDSAVE_SERVICE)
                .Method("getArchiveData")
                .Args("archiveUUID",  archiveUuid)
                .Args("archiveFileID",  archiveFileId)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (response) =>
            {
                if (callback == null) return;

                try
                {
                    if (response.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(response.content))
                    {
                        callback.OnRequestError(-1, "Failed to get archive data: code=" + response.code + "content="+response.content);
                        return;
                    }

                    var result = JsonConvert.DeserializeObject<TapEngineBridgeResult>(response.content);
                    if (result != null && result.code == TapEngineBridgeResult.RESULT_SUCCESS)
                    {
                        var archiveData = Convert.FromBase64String(result.content);
                        if (archiveData != null)
                        {
                            callback.OnArchiveDataResult(archiveData);
                        }
                        else
                        {
                            callback.OnRequestError(-1, "json convert failed: content="+response.content);
                        }
                    }
                    else
                    {
                        try
                        {
                            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(result.content);
                            if (errorResponse != null)
                            {
                                callback.OnRequestError(errorResponse.ErrorCode, errorResponse.ErrorMessage);
                            }
                            else
                            {
                                callback.OnRequestError(-1, "Failed to get archive data: content="+response.content);
                            }
                        }
                        catch (Exception e)
                        {
                            callback.OnRequestError(-1, "Failed to get archive data: content="+response.content);
                        }
                    }
                }
                catch (Exception e)
                {
                    callback.OnRequestError(-1, "Failed to get archive data: error=" + e.Message + ", content=" + response.content);
                }
            });
        }
        
        public void GetArchiveCover(string archiveUuid, string archiveFileId, ITapCloudSaveRequestCallback callback)
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(TAP_CLOUDSAVE_SERVICE)
                .Method("getArchiveCover")
                .Args("archiveUUIDForCover",  archiveUuid)
                .Args("archiveFileIDForCover",  archiveFileId)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (response) =>
            {
                if (callback == null) return;

                try
                {
                    if (response.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(response.content))
                    {
                        callback.OnRequestError(-1, "Failed to get archive cover: code="+response.code + "content="+response.content);
                        return;
                    }
                    
                    var result = JsonConvert.DeserializeObject<TapEngineBridgeResult>(response.content);
                    if (result != null && result.code == TapEngineBridgeResult.RESULT_SUCCESS)
                    {
                        var coverData = Convert.FromBase64String(result.content);
                        if (coverData != null)
                        {
                            callback.OnArchiveCoverResult(coverData);
                        }
                        else
                        {
                            callback.OnRequestError(-1, "json convert failed: content="+response.content);
                        }
                    }
                    else
                    {
                        try
                        {
                            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(result.content);
                            if (errorResponse != null)
                            {
                                callback.OnRequestError(errorResponse.ErrorCode, errorResponse.ErrorMessage);
                            }
                            else
                            {
                                callback.OnRequestError(-1, "Failed to get archive cover: content="+response.content);
                            }
                        }
                        catch (Exception e)
                        {
                            callback.OnRequestError(-1, "Failed to get archive cover: content="+response.content);
                        }
                    }
                }
                catch (Exception e)
                {
                    callback.OnRequestError(-1, "Failed to get archive cover: error=" + e.Message + ", content=" + response.content);
                }
            });
        }
    }
}