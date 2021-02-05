﻿
using DataGrabber.LogWriter;
using DataGrabber.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataGrabber.Utility
{
    public sealed class ProxyHelper
    {
        private ProxyHelper() { }

        public static ProxyHelper Instance { get; } = new ProxyHelper();


        private static Timer ProxyTimer { get; set; } = null;

        private readonly int RotateIpAddressAfter_Seconds = ConfigFields.RotateIpAddressAfter_Seconds;


        public void RotateIPAddress()
        {

            try
            {
                var startTimeSpan = TimeSpan.Zero;
                var periodTimeSpan = TimeSpan.FromSeconds(RotateIpAddressAfter_Seconds);

                ProxyTimer = new Timer((e) =>
                {
                    string ipAddress = GetIpAddress();
                    if (!string.IsNullOrEmpty(ipAddress))
                        InternetSettings.SetProxy(ipAddress);
                }, null, startTimeSpan, periodTimeSpan);

            }
            catch (Exception ex)
            {
                Logger.Write("Exception in ProxyHelper -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }
        }


        public void StopTimer()
        {
            if (ProxyTimer != null)
            {
                ProxyTimer.Change(Timeout.Infinite, Timeout.Infinite);

                InternetSettings.UnsetProxy();
            }
        }

        private string GetIpAddress()
        {
            string ipAddress = string.Empty;

            try
            {

                int counter = 1, maxLoop = 4;
                bool isValidIP = false;

                // check 10 times before ip give up
                while (counter < maxLoop && !isValidIP)
                {
                    Logger.Write($"INFO: Getting IpAddress in ProxyHelper -> DataGrabber. counter: {counter};");
                    ApiResponse response = ScrapeRestService.Instance.GetIpAddress();
                    Logger.Write($"INFO: {response.Message} success in ScrapeRestService -> DataGrabber.");

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var resp = JsonConvert.DeserializeObject<IpResponse>(response.Message);

                        // check if ip is working or not
                        isValidIP = ScrapeRestService.Instance.CheckIfIpIsWorking(resp.IpAddress, resp.Port);

                        if (isValidIP)
                        {
                            Logger.Write($"SUCCESS: ip {resp.IpAddress} port {resp.Port} success in ScrapeRestService -> DataGrabber.");
                        }
                        else
                        {
                            Logger.Write($"FAIL: ip {resp.IpAddress} port {resp.Port} failed in ScrapeRestService -> DataGrabber.");
                        }

                        ipAddress = isValidIP ? resp.IpAddress : string.Empty;
                    }
                    else
                    {
                        Logger.Write($"ERROR: Unable to GetIpAddress in ProxyHelper -> DataGrabber. StatusCode: {response.StatusCode.ToString()}; Message: {response.Message}");
                    }

                    counter++;
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in ProxyHelper -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }


            return ipAddress;
        }




    }



    #region Set/Unset Proxy 


    public class InternetSettings
    {
        public static bool GetProxyEnabled()
        {
            InternetPerConnOptionList request = new InternetPerConnOptionList();
            InternetPerConnOption[] options = new InternetPerConnOption[1];
            IntPtr optionsPtr = IntPtr.Zero;

            try
            {
                options[0].dwOption = OptionType.INTERNET_PER_CONN_FLAGS;

                request.dwSize = Marshal.SizeOf(typeof(InternetPerConnOptionList));
                request.pszConnection = IntPtr.Zero;
                request.dwOptionCount = options.Length;
                request.dwOptionError = 0;

                int optionSize = Marshal.SizeOf(options[0]);

                optionsPtr = Marshal.AllocCoTaskMem(optionSize * options.Length);

                if (optionsPtr == IntPtr.Zero)
                {
                    return false;
                }

                for (int i = 0; i < options.Length; i++)
                {
                    IntPtr optionPtr = new IntPtr(optionsPtr.ToInt32() + (i * optionSize));
                    Marshal.StructureToPtr(options[i], optionPtr, false);
                }

                request.pOptions = optionsPtr;

                int requestLength = request.dwSize;

                bool result = NativeMethods.InternetQueryOption(IntPtr.Zero, InternetOptionActions.INTERNET_OPTION_PER_CONNECTION_OPTION, ref request, ref requestLength);

                if (!result)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                else
                {
                    for (int i = 0; i < options.Length; i++)
                    {
                        IntPtr opt = new IntPtr(optionsPtr.ToInt32() + (i * optionSize));
                        options[i] = (InternetPerConnOption)Marshal.PtrToStructure(opt, typeof(InternetPerConnOption));
                    }

                    return ((ProxyFlags)options[0].Value.dwValue & ProxyFlags.PROXY_TYPE_PROXY) == ProxyFlags.PROXY_TYPE_PROXY;
                }
            }
            finally
            {
                if (optionsPtr == IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(optionsPtr);
                }
            }
        }
        public static bool UnsetProxy()
        {
            InternetPerConnOptionList request = new InternetPerConnOptionList();
            InternetPerConnOption[] options = new InternetPerConnOption[3];
            int optionSize = 0;
            IntPtr optionsPtr = IntPtr.Zero;

            try
            {
                options[0].dwOption = OptionType.INTERNET_PER_CONN_FLAGS;
                options[0].Value.dwValue = (int)ProxyFlags.PROXY_TYPE_DIRECT;
                options[1].dwOption = OptionType.INTERNET_PER_CONN_PROXY_SERVER;
                options[1].Value.szValue = IntPtr.Zero;
                options[2].dwOption = OptionType.INTERNET_PER_CONN_PROXY_BYPASS;
                options[2].Value.szValue = IntPtr.Zero;

                optionSize = Marshal.SizeOf(typeof(InternetPerConnOption));
                optionsPtr = Marshal.AllocCoTaskMem(optionSize * options.Length);

                if (optionsPtr == IntPtr.Zero)
                {
                    return false;
                }

                for (int i = 0; i < options.Length; ++i)
                {
                    IntPtr optionPtr = new IntPtr(optionsPtr.ToInt32() + (i * optionSize));
                    Marshal.StructureToPtr(options[i], optionPtr, false);
                }

                request.pszConnection = IntPtr.Zero;
                request.dwSize = Marshal.SizeOf(typeof(InternetPerConnOptionList));
                request.dwOptionCount = options.Length;
                request.dwOptionError = 0;
                request.pOptions = optionsPtr;

                int requestLength = request.dwSize;

                if (NativeMethods.InternetSetOption(IntPtr.Zero, InternetOptionActions.INTERNET_OPTION_PER_CONNECTION_OPTION, ref request, requestLength))
                {
                    NativeMethods.InternetSetOption(IntPtr.Zero, InternetOptionActions.INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
                    NativeMethods.InternetSetOption(IntPtr.Zero, InternetOptionActions.INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                if (optionsPtr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(optionsPtr);
                }

                for (int i = 1; i < options.Length; i++)
                {
                    if (options[i].Value.szValue != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(options[i].Value.szValue);
                    }
                }
            }
        }
        public static bool SetProxy(string strProxy)
        {
            InternetPerConnOptionList request = new InternetPerConnOptionList();
            InternetPerConnOption[] options = new InternetPerConnOption[2];
            int optionSize = 0;
            IntPtr optionsPtr = IntPtr.Zero;

            try
            {
                options[0].dwOption = OptionType.INTERNET_PER_CONN_FLAGS;
                options[0].Value.dwValue = (int)ProxyFlags.PROXY_TYPE_PROXY;
                options[1].dwOption = OptionType.INTERNET_PER_CONN_PROXY_SERVER;
                options[1].Value.szValue = String.IsNullOrEmpty(strProxy) ? IntPtr.Zero : Marshal.StringToHGlobalAnsi(strProxy);

                optionSize = Marshal.SizeOf(typeof(InternetPerConnOption));
                optionsPtr = Marshal.AllocCoTaskMem(optionSize * options.Length);

                if (optionsPtr == IntPtr.Zero)
                {
                    return false;
                }

                for (int i = 0; i < options.Length; ++i)
                {
                    IntPtr optionPtr = new IntPtr(optionsPtr.ToInt32() + (i * optionSize));
                    Marshal.StructureToPtr(options[i], optionPtr, false);
                }

                request.pszConnection = IntPtr.Zero;
                request.dwSize = Marshal.SizeOf(typeof(InternetPerConnOptionList));
                request.dwOptionCount = options.Length;
                request.dwOptionError = 0;
                request.pOptions = optionsPtr;

                int requestLength = request.dwSize;

                if (NativeMethods.InternetSetOption(IntPtr.Zero, InternetOptionActions.INTERNET_OPTION_PER_CONNECTION_OPTION, ref request, requestLength))
                {
                    NativeMethods.InternetSetOption(IntPtr.Zero, InternetOptionActions.INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
                    NativeMethods.InternetSetOption(IntPtr.Zero, InternetOptionActions.INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                if (optionsPtr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(optionsPtr);
                }

                for (int i = 1; i < options.Length; i++)
                {
                    if (options[i].Value.szValue != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(options[i].Value.szValue);
                    }
                }
            }
        }
        public static bool SetProxy(string strProxy, string strExceptions)
        {
            InternetPerConnOptionList request = new InternetPerConnOptionList();
            InternetPerConnOption[] options = new InternetPerConnOption[3];
            int optionSize = 0;
            IntPtr optionsPtr = IntPtr.Zero;

            try
            {
                options[0].dwOption = OptionType.INTERNET_PER_CONN_FLAGS;
                options[0].Value.dwValue = (int)ProxyFlags.PROXY_TYPE_PROXY;
                options[1].dwOption = OptionType.INTERNET_PER_CONN_PROXY_SERVER;
                options[1].Value.szValue = String.IsNullOrEmpty(strProxy) ? IntPtr.Zero : Marshal.StringToHGlobalAnsi(strProxy);
                options[2].dwOption = OptionType.INTERNET_PER_CONN_PROXY_BYPASS;
                options[2].Value.szValue = String.IsNullOrEmpty(strExceptions) ? IntPtr.Zero : Marshal.StringToHGlobalAnsi(strExceptions);

                optionSize = Marshal.SizeOf(typeof(InternetPerConnOption));
                optionsPtr = Marshal.AllocCoTaskMem(optionSize * options.Length);

                if (optionsPtr == IntPtr.Zero)
                {
                    return false;
                }

                for (int i = 0; i < options.Length; ++i)
                {
                    IntPtr optionPtr = new IntPtr(optionsPtr.ToInt32() + (i * optionSize));
                    Marshal.StructureToPtr(options[i], optionPtr, false);
                }

                request.pszConnection = IntPtr.Zero;
                request.dwSize = Marshal.SizeOf(typeof(InternetPerConnOptionList));
                request.dwOptionCount = options.Length;
                request.dwOptionError = 0;
                request.pOptions = optionsPtr;

                int requestLength = request.dwSize;

                if (NativeMethods.InternetSetOption(IntPtr.Zero, InternetOptionActions.INTERNET_OPTION_PER_CONNECTION_OPTION, ref request, requestLength))
                {
                    NativeMethods.InternetSetOption(IntPtr.Zero, InternetOptionActions.INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
                    NativeMethods.InternetSetOption(IntPtr.Zero, InternetOptionActions.INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                if (optionsPtr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(optionsPtr);
                }

                for (int i = 1; i < options.Length; i++)
                {
                    if (options[i].Value.szValue != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(options[i].Value.szValue);
                    }
                }
            }
        }

        #region WinInet structures
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct InternetPerConnOptionList
        {
            public int dwSize;
            public IntPtr pszConnection;
            public int dwOptionCount;
            public int dwOptionError;
            public IntPtr pOptions;
        };

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Auto)]
        public struct InternetPerConnOptionData
        {
            [FieldOffset(0)]
            public int dwValue;

            [FieldOffset(0)]
            public IntPtr szValue;

            [FieldOffset(0)]
            public System.Runtime.InteropServices.ComTypes.FILETIME ftValue;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct InternetPerConnOption
        {
            public OptionType dwOption;
            public InternetPerConnOptionData Value;
        }
        #endregion

        #region WinInet enums
        //
        // options manifests for Internet{Query|Set}Option
        //
        public enum InternetOptionActions : uint
        {
            INTERNET_OPTION_REFRESH = 37,
            INTERNET_OPTION_SETTINGS_CHANGED = 39,
            INTERNET_OPTION_PER_CONNECTION_OPTION = 75
        }

        //
        // Options used in INTERNET_PER_CONN_OPTON struct
        //
        public enum OptionType
        {
            INTERNET_PER_CONN_FLAGS = 1,
            INTERNET_PER_CONN_PROXY_SERVER = 2,
            INTERNET_PER_CONN_PROXY_BYPASS = 3,
            INTERNET_PER_CONN_AUTOCONFIG_URL = 4,
            INTERNET_PER_CONN_AUTODISCOVERY_FLAGS = 5,
            INTERNET_PER_CONN_AUTOCONFIG_SECONDARY_URL = 6,
            INTERNET_PER_CONN_AUTOCONFIG_RELOAD_DELAY_MINS = 7,
            INTERNET_PER_CONN_AUTOCONFIG_LAST_DETECT_TIME = 8,
            INTERNET_PER_CONN_AUTOCONFIG_LAST_DETECT_URL = 9
        }

        //
        // PER_CONN_FLAGS
        //
        [Flags]
        public enum ProxyFlags
        {
            PROXY_TYPE_DIRECT = 0x00000001,  // direct to net
            PROXY_TYPE_PROXY = 0x00000002,  // via named proxy
            PROXY_TYPE_AUTO_PROXY_URL = 0x00000004,  // autoproxy URL
            PROXY_TYPE_AUTO_DETECT = 0x00000008   // use autoproxy detection
        }
        #endregion

        internal static class NativeMethods
        {
            [DllImport("wininet.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int lpdwBufferLength);

            [DllImport("wininet.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool InternetQueryOption(IntPtr hInternet, InternetOptionActions dwOption, ref InternetPerConnOptionList lpOptionList, ref int lpdwBufferLength);

            [DllImport("wininet.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool InternetSetOption(IntPtr hInternet, InternetOptionActions dwOption, ref InternetPerConnOptionList lpOptionList, int lpdwBufferLength);

            [DllImport("wininet.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool InternetSetOption(IntPtr hInternet, InternetOptionActions dwOption, IntPtr lpbuf, int lpdwBufferLength);
        }
    }

    #endregion

}
