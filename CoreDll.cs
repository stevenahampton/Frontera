using System;
using System.Collections;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using OpenNETCF.AppSettings;

using HANDLE = System.IntPtr;

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

    public const int SW_HIDE = 0;
    public const int SW_SHOWNORMAL = 1;
    public const int SW_SHOW = 5;

    [DllImport("coredll.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, EntryPoint = "ShowWindow")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("coredll.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, EntryPoint = "EnableWindow")]
    public static extern bool EnableWindow(IntPtr hWnd, bool enabled);

    [DllImport("coredll.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    
    [DllImport("coredll.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, EntryPoint = "GetWindowText")]
    public static extern int GetWindowText(IntPtr hWnd, string lpString, int nMaxCount);

    [DllImport("coredll.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, EntryPoint = "DestroyWindow")]
    public static extern bool DestroyWindow(IntPtr hWnd);

    private const uint GW_HWNDFIRST = 0;
    private const uint GW_HWNDLAST = 1;
    private const uint GW_HWNDNEXT = 2;
    private const uint GW_HWNDPREV = 3;
    private const uint GW_OWNER = 4;
    private const uint GW_CHILD = 5;

    [DllImport("coredll.dll")]
    static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("coredll.dll", SetLastError = true)]
    static extern int GetWindowText(IntPtr hWnd, [Out] StringBuilder lpString, int nMaxCount);

    [DllImport("coredll.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("coredll.dll")]
    static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("coredll.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int cmd);

    [DllImport("coredll.dll")]
    static extern IntPtr GetActiveWindow();

    [DllImport("coredll.dll")]
    static extern bool IsWindow(IntPtr handle);
    
    [DllImport("coredll.dll")]
    static extern bool IsWindowVisible(IntPtr handle);

    [DllImport("coredll.dll")]
    private extern static int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("coredll.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);
    
    private static int WM_CLOSE = 0x0010;

    public static void CloseWindow(Window window)
    {
      SendMessage(window.Handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
    }

    public static Window[] EnumerateTopWindows(SettingGroup ignores)
    {
      ArrayList windowList = new ArrayList();
      IntPtr hWnd = IntPtr.Zero;
      Window window = null;
      StringBuilder sb = null;

      // Get access-button window because it will always be present.
      hWnd = FindWindow(null, AccessButton.WindowTitle);
      hWnd = GetWindow(hWnd, GW_HWNDFIRST);

      while (hWnd != IntPtr.Zero)
      {
        // Check if it is a windowm and it is visible
        if (IsWindow(hWnd) && IsWindowVisible(hWnd))
        {
          IntPtr parentWin = GetParent(hWnd);
          // Make sure that the window doesn't hav a parent
          if ((parentWin == IntPtr.Zero))
          {
            int length = GetWindowTextLength(hWnd);
            // Does it have the text caption
            if (length > 0)
            {
              sb = new StringBuilder(length + 1);
              GetWindowText(hWnd, sb, sb.Capacity);
              string text = sb.ToString();
              int nilidx = text.IndexOf('\0');
              if (nilidx >= 0)
              {
                text = text.Substring(0, nilidx);
              }
              // Exclude some known system programs
              if (!CheckForDialogs(windowList, text) && !IsInIgnoreList(ignores, text))
              {
                window = new Window();
                window.Handle = hWnd;
                window.Caption = text;
                windowList.Add(window);
              }
            }
          }
        }
        hWnd = GetWindow(hWnd, GW_HWNDNEXT);
      }
      return (Window[])windowList.ToArray(typeof(Window));
    }

    private static bool IsInIgnoreList(SettingGroup ignores, String title)
    {
      foreach (Setting ig in ignores.Settings)
      {
        if (ig.Value.ToString().ToUpper().Equals(title.ToUpper()))
        {
          return true;
        }
      }
      return false;
    }

    private static bool CheckForDialogs(ArrayList windowList, string value)
    {
      foreach (Window window in windowList)
      {
        if (window.Caption == value)
        {
          return true;
        }
      }
      return false;
    }

    [DllImport("coredll.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
    public static extern HANDLE CreateEvent(HANDLE lpEventAttributes, [In, MarshalAs(UnmanagedType.Bool)] bool bManualReset, [In, MarshalAs(UnmanagedType.Bool)] bool bIntialState, [In, MarshalAs(UnmanagedType.BStr)] string lpName);

    [DllImport("coredll.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EventModify(HANDLE hEvent, [In, MarshalAs(UnmanagedType.U4)] int dEvent);
    public enum EventFlags
    {
      PULSE = 1,
      RESET = 2,
      SET = 3
    }
    public static bool SetEvent(HANDLE hEvent)
    {
      return EventModify(hEvent, (int)EventFlags.SET);
    }
    public static bool ResetEvent(HANDLE hEvent)
    {
      return EventModify(hEvent, (int)EventFlags.RESET);
    }
    
    [DllImport("coredll.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(HANDLE hObject);

#endregion
  }

  public class Window : IComparable
  {
    public IntPtr Handle;
    public string Caption;

    public override string ToString()
    {
      return Caption;
    }


    #region IComparable Members

    public int CompareTo(object obj)
    {
      if (obj == null)
      {
        return -1;
      }
      else if (obj is Window)
      {
        return this.Caption.CompareTo(((Window)obj).Caption);
      }
      else
      {
        return -1;
      }
    }

    #endregion
  }
}
