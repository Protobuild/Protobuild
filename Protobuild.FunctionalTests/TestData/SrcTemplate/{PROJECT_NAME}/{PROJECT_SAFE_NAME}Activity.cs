#if PLATFORM_ANDROID || PLATFORM_OUYA

namespace {PROJECT_SAFE_NAME}
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using Android.Content.PM;
    
    using Microsoft.Xna.Framework;
    
    using Ninject;
    
    using Protogame;
  
    [Activity(
        Label = "{PROJECT_NAME}",
        MainLauncher = true,
        AlwaysRetainTaskState = true,
        LaunchMode = Android.Content.PM.LaunchMode.SingleInstance,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden,
        ScreenOrientation = ScreenOrientation.Landscape)]
    public class {PROJECT_SAFE_NAME}Activity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            var kernel = new StandardKernel();
            kernel.Load<Protogame2DIoCModule>();
            kernel.Load<ProtogameAssetIoCModule>();
            AssetManagerClient.AcceptArgumentsAndSetup<GameAssetManagerProvider>(kernel, null);

            // Create our OpenGL view, and display it
            var g = new {PROJECT_SAFE_NAME}Game(kernel);
            SetContentView(g.AndroidGameView);
            g.Run();
        }
    }
}

#endif
