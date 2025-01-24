#pragma warning disable CS1591 
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace VL.Core
{
    public static class SingleAppInstanceUtils
    {
        public static bool OtherInstanceIsRunning { get; private set; }

        public static Mutex GetOrAddAppMutex(string mutexName, out bool isNew, bool addGlobalPrefix = true)
        {
            mutexName = (addGlobalPrefix ? ("Global\\" + mutexName) : mutexName);
            Mutex result = new Mutex(false, mutexName, out isNew);
            SingleAppInstanceUtils.OtherInstanceIsRunning = !isNew;
            return result;
        }

        public static void SendStringMessage(string msg)
        {
            SingleAppInstanceUtils.SendDataMessage(SingleAppInstanceUtils.GetAlreadyRunningInstance(), msg);
        }

        public static Process GetAlreadyRunningInstance()
        {
            Process currentProcess = Process.GetCurrentProcess();
            List<Process> list = (from p in Process.GetProcessesByName(currentProcess.ProcessName)
                                  where p.MainModule?.FileVersionInfo?.ProductName?.StartsWith("vvvv gamma") == true
                                  select p).ToList<Process>();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Id != currentProcess.Id)
                {
                    return list[i];
                }
            }
            return null;
        }

        [DllImport("user32", EntryPoint = "SendMessageA")]
        private static extern int SendMessage(IntPtr Hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        public static void SendDataMessage(Process targetProcess, string msg)
        {
            IntPtr intPtr = Marshal.StringToHGlobalUni(msg);
            IntPtr intPtr2 = SingleAppInstanceUtils.IntPtrAlloc<COPYDATASTRUCT>(new COPYDATASTRUCT
            {
                dwData = IntPtr.Zero,
                lpData = intPtr,
                cbData = msg.Length * 2
            });
            SingleAppInstanceUtils.SendMessage(targetProcess.MainWindowHandle, 74, IntPtr.Zero, intPtr2);
            Marshal.FreeHGlobal(intPtr2);
            Marshal.FreeHGlobal(intPtr);
        }

        private static IntPtr IntPtrAlloc<T>(T param) where T : notnull
        {
            IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf<T>(param));
            Marshal.StructureToPtr<T>(param, intPtr, false);
            return intPtr;
        }

        public const int WM_COPYDATA = 74;
    }

    public struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        public IntPtr lpData;
    }
}
#pragma warning restore CS1591