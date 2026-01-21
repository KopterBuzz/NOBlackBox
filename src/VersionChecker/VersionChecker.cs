using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class VersionChecker
{
    public static async Task<string> GetLatestReleaseTagAsync()
    {
        const string url =
            "https://github.com/KopterBuzz/NOBlackBox/releases/latest";

        using (var request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("User-Agent", "Unity");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                throw new Exception(request.error);
            }

            // After redirect, this will be:
            // https://github.com/.../releases/tag/<TAG>
            var finalUrl = request.url;

            int index = finalUrl.LastIndexOf("/tag/", StringComparison.Ordinal);
            if (index == -1)
                throw new Exception("Tag not found in redirect URL.");

            return finalUrl.Substring(index + 5);
        }
    }
}
