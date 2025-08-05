using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TapSDK.CloudSave.Internal;
using TapSDK.Core;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Internal.Utils;
using TapSDK.Core.Standalone;
using TapSDK.Core.Standalone.Internal;
using TapSDK.Core.Standalone.Internal.Http;
using TapSDK.Login;
using UnityEngine;

namespace TapSDK.CloudSave.Standalone
{
    public class TapCloudSaveStandalone : ITapCloudSaveBridge
    {
        private List<ITapCloudSaveCallback> currentSaveCallback = null;
        private static readonly bool isRND = false;
        private bool _hasInitNative = false;
        private object _lockObj = new object();

        private TapLog cloudSaveLog;

        public void Init(TapTapSdkOptions options)
        {
            Log("TapCloudSave start init");
            TapCloudSaveTracker.Instance.TrackInit();
            string cacheDir = Path.Combine(
                Application.persistentDataPath,
                "cloudsave_" + options.clientId
            );
            string deviceID = SystemInfo.deviceUniqueIdentifier;
            Task.Run(async () =>
            {
                TapTapAccount tapAccount = await TapTapLogin.Instance.GetCurrentTapAccount();
                string loginKid = "";
                string loginKey = "";
                if (tapAccount != null && !string.IsNullOrEmpty(tapAccount.openId))
                {
                    loginKey = tapAccount.accessToken.macKey;
                    loginKid = tapAccount.accessToken.kid;
                }
                Dictionary<string, object> loginData = new Dictionary<string, object>
                {
                    { "kid", loginKid },
                    { "key", loginKey },
                };
                int region = isRND ? 2 : 0;
                try
                {
                    Dictionary<string, object> initConfig = new Dictionary<string, object>()
                    {
                        { "region", region },
                        { "log_to_console", 1 },
                        { "log_level", 3 },
                        { "data_dir", cacheDir },
                        { "client_id", options.clientId },
                        { "client_token", options.clientToken },
                        { "ua", TapHttpUtils.GenerateUserAgent() },
                        { "lang", Tracker.getServerLanguage() },
                        { "platform", "PC" },
                        { "device_id", deviceID },
                        { "sdk_artifact", "Unity" },
                        { "sdk_module_ver", TapTapCloudSave.Version },
                        { "sdk_token", loginData },
                    };
                    Log(" start invoke TapSdkCloudSaveInitialize result ");
                    string config = JsonConvert.SerializeObject(initConfig);
                    int initResult = TapCloudSaveWrapper.TapSdkCloudSaveInitialize(config);
                    Log("TapSdkCloudSaveInitialize result = " + initResult);
                    if (initResult < 0)
                    {
                        RunOnMainThread(() =>
                        {
                            if (currentSaveCallback != null && currentSaveCallback.Count > 0)
                            {
                                foreach (var callback in currentSaveCallback)
                                {
                                    callback?.OnResult(TapCloudSaveResultCode.INIT_FAIL);
                                }
                            }
                        });
                    }
                    else
                    {
                        lock (_lockObj)
                        {
                            _hasInitNative = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    Log("TapSdkCloudSaveInitialize error " + e.Message);
                }
            });

            EventManager.AddListener(EventManager.OnTapUserChanged, OnLoginInfoChanged);
        }

        public void CreateArchive(
            ArchiveMetadata metadata,
            string archiveFilePath,
            string archiveCoverPath,
            ITapCloudSaveRequestCallback callback
        )
        {
            CheckPCLaunchState();
            string seesionId = Guid.NewGuid().ToString();
            const string method = "createArchive";
            Task.Run(async () =>
            {
                bool hasInit = await CheckInitAndLoginState(method, seesionId);
                if (!hasInit)
                {
                    return;
                }
                try
                {
                    byte[] fileBytes = File.ReadAllBytes(archiveFilePath);
                    byte[] coverData = null;
                    if (!string.IsNullOrEmpty(archiveCoverPath))
                    {
                        coverData = File.ReadAllBytes(archiveCoverPath);
                    }
                    string metaValue = JsonConvert.SerializeObject(metadata);
                    string result = TapCloudSaveWrapper.CreateArchive(
                        metaValue,
                        fileBytes,
                        fileBytes.Length,
                        coverData,
                        coverData?.Length ?? 0
                    );
                    TapCloudSaveBaseResponse response =
                        JsonConvert.DeserializeObject<TapCloudSaveBaseResponse>(result);
                    if (response.success)
                    {
                        ArchiveData data = response.data.ToObject<ArchiveData>();
                        TapCloudSaveTracker.Instance.TrackSuccess(method, seesionId);
                        RunOnMainThread(() =>
                        {
                            callback?.OnArchiveCreated(
                                new ArchiveData()
                                {
                                    FileId = data.FileId,
                                    Uuid = data.Uuid,
                                    Name = metadata.Name,
                                    Summary = metadata.Summary,
                                    Extra = metadata.Extra,
                                    Playtime = metadata.Playtime,
                            });
                        });
                    }
                    else
                    {
                        try
                        {
                            TapCloudSaveError error = response.data.ToObject<TapCloudSaveError>();
                            Log(
                                "createArchive failed error = " + JsonConvert.SerializeObject(error)
                            );
                            TapCloudSaveTracker.Instance.TrackFailure(
                                method,
                                seesionId,
                                error.code,
                                error.msg ?? ""
                            );
                            RunOnMainThread(() =>
                            {
                                callback?.OnRequestError(error.code, $"创建存档失败: {error.msg}");
                            });
                        }
                        catch (Exception e)
                        {
                            TapCloudSaveTracker.Instance.TrackFailure(
                                method,
                                seesionId,
                                -1,
                                "创建存档失败: 数据解析异常"
                            );
                            RunOnMainThread(() =>
                            {
                                callback?.OnRequestError(-1, "创建存档失败: 数据解析异常");
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    string msg = $"创建存档失败: {e.Message}";
                    TapCloudSaveTracker.Instance.TrackFailure(method, seesionId, -1, msg);
                    RunOnMainThread(() =>
                    {
                        callback?.OnRequestError(-1, msg);
                    });
                }
            });
        }

        public void DeleteArchive(string archiveUuid, ITapCloudSaveRequestCallback callback)
        {
            CheckPCLaunchState();
            string seesionId = Guid.NewGuid().ToString();
            const string method = "deleteArchive";
            Task.Run(async () =>
            {
                bool hasInit = await CheckInitAndLoginState(method, seesionId);
                if (!hasInit)
                {
                    return;
                }
                try
                {
                    string result = TapCloudSaveWrapper.DeleteArchive(archiveUuid);
                    TapCloudSaveBaseResponse response =
                        JsonConvert.DeserializeObject<TapCloudSaveBaseResponse>(result);
                    if (response.success)
                    {
                        ArchiveData archiveData = response.data.ToObject<ArchiveData>();
                        TapCloudSaveTracker.Instance.TrackSuccess(method, seesionId);
                        RunOnMainThread(() =>
                        {
                            callback?.OnArchiveDeleted(archiveData);
                        });
                    }
                    else
                    {
                        try
                        {
                            TapCloudSaveError error = response.data.ToObject<TapCloudSaveError>();
                            Log(
                                "deleteArchive failed error = " + JsonConvert.SerializeObject(error)
                            );
                            TapCloudSaveTracker.Instance.TrackFailure(
                                method,
                                seesionId,
                                error.code,
                                error.msg ?? ""
                            );
                            RunOnMainThread(() =>
                            {
                                callback?.OnRequestError(error.code, $"删除存档失败: {error.msg}");
                            });
                        }
                        catch (Exception e)
                        {
                            TapCloudSaveTracker.Instance.TrackFailure(
                                method,
                                seesionId,
                                -1,
                                "删除存档失败: 数据解析异常"
                            );
                            RunOnMainThread(() =>
                            {
                                callback?.OnRequestError(-1, "删除存档失败: 数据解析异常");
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    string msg = $"删除失败: {e.Message}";
                    TapCloudSaveTracker.Instance.TrackFailure(method, seesionId, -1, msg);
                    RunOnMainThread(() =>
                    {
                        callback?.OnRequestError(-1, msg);
                    });
                }
            });
        }

        public void GetArchiveCover(
            string archiveUuid,
            string archiveFileId,
            ITapCloudSaveRequestCallback callback
        )
        {
            CheckPCLaunchState();
            string seesionId = Guid.NewGuid().ToString();
            const string method = "getArchiveCover";
            Task.Run(async () =>
            {
                bool hasInit = await CheckInitAndLoginState(method, seesionId);
                if (!hasInit)
                {
                    return;
                }
                try
                {
                    byte[] result = TapCloudSaveWrapper.GetArchiveCover(
                        archiveUuid,
                        archiveFileId,
                        out int coverSize
                    );
                    if (coverSize >= 0)
                    {
                        RunOnMainThread(() =>
                        {
                            callback.OnArchiveCoverResult(result);
                        });
                        TapCloudSaveTracker.Instance.TrackSuccess(method, seesionId);
                    }
                    else
                    {
                        try
                        {
                            TapCloudSaveBaseResponse response =
                                JsonConvert.DeserializeObject<TapCloudSaveBaseResponse>(
                                    Encoding.UTF8.GetString(result)
                                );
                            TapCloudSaveError error = response.data.ToObject<TapCloudSaveError>();
                            Log(
                                "getArchiveCover failed error = " + JsonConvert.SerializeObject(error)
                            );
                            TapCloudSaveTracker.Instance.TrackFailure(
                                method,
                                seesionId,
                                error.code,
                                error.msg ?? ""
                            );
                            RunOnMainThread(() =>
                            {
                                callback?.OnRequestError(error.code, $"获取封面失败: {error.msg}");
                            });
                        }
                        catch (Exception e)
                        {
                            TapCloudSaveTracker.Instance.TrackFailure(
                                method,
                                seesionId,
                                -1,
                                "获取封面失败: 数据解析异常"
                            );
                            RunOnMainThread(() =>
                            {
                                callback?.OnRequestError(-1, "获取封面失败: 数据解析异常");
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    string msg = $"获取封面失败: {e.Message}";
                    TapCloudSaveTracker.Instance.TrackFailure(method, seesionId, -1, msg);
                    RunOnMainThread(() =>
                    {
                        callback?.OnRequestError(-1, msg);
                    });
                }
            });
        }

        public void GetArchiveData(
            string archiveUuid,
            string archiveFileId,
            ITapCloudSaveRequestCallback callback
        )
        {
            CheckPCLaunchState();
            string seesionId = Guid.NewGuid().ToString();
            const string method = "getArchiveData";
            Task.Run(async () =>
            {
                bool hasInit = await CheckInitAndLoginState(method, seesionId);
                if (!hasInit)
                {
                    return;
                }
                try
                {
                    byte[] result = TapCloudSaveWrapper.GetArchiveData(
                        archiveUuid,
                        archiveFileId,
                        out int fileSize
                    );
                    if (fileSize >= 0)
                    {
                        RunOnMainThread(() =>
                        {
                            callback.OnArchiveDataResult(result);
                        });
                        TapCloudSaveTracker.Instance.TrackSuccess(method, seesionId);
                    }
                    else
                    {
                        try
                        {
                            TapCloudSaveBaseResponse response =
                                JsonConvert.DeserializeObject<TapCloudSaveBaseResponse>(
                                    Encoding.UTF8.GetString(result)
                                );
                            TapCloudSaveError error = response.data.ToObject<TapCloudSaveError>();
                            Log(
                                "getArchiveData failed error = " + JsonConvert.SerializeObject(error)
                            );
                            TapCloudSaveTracker.Instance.TrackFailure(
                                method,
                                seesionId,
                                error.code,
                                error.msg ?? ""
                            );
                            RunOnMainThread(() =>
                            {
                                callback?.OnRequestError(error.code, $"获取存档失败: {error.msg}");
                            });
                        }
                        catch (Exception e)
                        {
                            TapCloudSaveTracker.Instance.TrackFailure(
                                method,
                                seesionId,
                                -1,
                                "获取存档失败: 数据解析异常"
                            );
                            RunOnMainThread(() =>
                            {
                                callback?.OnRequestError(-1, "获取存档失败: 数据解析异常");
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    string msg = $"获取存档失败: {e.Message}";
                    TapCloudSaveTracker.Instance.TrackFailure(method, seesionId, -1, msg);
                    RunOnMainThread(() =>
                    {
                        callback?.OnRequestError(-1, msg);
                    });
                }
            });
        }

        public void GetArchiveList(ITapCloudSaveRequestCallback callback)
        {
            CheckPCLaunchState();
            string seesionId = Guid.NewGuid().ToString();
            const string method = "getArchiveList";
            Task.Run(async () =>
            {
                bool hasInit = await CheckInitAndLoginState(method, seesionId);
                if (!hasInit)
                {
                    return;
                }
                try
                {
                    string result = TapCloudSaveWrapper.GetArchiveList();
                    TapCloudSaveBaseResponse response =
                        JsonConvert.DeserializeObject<TapCloudSaveBaseResponse>(result);
                    if (response.success)
                    {
                        TapCloudSaveArchiveListResponse archiveDatas =
                            response.data.ToObject<TapCloudSaveArchiveListResponse>();
                        TapCloudSaveTracker.Instance.TrackSuccess(method, seesionId);
                        RunOnMainThread(() =>
                        {
                            callback?.OnArchiveListResult(archiveDatas.saves);
                        });
                    }
                    else
                    {
                        try
                        {
                            TapCloudSaveError error = response.data.ToObject<TapCloudSaveError>();
                            Log(
                                "getArchiveList failed error = " + JsonConvert.SerializeObject(error)
                            );
                            TapCloudSaveTracker.Instance.TrackFailure(
                                method,
                                seesionId,
                                error.code,
                                error.msg ?? ""
                            );
                            RunOnMainThread(() =>
                            {
                                callback?.OnRequestError(
                                    error.code,
                                    $"获取存档列表失败: {error.msg}"
                                );
                            });
                        }
                        catch (Exception e)
                        {
                            TapCloudSaveTracker.Instance.TrackFailure(
                                method,
                                seesionId,
                                -1,
                                "获取存档列表失败: 数据解析异常"
                            );
                            RunOnMainThread(() =>
                            {
                                callback?.OnRequestError(-1, "获取存档列表失败: 数据解析异常");
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    string msg = $"获取存档列表失败: {e.Message}";
                    TapCloudSaveTracker.Instance.TrackFailure(method, seesionId, -1, msg);
                    RunOnMainThread(() =>
                    {
                        callback?.OnRequestError(-1, msg);
                    });
                }
            });
        }

        public void RegisterCloudSaveCallback(ITapCloudSaveCallback callback)
        {
            currentSaveCallback?.Clear();
            string seesionId = Guid.NewGuid().ToString();
            const string method = "registerCloudSaveCallback";
            TapCloudSaveTracker.Instance.TrackStart(method, seesionId);
            if (currentSaveCallback == null)
            {
                currentSaveCallback = new List<ITapCloudSaveCallback>();
            }
            if (!currentSaveCallback.Contains(callback))
            {
                TapCloudSaveTracker.Instance.TrackSuccess(method, seesionId);
                currentSaveCallback.Add(callback);
            }
            else
            {
                TapCloudSaveTracker.Instance.TrackFailure(
                    method,
                    seesionId,
                    errorMessage: "callback has already registered"
                );
            }
        }

        public void UpdateArchive(
            string archiveUuid,
            ArchiveMetadata metadata,
            string archiveFilePath,
            string archiveCoverPath,
            ITapCloudSaveRequestCallback callback
        )
        {
            CheckPCLaunchState();
            string seesionId = Guid.NewGuid().ToString();
            const string method = "updateArchive";
            Task.Run(async () =>
            {
                bool hasInit = await CheckInitAndLoginState(method, seesionId);
                if (!hasInit)
                {
                    return;
                }
                try
                {
                    byte[] fileBytes = File.ReadAllBytes(archiveFilePath);
                    byte[] coverData = null;
                    if (!string.IsNullOrEmpty(archiveCoverPath))
                    {
                        coverData = File.ReadAllBytes(archiveCoverPath);
                    }
                    string metaValue = JsonConvert.SerializeObject(metadata);
                    string result = TapCloudSaveWrapper.UpdateArchive(
                        archiveUuid,
                        metaValue,
                        fileBytes,
                        fileBytes.Length,
                        coverData,
                        coverData?.Length ?? 0
                    );
                    TapCloudSaveBaseResponse response =
                        JsonConvert.DeserializeObject<TapCloudSaveBaseResponse>(result);
                    if (response.success)
                    {
                        ArchiveData data = response.data.ToObject<ArchiveData>();
                        TapCloudSaveTracker.Instance.TrackSuccess(method, seesionId);
                        RunOnMainThread(() =>
                        {
                            callback?.OnArchiveUpdated(data);
                        });
                    }
                    else
                    {
                        try
                        {
                            TapCloudSaveError error = response.data.ToObject<TapCloudSaveError>();
                            Log(
                                "updateArchive failed error = " + JsonConvert.SerializeObject(error)
                            );
                            TapCloudSaveTracker.Instance.TrackFailure(
                                method,
                                seesionId,
                                error.code,
                                error.msg ?? ""
                            );
                            RunOnMainThread(() =>
                            {
                                callback?.OnRequestError(error.code, $"更新存档失败: {error.msg}");
                            });
                        }
                        catch (Exception e)
                        {
                            TapCloudSaveTracker.Instance.TrackFailure(
                                method,
                                seesionId,
                                -1,
                                "更新存档失败: 数据解析异常"
                            );
                            RunOnMainThread(() =>
                            {
                                callback?.OnRequestError(-1, "更新存档失败: 数据解析异常");
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    string msg = $"更新存档失败: {e.Message}";
                    TapCloudSaveTracker.Instance.TrackFailure(method, seesionId, -1, msg);
                    RunOnMainThread(() =>
                    {
                        callback?.OnRequestError(-1, msg);
                    });
                }
            });
        }

        private async Task<bool> CheckInitAndLoginState(string method, string sessionId)
        {
            if (TapCoreStandalone.CheckInitState())
            {
                lock (_lockObj)
                {
                    if (!_hasInitNative)
                    {
                        Log("not init success, so return", true);
                        return false;
                    }
                }
                TapCloudSaveTracker.Instance.TrackStart(method, sessionId);
                TapTapAccount tapAccount = await TapTapLogin.Instance.GetCurrentTapAccount();
                if (tapAccount != null && !string.IsNullOrEmpty(tapAccount.openId))
                {
                    return true;
                }
                else
                {
                    if (currentSaveCallback != null && currentSaveCallback.Count > 0)
                    {
                        foreach (var callback in currentSaveCallback)
                        {
                            callback?.OnResult(TapCloudSaveResultCode.NEED_LOGIN);
                        }
                    }
                    TapCloudSaveTracker.Instance.TrackFailure(method, sessionId, -1, "not login");
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查是否通过 PC 启动校验
        /// </summary>
        private void CheckPCLaunchState()
        {
#if UNITY_STANDALONE_WIN
            if (!TapClientStandalone.isPassedInLaunchedFromTapTapPCCheck())
            {
                throw new Exception("TapCloudSave method must be invoked after isLaunchedFromTapTapPC succeed");
            }
#endif
        }

        internal void OnLoginInfoChanged(object data)
        {
            lock (_lockObj)
            {
                if (_hasInitNative)
                {
                    Task.Run(async () =>
                    {
                        TapTapAccount tapAccount =
                            await TapTapLogin.Instance.GetCurrentTapAccount();
                        string loginKid = "";
                        string loginKey = "";
                        if (tapAccount != null && !string.IsNullOrEmpty(tapAccount.openId))
                        {
                            loginKey = tapAccount.accessToken.macKey;
                            loginKid = tapAccount.accessToken.kid;
                        }
                        Dictionary<string, object> loginData = new Dictionary<string, object>
                        {
                            { "kid", loginKid },
                            { "key", loginKey },
                        };
                        int result = TapCloudSaveWrapper.TapSdkCloudSaveUpdateAccessToken(
                            JsonConvert.SerializeObject(loginData)
                        );
                        Log("update login msg result = " + result);
                    });
                }
            }
        }

        private void RunOnMainThread(Action action)
        {
            TapLoom.QueueOnMainThread(action);
        }

        private void Log(string msg, bool isError = false)
        {
            if (cloudSaveLog == null)
            {
                cloudSaveLog = new TapLog("TapCloudSave");
            }
            if (!string.IsNullOrEmpty(msg))
            {
                if (isError)
                {
                    cloudSaveLog.Error(msg);
                }
                else
                {
                    cloudSaveLog.Log(msg);
                }
            }
        }
    }
}
