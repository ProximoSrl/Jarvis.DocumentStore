﻿using System;
using System.Runtime.InteropServices;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable RCS1213 // Remove unused member declaration.
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable RCS1018 // Add default access modifier.

namespace Jarvis.DocumentStore.Core.Storage.FileSystem
{
    /// <summary>
    /// http://stackoverflow.com/questions/659013/accessing-a-shared-file-unc-from-a-remote-non-trusted-domain-with-credentials
    /// </summary>
    public class PinvokeWindowsNetworking
    {
        #region Consts

        private const int RESOURCE_CONNECTED = 0x00000001;
        private const int RESOURCE_GLOBALNET = 0x00000002;
        private const int RESOURCE_REMEMBERED = 0x00000003;
        private const int RESOURCETYPE_ANY = 0x00000000;
        private const int RESOURCETYPE_DISK = 0x00000001;
        private const int RESOURCETYPE_PRINT = 0x00000002;
        private const int RESOURCEDISPLAYTYPE_GENERIC = 0x00000000;
        private const int RESOURCEDISPLAYTYPE_DOMAIN = 0x00000001;
        private const int RESOURCEDISPLAYTYPE_SERVER = 0x00000002;
        private const int RESOURCEDISPLAYTYPE_SHARE = 0x00000003;
        private const int RESOURCEDISPLAYTYPE_FILE = 0x00000004;
        private const int RESOURCEDISPLAYTYPE_GROUP = 0x00000005;
        private const int RESOURCEUSAGE_CONNECTABLE = 0x00000001;
        private const int RESOURCEUSAGE_CONTAINER = 0x00000002;
        private const int CONNECT_INTERACTIVE = 0x00000008;
        private const int CONNECT_PROMPT = 0x00000010;
        private const int CONNECT_REDIRECT = 0x00000080;
        private const int CONNECT_UPDATE_PROFILE = 0x00000001;
        private const int CONNECT_COMMANDLINE = 0x00000800;
        private const int CONNECT_CMD_SAVECRED = 0x00001000;
        private const int CONNECT_LOCALDRIVE = 0x00000100;

        #endregion

        #region Errors

        private const int NO_ERROR = 0;
        private const int ERROR_ACCESS_DENIED = 5;
        private const int ERROR_ALREADY_ASSIGNED = 85;
        private const int ERROR_BAD_DEVICE = 1200;
        private const int ERROR_BAD_NET_NAME = 67;
        private const int ERROR_BAD_PROVIDER = 1204;
        private const int ERROR_CANCELLED = 1223;
        private const int ERROR_EXTENDED_ERROR = 1208;
        private const int ERROR_INVALID_ADDRESS = 487;
        private const int ERROR_INVALID_PARAMETER = 87;
        private const int ERROR_INVALID_PASSWORD = 1216;
        private const int ERROR_MORE_DATA = 234;
        private const int ERROR_NO_MORE_ITEMS = 259;
        private const int ERROR_NO_NET_OR_BAD_PATH = 1203;
        private const int ERROR_NO_NETWORK = 1222;
        private const int ERROR_BAD_PROFILE = 1206;
        private const int ERROR_CANNOT_OPEN_PROFILE = 1205;
        private const int ERROR_DEVICE_IN_USE = 2404;
        private const int ERROR_NOT_CONNECTED = 2250;
        private const int ERROR_OPEN_FILES = 2401;

        private struct ErrorClass
        {
            public int num;
            public string message;

            public ErrorClass(int num, string message)
            {
                this.num = num;
                this.message = message;
            }
        }

        // Created with excel formula:
        // ="new ErrorClass("&A1&", """&PROPER(SUBSTITUTE(MID(A1,7,LEN(A1)-6), "_", " "))&"""), "
        private static ErrorClass[] ERROR_LIST = new ErrorClass[] {
            new ErrorClass(ERROR_ACCESS_DENIED, "Error: Access Denied"),
            new ErrorClass(ERROR_ALREADY_ASSIGNED, "Error: Already Assigned"),
            new ErrorClass(ERROR_BAD_DEVICE, "Error: Bad Device"),
            new ErrorClass(ERROR_BAD_NET_NAME, "Error: Bad Net Name"),
            new ErrorClass(ERROR_BAD_PROVIDER, "Error: Bad Provider"),
            new ErrorClass(ERROR_CANCELLED, "Error: Cancelled"),
            new ErrorClass(ERROR_EXTENDED_ERROR, "Error: Extended Error"),
            new ErrorClass(ERROR_INVALID_ADDRESS, "Error: Invalid Address"),
            new ErrorClass(ERROR_INVALID_PARAMETER, "Error: Invalid Parameter"),
            new ErrorClass(ERROR_INVALID_PASSWORD, "Error: Invalid Password"),
            new ErrorClass(ERROR_MORE_DATA, "Error: More Data"),
            new ErrorClass(ERROR_NO_MORE_ITEMS, "Error: No More Items"),
            new ErrorClass(ERROR_NO_NET_OR_BAD_PATH, "Error: No Net Or Bad Path"),
            new ErrorClass(ERROR_NO_NETWORK, "Error: No Network"),
            new ErrorClass(ERROR_BAD_PROFILE, "Error: Bad Profile"),
            new ErrorClass(ERROR_CANNOT_OPEN_PROFILE, "Error: Cannot Open Profile"),
            new ErrorClass(ERROR_DEVICE_IN_USE, "Error: Device In Use"),
            new ErrorClass(ERROR_EXTENDED_ERROR, "Error: Extended Error"),
            new ErrorClass(ERROR_NOT_CONNECTED, "Error: Not Connected"),
            new ErrorClass(ERROR_OPEN_FILES, "Error: Open Files"),
        };

        private static string GetErrorForNumber(int errNum)
        {
            foreach (ErrorClass er in ERROR_LIST)
            {
                if (er.num == errNum)
                {
                    return er.message;
                }
            }
            return "Error: Unknown, " + errNum;
        }

        #endregion

        [DllImport("Mpr.dll")]
        private static extern int WNetUseConnection(
            IntPtr hwndOwner,
            NETRESOURCE lpNetResource,
            string lpPassword,
            string lpUserID,
            int dwFlags,
            string lpAccessName,
            string lpBufferSize,
            string lpResult
        );

        [DllImport("Mpr.dll")]
        private static extern int WNetCancelConnection2(
            string lpName,
            int dwFlags,
            bool fForce
        );

        [StructLayout(LayoutKind.Sequential)]
        private class NETRESOURCE
        {
            public int dwScope = 0;
            public int dwType = 0;
            public int dwDisplayType = 0;
            public int dwUsage = 0;
            public string lpLocalName = "";
            public string lpRemoteName = "";
            public string lpComment = "";
            public string lpProvider = "";
        }

        public static string ConnectToRemote(string remoteUNC, string username, string password)
        {
            return ConnectToRemote(remoteUNC, username, password, false);
        }

        public static string ConnectToRemote(string remoteUNC, string username, string password, bool promptUser)
        {
            NETRESOURCE nr = new NETRESOURCE();
            nr.dwType = RESOURCETYPE_DISK;
            nr.lpRemoteName = remoteUNC;

            int ret;
            if (promptUser)
            {
                ret = WNetUseConnection(IntPtr.Zero, nr, "", "", CONNECT_INTERACTIVE | CONNECT_PROMPT, null, null, null);
            }
            else
            {
                ret = WNetUseConnection(IntPtr.Zero, nr, password, username, 0, null, null, null);
            }
            if (ret == NO_ERROR)
            {
                return null;
            }

            return GetErrorForNumber(ret);
        }

        public static string DisconnectRemote(string remoteUNC)
        {
            int ret = WNetCancelConnection2(remoteUNC, CONNECT_UPDATE_PROFILE, false);
            if (ret == NO_ERROR)
            {
                return null;
            }

            return GetErrorForNumber(ret);
        }
    }
}
#pragma warning restore S1144 // Unused private types or members should be removed
#pragma warning restore RCS1018 // Add default access modifier.
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore RCS1213 // Remove unused member declaration.
