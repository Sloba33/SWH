using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using com.unity3d.mediation;
using UnityEngine.UIElements;
public class AdsManager : MonoBehaviour
{
#if UNITY_ANDROID
    string appKey = "208ef02bd";
    string bannerAdUnitId = "thnfvcsog13bhn08";
    string interstitialAdUnitId = "aeyqi3vqlv6o8sh9";
#elif UNITY_IPHONE
    string appKey = "208ef02bd";
    string bannerAdUnitId = "iep3rxsyp9na3rw8";
    string interstitialAdUnitId = "wmgt0712uuux8ju4";

#else
    string appKey = "unexpected_platform";
    string bannerAdUnitId = "unexpected_platform";
    string interstitialAdUnitId = "unexpected_platform";
#endif
    string currentMessage;

    public TextMeshProUGUI adsWatchedText;
    public void Start()
    {

        Debug.Log("unity-script: IronSource.Agent.validateIntegration");
        IronSource.Agent.validateIntegration();
        Debug.Log("unity-script: unity version" + IronSource.unityVersion());


        LevelPlay.Init(appKey, adFormats: new[] { LevelPlayAdFormat.REWARDED });

        LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitializationFailedEvent;
    }
    private void EnableAds()
    {

        IronSourceEvents.onImpressionDataReadyEvent += ImpressionDataReadyEvent;
        //rewarded ads
        IronSourceRewardedVideoEvents.onAdOpenedEvent += RewardedVideoOnAdOpenedEvent;
        IronSourceRewardedVideoEvents.onAdClosedEvent += RewardedVideoOnAdClosedEvent;
        IronSourceRewardedVideoEvents.onAdAvailableEvent += RewardedVideoOnAdAvailable;
        IronSourceRewardedVideoEvents.onAdUnavailableEvent += RewardedVideoOnAdUnavailable;
        IronSourceRewardedVideoEvents.onAdShowFailedEvent += RewardedVideoOnAdShowFailedEvent;

        IronSourceRewardedVideoEvents.onAdRewardedEvent += RewardedVideoOnAdRewardedEvent;
        IronSourceRewardedVideoEvents.onAdClickedEvent += RewardedVideoOnAdClickedEvent;
    }
    void ImpressionDataReadyEvent(IronSourceImpressionData impressionData)
    {
        Debug.Log("unity - script: I got ImpressionDataReadyEvent ToString(): " + impressionData.ToString());
        Debug.Log("unity - script: I got ImpressionDataReadyEvent allData: " + impressionData.allData);
    }
    private void SDKInitialized()
    {
        Debug.Log("Sdk inititalization is successful!");
    }
    private void OnApplicationPause(bool pause)
    {
        IronSource.Agent.onApplicationPause(pause);
    }
    #region Rewarded_Ads
    public void LoadRewardedAds()
    {
        IronSource.Agent.loadRewardedVideo();
    }
    public void ShowRewarded()
    {
        if (IronSource.Agent.isRewardedVideoAvailable())
        {
            IronSource.Agent.showRewardedVideo();
           
        }
        else 
        {
            Debug.Log("unity-script: IronSource.Agent.isRewardedVideoAvailable - False");

        }
    }
    string currentCharSaveString;
    public void ShowRewarded(string _save_string)
    {
        currentCharSaveString = _save_string;
        if (IronSource.Agent.isRewardedVideoAvailable())
        {
            IronSource.Agent.showRewardedVideo();
            currentMessage = AddErrorFeedback(true);
             PlayerPrefs.SetInt(_save_string, PlayerPrefs.GetInt(_save_string, 0) + 1);

        }
        else
        {
           
            currentMessage = AddErrorFeedback(false);
            Debug.Log(currentMessage);
            // adsWatchedText.text = currentMessage;

        }
    }

    /************* RewardedVideo AdInfo Delegates *************/
    // Indicates that there’s an available ad.
    // The adInfo object includes information about the ad that was loaded successfully
    // This replaces  the RewardedVideoAvailabilityChangedEvent(true) event
    void RewardedVideoOnAdAvailable(IronSourceAdInfo adInfo)
    {
    }
    // Indicates that no ads are available to be displayed
    // This replaces the RewardedVideoAvailabilityChangedEvent(false) event
    void RewardedVideoOnAdUnavailable()
    {
    }
    // The Rewarded Video ad view has opened. Your activity will loose focus.
    void RewardedVideoOnAdOpenedEvent(IronSourceAdInfo adInfo)
    {
    }
    // The Rewarded Video ad view is about to be closed. Your activity will regain its focus.
    void RewardedVideoOnAdClosedEvent(IronSourceAdInfo adInfo)
    {
    }
    // The user completed to watch the video, and should be rewarded.
    // The placement parameter will include the reward data.
    // When using server-to-server callbacks, you may ignore this event and wait for the ironSource server callback.
    void RewardedVideoOnAdRewardedEvent(IronSourcePlacement placement, IronSourceAdInfo adInfo)
    {

        Debug.Log("unity-script: I got RewardedVideoOnAdRewardedEvent With Placement" + placement + "And AdInfo " + adInfo);
        PlayerPrefs.SetInt(currentCharSaveString, PlayerPrefs.GetInt(currentCharSaveString, 0) + 1);
        Debug.Log("total ads watched" + PlayerPrefs.GetInt("AdsWatched"));
        adsWatchedText.text = PlayerPrefs.GetInt("AdsWatched").ToString();
    }

    // The rewarded video ad was failed to show.
    void RewardedVideoOnAdShowFailedEvent(IronSourceError error, IronSourceAdInfo adInfo)
    {
    }
    // Invoked when the video ad was clicked.
    // This callback is not supported by all networks, and we recommend using it only if
    // it’s supported by all networks you included in your build.
    void RewardedVideoOnAdClickedEvent(IronSourcePlacement placement, IronSourceAdInfo adInfo)
    {
    }
    void SdkInitializationCompletedEvent(LevelPlayConfiguration config)
    {
        Debug.Log("unity-script: I got SdkInitializationCompletedEvent with config: " + config);
        EnableAds();
    }

    void SdkInitializationFailedEvent(LevelPlayInitError error)
    {
        Debug.Log("unity-script: I got SdkInitializationFailedEvent with error: " + error);
    }
    #endregion
    private string AddErrorFeedback(bool hasAd)
    {
        if (!hasAd)

            return "Ad not ready";
        else return "";
    }

}


