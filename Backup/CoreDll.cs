using System;
using System.Collections;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Frontera
{
  class CoreDll
  {
#region PInvoke declarations

    [DllImport("coredll.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, EntryPoint = "FindWindow")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    public const int SWP_ASYNCWINDOWPOS = 0x4000;
    public const int SWP_DEFERERASE = 0x2000;
    public const int SWP_DRAWFRAME = 0x0020;
    public const int SWP_FRAMECHANGED = 0x0020;
    public const int SWP_HIDEWINDOW = 0x0080;
    public const int SWP_NOACTIVATE = 0x0010;
    public const int SWP_NOCOPYBITS = 0x0100;
    public const int SWP_NOMOVE = 0x0002;
    public const int SWP_NOOWNERZORDER = 0x0200;
    public const int SWP_NOREDRAW = 0x0008;
    public const int SWP_NOREPOSITION = 0x0200;
    public const int SWP_NOSENDCHANGING = 0x0400;
    public const int SWP_NOSIZE = 0x0001;
    public const int SWP_NOZORDER = 0x0004;
    public const int SWP_SHOWWINDOW = 0x0040;

    public const int HWND_TOP = 0;
    public const int HWND_BOTTOM = 1;
    public const int HWND_TOPMOST = -1;
    public const int HWND_NOTOPMOST = -2;

    [DllImport("coredll.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, EntryPoint = "SetWindowPos", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("CoreDll.DLL", SetLastError = true)]
    public extern static int CreateProcess
    (
      String imageName,
      String cmdLine,
      IntPtr lpProcessAttributes,
      IntPtr lpThreadAttributes,
      Int32 boolInheritHandles,
      Int32 dwCreationFlags,
      IntPtr lpEnvironment,
      IntPtr lpszCurrentDir,
      byte[] si,
      ProcessInfo pi
    );

    // GetLastError PInvoke API
    [DllImport("CoreDll.dll")]
    public extern static Int32 GetLastError();

    public Int32 GetPInvokeError()
    {
      return GetLastError();
    }

    [DllImport("CoreDll.dll")]
    public extern static Int32 WaitForSingleObject(IntPtr Handle, Int32
      Wait);

    public static bool CreateProcess(String ExeName, String CmdLine, ProcessInfo
      pi, bool wait)
    {
      Int32 INFINITE;
      unchecked { INFINITE = (int)0xFFFFFFFF; }
      bool result = false;
      if (pi == null)
      {
        pi = new ProcessInfo();
      }
      byte[] si = new byte[128];
      result = CreateProcess(ExeName, CmdLine, IntPtr.Zero, IntPtr.Zero, 0,
        0, IntPtr.Zero, IntPtr.Zero, si, pi) != 0;
      if (wait)
      {
        WaitForSingleObject(pi.hProcess, INFINITE);
      }
      return result;
    }

    public sealed class ProcessInfo
    {
      public IntPtr hProcess = IntPtr.Zero;
      public IntPtr hThread = IntPtr.Zero;
      public int dwProcessID = 0;
      public int dwThreadID = 0;
    }

    public static Icon extractIconFromExe(string file, bool large)
    {
      int readIconCount = 0;
      IntPtr[] hDummy = new IntPtr[1] { IntPtr.Zero };
      IntPtr[] hIconEx = new IntPtr[1] { IntPtr.Zero };

      try
      {
        if (large)
        {
          readIconCount = ExtractIconEx(file, 0, hIconEx, hDummy, 1);
        }
        else
        {
          readIconCount = ExtractIconEx(file, 0, hDummy, hIconEx, 1);
        }

        if (readIconCount > 0 && hIconEx[0] != IntPtr.Zero)
        {
					return CoreDll.GetIconFromHandle(hIconEx[0]);
        }
        else // NO ICONS READ
        {
          return null;
        }
      }
      catch (Exception ex)
      {
        /* EXTRACT ICON ERROR */
        // BUBBLE UP
        //throw new ApplicationException("Could not extract icon", ex);
        return null;
      }
      finally
      {
        // RELEASE RESOURCES
        foreach (IntPtr ptr in hIconEx)
        {
          if (ptr != IntPtr.Zero)
          {
            DestroyIcon(ptr);
          }
        }
        foreach (IntPtr ptr in hDummy)
        {
          if (ptr != IntPtr.Zero)
          {
            DestroyIcon(ptr);
          }
        }
      }
    }

		public static Icon GetIconFromHandle(IntPtr handle)
		{
			Icon icon;
			// .NET 2.0 and later
#if DOTNET2
			icon = (Icon)System.Drawing.Icon.FromHandle(handle).Clone();
#else
			// .NET 1.1
			icon = null;
#endif

			return icon;
		}

    [DllImport("coredll.dll")]
    public static extern int ExtractIconEx(string szFileName, int nIconIndex,
      IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);

    [DllImport("coredll.dll", EntryPoint = "DestroyIcon", SetLastError = true)]
    public static extern int DestroyIcon(IntPtr hIcon);

    /// <summary>
    /// Process calls
    /// </summary>
    public const int TH32CS_SNAPPROCESS = 0x00000002;
    [DllImport("toolhelp.dll")]
    public static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processid);
    [DllImport("toolhelp.dll")]
    public static extern int CloseToolhelp32Snapshot(IntPtr handle);
    [DllImport("toolhelp.dll")]
    public static extern int Process32First(IntPtr handle, byte[] pe);
    [DllImport("toolhelp.dll")]
    public static extern int Process32Next(IntPtr handle, byte[] pe);
    [DllImport("coredll.dll")]
    public static extern IntPtr OpenProcess(int flags, bool fInherit, int PID);
    public const int PROCESS_TERMINATE = 1;
    [DllImport("coredll.dll")]
    public static extern bool TerminateProcess(IntPtr hProcess, uint ExitCode);
    [DllImport("coredll.dll")]
    public static extern bool CloseHandle(IntPtr handle);
    public const int INVALID_HANDLE_VALUE = -1;

    public const int SW_HIDE = 0;
    public const int SW_SHOWNORMAL = 1;
    public const int SW_SHOW = 5;

    [DllImport("coredll.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, EntryPoint = "ShowWindow")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("coredll.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, EntryPoint = "EnableWindow")]
    public static extern bool EnableWindow(IntPtr hWnd, bool enabled);

    public const uint GW_HWNDNEXT = 2;
    public const uint GW_HWNDPREV = 3;

    [DllImport("coredll.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, EntryPoint = "GetWindow")]
    public static extern IntPtr GetWindow(IntPtr hWnd, uint wCmd);

    [DllImport("coredll.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, EntryPoint = "GetWindowText")]
    public static extern int GetWindowText(IntPtr hWnd, string lpString, int nMaxCount);

    [DllImport("coredll.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, EntryPoint = "DestroyWindow")]
    public static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("coredll.dll", SetLastError = true)]
    internal static extern bool KernelIoControl(UInt32 dwIoControlCode,
    IntPtr lpInBuf,
    Int32 nInBufSize,
    byte[] lpOutBuf,
    Int32 nOutBufSize,
    ref Int32 lpBytesReturned);

    public static void ResetUnit()
    {
      int bytesReturned = 0;
      uint IOCTL_HAL_REBOOT = 0x0101003C;

      KernelIoControl(IOCTL_HAL_REBOOT, IntPtr.Zero, 0, null, 0, ref bytesReturned);

    }
#endregion
  }
}
