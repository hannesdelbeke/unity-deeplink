

using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Needle.Deeplink
{
    internal class DeepLinking
    {
        [InitializeOnLoadMethod]
        static void InitializePatch()
        {
            var type = typeof(UnityEditor.PackageManager.UI.Window).Assembly.GetType("UnityEditor.PackageManager.UI.PackageManagerWindow");
            var method = type?.GetMethod("OpenURL", (BindingFlags)(-1));
            if (method == null) return;

            try
            {
                var h = new Harmony(nameof(DeepLinking));
                h.Patch(method, new HarmonyMethod(typeof(DeepLinking), nameof(Prefix)));
            }
            catch (Exception)
            {
                // ignore
            }
        }


        private static bool Prefix(object __instance, string url)
        {
            // convert space
            url = url.Replace("%20", " ");


            // Check for regular AssetStore URLs and prevent those from being processed
            // e.g. com.unity3d.kharma/content/163802 - installs the package with ID 163802
            if (Regex.IsMatch(url, @"com.unity3d.kharma:content\/(\d+)"))
            {
                return true;
            }


            // custom commands. first menu name can't be empty, so // is used to indicate a custom command


            // custom command: select asset
            // Check for Unity select asset commands in the URL (com.unity3d.kharma:content//select/{asset_path})
            // e.g. com.unity3d.kharma:content//select/Assets - selects the asset folder
            if (Regex.IsMatch(url, @"com.unity3d.kharma:content\/\/select\/(.*)"))
            {
                // Extract the asset path string from the URL (after the select/)
                string assetPath = null;
                // only split of the first /
                var parts = url.Split(new[] { '/' }, 4);
                if (parts.Length > 3)
                {
                    assetPath = parts[3];
                }
                if (assetPath != null)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (asset != null)
                    {
                        Selection.activeObject = asset;
                    }
                    else
                    {
                        Debug.LogWarning("[Deep Link] Select asset link: asset not found at path " + assetPath);
                    }
                }
                return true;
            }


            // Check for Unity menu commands in the URL (com.unity3d.kharma:content/{menu_path})
            // e.g. com.unity3d.kharma:content/Help/About%20Unity - opens the About Unity window
            if (Regex.IsMatch(url, @"com.unity3d.kharma:content\/(.*)"))
            {
                // Extract the menu path string from the URL (after the content/)
                string menuPath = null;
                // only split of the first /
                var parts = url.Split(new[] { '/' }, 2);
                if (parts.Length > 1)
                {
                    menuPath = parts[1];
                }

                if (menuPath != null)
                {
                    EditorApplication.ExecuteMenuItem(menuPath);
                }

                return true;
            }


            // If the URL doesn't match any of the above conditions, return false to continue processing
            return false;
        }
    }
}

