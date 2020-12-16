
using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;
using Klak.Ndi;
using Klak.Ndi.Interop;

public class NDITrackDesktopWindow : MonoBehaviour
{
    private const int MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct MONITORINFOEX
    {
        public int Size;
        public RECT Monitor;
        public RECT WorkArea;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr FindWindow(string strClassName, string strWindowName);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hwnd, ref RECT rectangle);

    [DllImport("user32.dll")]
    static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    static Regex normalizeMonitorNamePattern = new Regex(@"[^a-zA-Z0-9 -]");
    static string GetNormalizedMonitorName(string monitorName)
    {
        return normalizeMonitorNamePattern.Replace(monitorName, "");
    }
    static Rect GetNormalizedRectRelativeToMonitor(RECT globalPos, MONITORINFOEX targetMonitor, NDITrackDesktopWindow ndiTracker)
    {
        Rect result = new Rect();

        int width = Math.Abs(targetMonitor.Monitor.Right - targetMonitor.Monitor.Left);
        int height = Math.Abs(targetMonitor.Monitor.Bottom - targetMonitor.Monitor.Top);
        
        result.y = ((float)(globalPos.Top - targetMonitor.Monitor.Top) / height) + ((float)ndiTracker.topCropPixels / height);
        result.yMax = ((float)(globalPos.Bottom - targetMonitor.Monitor.Top) / height) - ((float)ndiTracker.bottomCropPixels / height);
        result.x = ((float)(globalPos.Left - targetMonitor.Monitor.Left) / width) + ((float)ndiTracker.leftCropPixels / width);
        result.xMax = ((float)(globalPos.Right - targetMonitor.Monitor.Left) / width) - ((float)ndiTracker.rightCropPixels / width);

        result.y = (1f - result.y) - result.height; // not sure why i have to mod this value at the moment

        return result;
    }

    [Serializable]
    public class NDIMonitorId
    {
        public string ndiMonitorName;
        public string monitorName;
    }

    public string targetWName = "";
    public string requestNDIName = "";
    public NDIMonitorId[] ndiMonitorMapping;
    public Rect normalizedWindowRectangle;
    public int topCropPixels = 0;
    public int bottomCropPixels = 0;
    public int leftCropPixels = 0;
    public int rightCropPixels = 0;

    private string currentNDIName;
    private NdiReceiver ndiReceiver;

    void Start()
    {
        Reinitialize();
    }

    void Update()
    {
        KeepMonitor();
        UpdateMaterial();
    }

    void KeepMonitor()
    {
        if (!ndiReceiver)
        {
            Reinitialize();
            return;
        }

        string determineNDIName = null;
        RECT output = new RECT();
        bool trackWindow = targetWName != null && targetWName.Length > 0;
        IntPtr hwnd = new IntPtr();
        IntPtr monitorFromWindow = new IntPtr();
        MONITORINFOEX monitorInfo = new MONITORINFOEX();
        monitorInfo.Size = Marshal.SizeOf(typeof(MONITORINFOEX));


        if (trackWindow)
        {
            hwnd = FindWindow(null, targetWName);
            if(hwnd == IntPtr.Zero) return;
            GetWindowRect(hwnd, ref output);

            monitorFromWindow = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            GetMonitorInfo(monitorFromWindow, ref monitorInfo);

            string targetMonitorName = GetNormalizedMonitorName(monitorInfo.DeviceName);

            if(ndiMonitorMapping == null || ndiMonitorMapping.Length == 0)
            {
                // small warning and utility to help get values populated in monitor mapping object
                Debug.LogWarning("NDI Track Desktop Window Component's ndiMonitorMapping value is not setup. Drag the window: " + targetWName + " around all your monitors to get the monitor names");
                Debug.LogWarning("Current Monitor Name: " + targetMonitorName);
                return;
            }
            else
            {
                foreach (var target in ndiMonitorMapping)
                {
                    if (target.monitorName == targetMonitorName)
                    {
                        determineNDIName = target.ndiMonitorName;
                    }
                }
            }

            
        }
        else if(requestNDIName != null && requestNDIName.Length > 0)
        {
            determineNDIName = requestNDIName;
        }

        if(determineNDIName != null && determineNDIName.Length > 0 && determineNDIName != currentNDIName)
        {
            currentNDIName = determineNDIName;
            Reinitialize();
            return;
        }

        if(currentNDIName != null && currentNDIName.Length > 0 && ndiReceiver && ndiReceiver.texture)
        {
            if (trackWindow)
            {
                normalizedWindowRectangle = GetNormalizedRectRelativeToMonitor(output, monitorInfo, this);
            }
            else
            {
                normalizedWindowRectangle = new Rect(0, 0, 1, 1);
            }
        }
    }

    void Reinitialize()
    {
        if (!ndiReceiver)
        {
            ndiReceiver = GetComponent<NdiReceiver>();
        }

        if (currentNDIName != null && currentNDIName.Length > 0)
        {
            ndiReceiver.ndiName = currentNDIName;
        }
    }

    void UpdateMaterial()
    {
        if (ndiReceiver && ndiReceiver.targetRenderer && currentNDIName != null && currentNDIName.Length > 0)
        {
            ndiReceiver.targetRenderer.material.SetTextureOffset(ndiReceiver.targetMaterialProperty, new Vector2(normalizedWindowRectangle.x, normalizedWindowRectangle.y));
            ndiReceiver.targetRenderer.material.SetTextureScale(ndiReceiver.targetMaterialProperty, new Vector2(normalizedWindowRectangle.width, normalizedWindowRectangle.height));
        }
    }

}
