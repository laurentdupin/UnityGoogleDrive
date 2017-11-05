﻿using System;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Issues a new access token using provided refresh token.
/// Protocol: https://developers.google.com/identity/protocols/OAuth2WebServer#offline.
/// </summary>
public class AccessTokenRefresher
{
    #pragma warning disable 0649
    [Serializable] struct RefreshResponse { public string error, error_description, access_token, expires_in, token_type; }
    #pragma warning restore 0649

    public event Action<AccessTokenRefresher> OnDone;

    public bool IsDone { get; private set; }
    public bool IsError { get; private set; }
    public string AccesToken { get; private set; }

    private UnityWebRequest refreshRequest;

    public void RefreshAccessToken (GoogleDriveSettings googleDriveSettings, string refreshToken)
    {
        var refreshRequestURI = googleDriveSettings.AuthCredentials.TokenUri;

        var refreshRequestForm = new WWWForm();
        refreshRequestForm.AddField("client_id", googleDriveSettings.AuthCredentials.ClientId);
        refreshRequestForm.AddField("client_secret", googleDriveSettings.AuthCredentials.ClientSecret);
        refreshRequestForm.AddField("refresh_token", refreshToken);
        refreshRequestForm.AddField("grant_type", "refresh_token");

        refreshRequest = UnityWebRequest.Post(refreshRequestURI, refreshRequestForm);
        refreshRequest.SetRequestHeader("Content-Type", GoogleDriveSettings.REQUEST_CONTENT_TYPE);
        refreshRequest.SetRequestHeader("Accept", "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        refreshRequest.RunWebRequest().completed += HandleRequestComplete;
    }

    private void HandleRefreshComplete (bool error = false)
    {
        IsError = error;
        IsDone = true;
        if (OnDone != null)
            OnDone.Invoke(this);
    }

    private void HandleRequestComplete (AsyncOperation requestYeild)
    {
        if (refreshRequest == null || !string.IsNullOrEmpty(refreshRequest.error))
        {
            HandleRefreshComplete(true);
            return;
        }

        var response = JsonUtility.FromJson<RefreshResponse>(refreshRequest.downloadHandler.text);
        if (!string.IsNullOrEmpty(response.error))
        {
            Debug.LogError(string.Format("{0}: {1}", response.error, response.error_description));
            HandleRefreshComplete(true);
            return;
        }

        AccesToken = response.access_token;
        HandleRefreshComplete();
    }
}
