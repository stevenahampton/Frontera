using System;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using Frontera;

namespace Frontera
{
	/// <summary>
	/// Window and process manager and program launcher for Windows CE.
	/// </summary>
	public class MainForm : System.Windows.Forms.Form
  {
    private ListView listView;
    private Button windowButton;
    private Button processButton;
    private Button programButton;
    private Button refreshButton;
    private Button actionButton;

    private AccessButton accessButton;

		public static string HomeDirectory = System.IO.Path.GetDirectoryName(
			System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

		public static string CardDirectory = "\\SDMMC\\Frontera";

		private string resetFile = "reset.bmp";
    private string windowFile = "window.bmp";
    public static string launchFile = "launch.bmp";
    private string processFile = "process.bmp";

    private string launchList = "launch.txt";
    private string ignoreList = "ignore.txt";
    private static string debugOut   = "debug.txt";

    private static StreamWriter debugStream = startDebug();

    private ArrayList processes;
    private ArrayList windows;
    private ArrayList programs;

		private WinDetails currWin = null;

    private ArrayList ignoredWindows;

		private static Rectangle screen = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
		public static int ScreenWidth = screen.Width;
    public static int ScreenHeight = screen.Height;
		private static int X = screen.X;
		private static int Y = screen.X;
		private static int numButtons = 5;

    private static int buttonSpacing = 5;
    public static int ButtonWidth = 
      (ScreenWidth - (numButtons + 1) * buttonSpacing) / numButtons;
    public static int ButtonHeight = 32;
    private static int buttonYPos = Y + ScreenHeight - ButtonHeight - 26;

    private static string endText = "End Proc";
    private static string switchText = "Switch";
    private static string launchText = "Launch";

    public static string LauncherTitle = "Frontera - Launcher";
    public static string WindowsTitle = "Frontera - Windows";
    private static string processesTitle = "Frontera - Processes";

    private IntPtr accessWin = IntPtr.Zero;
		private System.Windows.Forms.MainMenu mm;
		private System.Windows.Forms.MenuItem frontera;
		private System.Windows.Forms.MenuItem reset;
		private System.Windows.Forms.MenuItem end;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

    public MainForm(AccessButton accessButton)
		{
			this.InitializeComponent();
			this.InitializeExtras();

      loadIgnoredWindows();
			
			this.accessButton = accessButton;
      accessWin = CoreDll.FindWindow(null, AccessButton.WindowTitle);
#if DOTNET2
			accessButton.TopMost = true;
#else
			CoreDll.SetWindowPos(accessWin, (IntPtr)CoreDll.HWND_TOPMOST, 0, 0, 0, 0, 
				CoreDll.SWP_NOSIZE | CoreDll.SWP_NOMOVE);
#endif
			windows = GetAllWindows();
			// Default view is program launcher
      refreshLauncher();
    }

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.listView = new System.Windows.Forms.ListView();
			this.windowButton = new System.Windows.Forms.Button();
			this.actionButton = new System.Windows.Forms.Button();
			this.processButton = new System.Windows.Forms.Button();
			this.programButton = new System.Windows.Forms.Button();
			this.refreshButton = new System.Windows.Forms.Button();
			this.mm = new System.Windows.Forms.MainMenu();
			this.frontera = new System.Windows.Forms.MenuItem();
			this.reset = new System.Windows.Forms.MenuItem();
			this.end = new System.Windows.Forms.MenuItem();
			// 
			// listView
			// 
			this.listView.Size = new System.Drawing.Size(325, 180);
			this.listView.ItemActivate += new System.EventHandler(this.listView_ItemActivate);
			this.listView.SelectedIndexChanged += new System.EventHandler(this.listView_SelectedIndexChanged);
			// 
			// frontera
			// 
			this.frontera.Text = "Frontera";
			// 
			// reset
			// 
			this.reset.Text = "Reset Unit";
			this.reset.Click += new System.EventHandler(this.reset_Click);
			// 
			// end
			// 
			this.end.Text = "Exit";
			this.end.Click += new System.EventHandler(this.end_Click);
			// 
			// MainForm
			// 
			this.BackColor = System.Drawing.SystemColors.Desktop;
			this.ClientSize = new System.Drawing.Size(319, 238);
			this.ControlBox = false;
			this.Controls.Add(this.listView);
			this.Controls.Add(this.windowButton);
			this.Controls.Add(this.processButton);
			this.Controls.Add(this.programButton);
			this.Controls.Add(this.refreshButton);
			this.Controls.Add(this.actionButton);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.Activated += new System.EventHandler(this.MainForm_Activated);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.MainForm_Paint);

		}

		private static void listResources()
		{            
			// get a reference to the current assembly
			System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
        
			// get a list of resource names from the manifest
			string [] resNames = a.GetManifestResourceNames();

			WriteDebug(String.Format("Found {0} resources\r\n", resNames.Length));
			WriteDebug("----------");
			foreach(string s in resNames)
			{
				WriteDebug(s);
			}            
			WriteDebug("----------");
		}

		void InitializeExtras()
		{
			listResources();
/*			Stream ico = new StreamReader(
				System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(
						"Frontera.frontera.ico")).BaseStream;
			Stream ico = new FileStream(HomeDirectory + "\\frontera.ico", FileMode.Open);
			WriteDebug("Stream length = " + ico.Length);
			this.Icon = new Icon(ico, 16, 16);
			ico.Close();
*/
			this.listView.Size = new System.Drawing.Size(ScreenWidth, ScreenHeight - ButtonHeight - 28);
			this.listView.Activation = ItemActivation.OneClick;
			//
			// windowButton
			// 
			int xpos = buttonSpacing;
			//windowButton.Image = new Bitmap(homeDir + "\\window.gif");
			windowButton.Text = "Wins";
			windowButton.Location = new Point(xpos, buttonYPos);
			windowButton.Size = new Size(ButtonWidth, ButtonHeight);
			windowButton.Click += new System.EventHandler(this.windowButton_Click);
			//
			// processButton
			//
			xpos += ButtonWidth + buttonSpacing;
			//processButton.Image = new Bitmap(homeDir + "\\process.gif");
			processButton.Text = "Procs";
			processButton.Location = new Point(xpos, buttonYPos);
			processButton.Size = new Size(ButtonWidth, ButtonHeight);
			//Hook up into click event
			processButton.Click += new System.EventHandler(this.processButton_Click);
			//
			// programButton
			//
			xpos += ButtonWidth + buttonSpacing;
			//programButton.Image = new Bitmap(homeDir + "\\program.gif");
			programButton.Text = "Progs";
			programButton.Location = new Point(xpos, buttonYPos);
			programButton.Size = new Size(ButtonWidth, ButtonHeight);
			//Hook up into click event
			programButton.Click += new System.EventHandler(this.programButton_Click);
			//
			// refreshButton
			//
			xpos += ButtonWidth + buttonSpacing;
			refreshButton.Text = "Refresh";
			//refreshButton.Image = new Bitmap(homeDir + "\\refresh.gif");
			refreshButton.Location = new Point(xpos, buttonYPos);
			refreshButton.Size = new Size(ButtonWidth, ButtonHeight);
			//Hook up into click event
			refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
			//
			// actionButton
			//
			xpos += ButtonWidth + buttonSpacing;
			//actionButton.Image = new Bitmap(homeDir + "\\refresh.gif");
			actionButton.Location = new Point(xpos, buttonYPos);
			actionButton.Size = new Size(ButtonWidth, ButtonHeight);
			//Hook up into click event
			actionButton.Click += new System.EventHandler(this.actionButton_Click);
			
			//this.listView.BackColor = System.Drawing.SystemColors.Desktop;
			//this.ClientSize = new System.Drawing.Size(638, 455);
			//this.ControlBox = false;
			//this.FormBorderStyle = FormBorderStyle.None;
		}

    void reset_Click(object sender, EventArgs e)
    {
      CoreDll.ResetUnit();
    }

    void end_Click(object sender, EventArgs e)
    {
      this.Dispose();
    }

    private void MainForm_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
    {
    }

    private void MainForm_Load(object sender, System.EventArgs e)
    {
    }

    private void MainForm_Activated(object sender, System.EventArgs e)
    {
			this.Focus();
    }

    private void windowButton_Click(object sender, System.EventArgs e)
    {
      actionButton.Text = switchText;
      DoRefresh();
    }

    private void processButton_Click(object sender, System.EventArgs e)
    {
      actionButton.Text = endText;
      DoRefresh();
    }

    private void programButton_Click(object sender, System.EventArgs e)
    {
      actionButton.Text = launchText;
      DoRefresh();
    }

    private void refreshButton_Click(object sender, System.EventArgs e)
		{
      DoRefresh();
		}

    private void actionButton_Click(object sender, System.EventArgs e)
    {
      doAction(true);
    }

    private void listView_SelectedIndexChanged(object sender, System.EventArgs e)
    {
		}

    private void listView_ItemActivate(object sender, System.EventArgs e)
    {
      doAction(false);
    }

    private void doAction(bool fromButton)
    {
      if (actionButton.Text.Equals(launchText))
      {
        launchSelectedPrograms();
      }
      else if (actionButton.Text.Equals(endText) && fromButton)
      {
        endSelectedProcesses();
        DoRefresh();
      }
      else if (actionButton.Text.Equals(switchText))
      {
        placeAtBack();
        switchToSelectedWindows();
      }
    }

    private void placeAtBack()
    {
      this.Hide();
      accessButton.WinsTimer.Enabled = true;
    }

    public void DoRefresh()
    {
      listView.Items.Clear();
      if (actionButton.Text.Equals(launchText))
      {
        refreshLauncher();
      }
      else if (actionButton.Text.Equals(endText))
      {
        refreshProcesses();
      }
      else if (actionButton.Text.Equals(switchText))
      {
        refreshWindows();
      }
    }

    private void refreshLauncher()
    {
      actionButton.Text = launchText;
      windowButton.Enabled = true;
      processButton.Enabled = true;
      programButton.Enabled = false;
      this.Text = LauncherTitle;

      listView.View = View.LargeIcon;

      ImageList imageList = new ImageList();
      // Reset icon
      Bitmap bm = new Bitmap(HomeDirectory + "\\" + resetFile);
      imageList.Images.Add(bm);
      // Default icon
      bm = new Bitmap(HomeDirectory + "\\" + launchFile);
      imageList.Images.Add(bm);
      // Add columns
      listView.Columns.Clear();
      listView.Columns.Add("Name",
        -2,
        HorizontalAlignment.Left);
      listView.Columns.Add("Executable",
        -2,
        HorizontalAlignment.Left);
      listView.Columns.Add("Arguments",
        -2,
        HorizontalAlignment.Left);

      programs = new ArrayList();
			// Look for launch list in home directory and under Frontera
			// on the memory card.
      StreamReader s = File.OpenText(HomeDirectory + "\\" + launchList);
      string read = null;
      while ((read = s.ReadLine()) != null)
      {
        programs.Add(read);
      }
			string cardlaunch = CardDirectory + "\\" + launchList;
			FileInfo cl = new FileInfo(cardlaunch);
			if (cl.Exists && !CardDirectory.ToUpper().Equals(HomeDirectory.ToUpper()))
			{
				s = File.OpenText(cardlaunch);
				read = null;
				while ((read = s.ReadLine()) != null)
				{
					programs.Add(read);
				}
			}
      programs.Sort();

      listView.Items.Clear();
      // Add reset option first
      ListViewItem resetItem = new ListViewItem(new string[] { "RESET", "", "" });
      resetItem.ImageIndex = 0;
      listView.Items.Add(resetItem);
      // Add the rest from list file
      int i = 2;
      foreach (string prog in programs)
      {
        string[] parts = prog.Split(new char[] { ',' });
        ListViewItem lvi = new ListViewItem(parts);

        Icon icon = CoreDll.extractIconFromExe(parts[1], false);
        if (icon != null)
        {
          imageList.Images.Add(icon);
          lvi.ImageIndex = i++;
        }
        else
        {
          lvi.ImageIndex = 1;
        }
        listView.Items.Add(lvi);
      }
      s.Close();
      listView.LargeImageList = imageList;
      listView.SmallImageList = imageList;
    }

    private void refreshProcesses()
    {
      actionButton.Text = endText;
      windowButton.Enabled = true;
      processButton.Enabled = false;
      programButton.Enabled = true;
      this.Text = processesTitle;

      listView.View = View.List;
      ImageList imageList = new ImageList();
      Bitmap bm = new Bitmap(HomeDirectory + "\\" + processFile);
      // Default icon
      imageList.Images.Add(bm);
      // Add columns
      listView.Columns.Clear();
      listView.Columns.Add("",
        ScreenWidth / 2 - 5,
        HorizontalAlignment.Left);
      listView.Columns.Add("",
        ScreenWidth / 2 - 5,
        HorizontalAlignment.Left);

      processes = new ArrayList();
      WriteDebug("Before GetProcesses");
			Frontera.Process[] procs = Frontera.Process.GetProcesses();
			WriteDebug("After GetProcesses");
			processes.AddRange(procs);
      processes.Sort();

      listView.Items.Clear();
      int i = 1;
      foreach (Frontera.Process proc in processes)
      {
				string[] procDetails = new string[] { proc.ProcessName, proc.Handle.ToString(), proc.BaseAddress.ToString(), proc.ThreadCount.ToString() };
        ListViewItem lvi = new ListViewItem(procDetails);
        lvi.Text = proc.ProcessName;

        Icon icon = CoreDll.extractIconFromExe(proc.ProcessName, false);
        if (icon != null)
        {
          imageList.Images.Add(icon);
          lvi.ImageIndex = i++;
        }
        else
        {
          lvi.ImageIndex = 0;
        }
        listView.Items.Add(lvi);
      }
      listView.SmallImageList = imageList;
      listView.LargeImageList = imageList;
    }

		public void NextWindow()
		{
			windows = GetAllWindows();
			int i = 0;
			if (currWin != null)
			{
				WriteDebug("CurrWindow-> "+currWin.Title);
				for (; i < windows.Count; i++)
				{
					if (((WinDetails)windows[i]).Title.Equals(currWin.Title))
					{
						break;
					}
				}
				i++;
			}
			if (i >= windows.Count)
			{
				i = 0;
			}
			if (i < windows.Count)
			{
				currWin = (WinDetails)windows[i];
				CoreDll.ShowWindow(CoreDll.FindWindow(null, currWin.Title), CoreDll.SW_SHOW);
			}
		}

    private void refreshWindows()
    {
      actionButton.Text = switchText;
      windowButton.Enabled = false;
      processButton.Enabled = true;
      programButton.Enabled = true;
      this.Text = WindowsTitle;

      listView.View = View.LargeIcon;
      // Add columns
      listView.Columns.Clear();
      listView.Columns.Add("Title",
        -2,
        HorizontalAlignment.Left);
      listView.Columns.Add("Id",
        -2,
        HorizontalAlignment.Left);

      ImageList imageList = new ImageList();
      Bitmap bm = new Bitmap(HomeDirectory + "\\" + windowFile);
      imageList.Images.Add(bm);
      listView.LargeImageList = imageList;
      listView.SmallImageList = imageList;

      // Populate the window list
      listView.Items.Clear();
      windows = GetAllWindows();
			foreach (WinDetails win in windows)
      {
        ListViewItem lvi = new ListViewItem(new string[] { win.Title, win.Id.ToString() });
        lvi.ImageIndex = 0;
        listView.Items.Add(lvi);
      }
    }

    private void launchSelectedPrograms()
    {
      bool resetCancelled = false;
      for (int i = 0; i < listView.SelectedIndices.Count; i++)
      {
        int idx = listView.SelectedIndices[i];
        if (idx == 0)
        {
          // Special case - reset the unit.
          ConfirmDialog cd = new ConfirmDialog("Are you sure?");
          cd.ShowDialog();
          if (cd.isConfirmed())
          {
            CoreDll.ResetUnit();
          }
          else
          {
            resetCancelled = true;
          }

        }
        else
        {
          ListViewItem lvi = listView.Items[idx];
          launch(lvi.SubItems[1].Text, lvi.SubItems[2].Text);
        }
      }
      if (resetCancelled)
      {
        this.Show();
      }
      else
      {
        placeAtBack();
      }
    }

    private void endSelectedProcesses()
    {
      for (int i = 0; i < listView.SelectedIndices.Count; i++)
      {
        Process proc = (Process)processes[listView.SelectedIndices[i]];
        proc.Kill();
      }
    }

    /// <summary>
    /// Switch to the selected window(s)
    /// </summary>
		private void switchToSelectedWindows()
		{
			for (int i = 0; i < listView.SelectedIndices.Count; i++)
			{
				ListViewItem lvi = listView.Items[listView.SelectedIndices[i]];
				switchToSelectedWindow(lvi.SubItems[0].Text, lvi.SubItems[1].Text);
			}
		}

    private static void launch(string exe, string args)
    {
      CoreDll.ProcessInfo pi = new CoreDll.ProcessInfo();
      CoreDll.CreateProcess(exe, null, pi, false);
    }

		public ArrayList GetAllWindows()
		{
			IntPtr hWnd = accessWin;
			ArrayList wins = new ArrayList();
			while (hWnd != (IntPtr)null && wins.Count < 1000)
			{
				IntPtr nextWin = CoreDll.GetWindow(hWnd, CoreDll.GW_HWNDNEXT);
				if (nextWin != (IntPtr)null)
				{
					char[] buffer = new char[101];
					string title = new string(buffer);
					int len = CoreDll.GetWindowText(nextWin, title, 100);
					if (len > 0)
					{
						String titleS = title.Substring(0, len);
						
						if (!isIgnoredWindow(titleS))
						{
							wins.Add(new WinDetails(hWnd, titleS));
						}
					}
				}
				hWnd = nextWin;
			}
			wins.Sort();

			return wins;
		}

    private void loadIgnoredWindows()
    {
      ignoredWindows = new ArrayList();
      StreamReader s = File.OpenText(HomeDirectory + "\\" + ignoreList);
      string read = null;
      while ((read = s.ReadLine()) != null)
      {
        ignoredWindows.Add(read);
      }
      ignoredWindows.Sort();
    }

		private bool isIgnoredWindow(String title)
		{
			foreach (String sysw in ignoredWindows)
			{
				if (sysw.ToUpper().Equals(title.ToUpper()))
				{
					return true;
				}
			}
			return false;
		}

		private void switchToSelectedWindow(string title, string id)
		{
			IntPtr newWin = CoreDll.FindWindow(null, title);
			if (newWin != IntPtr.Zero)
			{
        CoreDll.SetWindowPos(newWin, (System.IntPtr)CoreDll.HWND_TOP, 0, 0, 0, 0,
                CoreDll.SWP_NOMOVE | CoreDll.SWP_NOSIZE);
        CoreDll.ShowWindow(newWin, CoreDll.SW_SHOW);
        CoreDll.EnableWindow(newWin, true);
      }
		}

    private static StreamWriter startDebug()
    {
      return File.CreateText(HomeDirectory + "\\" + debugOut);
    }

    private static void stopDebug()
    {
			debugStream.Flush();
      debugStream.Close();
    }

    public static void WriteDebug(string msg)
    {
      debugStream.WriteLine(DateTime.Now + ": " + msg);
      debugStream.Flush();
    }
	}

  /// <summary>
  /// Class to keep track of details of each window.
  /// </summary>
	public class WinDetails : IComparable
	{
		public string Title;
		public IntPtr Id;

		public WinDetails(IntPtr id, string title)
		{
			this.Title = title;
			this.Id = id;
		}

		public override string ToString()		
		{
			return this.Id.ToString() + ": " + this.Title + ":";
		}

		public int CompareTo(object obj)
		{
			WinDetails Compare = (WinDetails)obj;
			return this.Title.CompareTo(Compare.Title);
		}
	}
}
