using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Extensions;
using Firebase;
using Firebase.Crashlytics;

public class FirebaseInit : MonoBehaviour
{
    private static FirebaseInit _instance;
    private FirebaseApp app;

    public static FirebaseInit Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("FirebaseInit is null");
            }
            return _instance;
        }
    }
    void Awake()
    {
        _instance = this;
        Debug.Log("Loading firebase app");
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                // Create and hold a reference to your FirebaseApp,
                // where app is a Firebase.FirebaseApp property of your application class.
                app = Firebase.FirebaseApp.DefaultInstance;
                Debug.Log(app.Options.DatabaseUrl);
                // Set a flag here to indicate whether Firebase is ready to use by your app.
                // Crashlytics.ReportUncaughtExceptionsAsFatal = true;
            }
            else
            {
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }
        });
    }


}
