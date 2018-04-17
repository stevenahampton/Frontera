using System;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using Frontera;
using OpenNETCF.WindowsCE;
using OpenNETCF.AppSettings;
using OpenNETCF.ToolHelp;
using OpenNETCF.IO;
using OpenNETCF.Win32;
using Microsoft.Win32;
using System.IO.Ports;
using OpenNETCF.Media;

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
    private Button systemButton;
    private Button refreshButton;

    private AccessButton accessButton;

    private enum FronteraScreen
    {
      Windows,
      Processes,
      Programs,
      System
    };
    private FronteraScreen currentScreen;

		public static string HomeDirectory = System.IO.Path.GetDirectoryName(
			System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

    private static bool debug = false;
    private bool useStartList = false;
    public bool Cycle = false;
    public int BlankTimeout = 0;

    private string moveImage = "move.bmp";
    private string hideImage = "hide.bmp";
    private string exitImage = "exit.bmp";
    private string muteImage = "mute.bmp";
    private string unmuteImage = "unmute.bmp";
    private string sleepImage = "sleep.bmp";
    private string brightnessImage = "brightness.jpg";
    private string softresetImage = "softreset.bmp";
    private string hardresetImage = "hardreset.bmp";
    private string windowImage = "window.bmp";
    public static string launchImage = "launch.bmp";
    private string processImage = "process.bmp";

    private string configFile = "frontera.xml";
    public SettingsFile ConfigSettings = null;
    private static string debugOut   = "debug.txt";

    private static StreamWriter debugStream = startDebug();
    private DeviceStatusMonitor Dsm;

    private ArrayList processes;
    private ArrayList windows;
    private ArrayList programs;

		private Window currWin = null;

		private static Rectangle screen = Screen.PrimaryScreen.WorkingArea;
		public static int ScreenWidth = screen.Width;
    public static int ScreenHeight = screen.Height;
		private static int X = screen.X;
		private static int Y = screen.X;
		private static int numButtons = 5;
    
    private static int VOL_INCR = (65536 / 5);
    
    private static int buttonSpacing = 5;
    public static int ButtonWidth = 
      (ScreenWidth - (numButtons + 1) * buttonSpacing) / numButtons;
    public static int ButtonHeight = 32;
    private static int buttonYPos = Y + ScreenHeight - ButtonHeight - 26;

    public static string LauncherTitle = "Frontera - Launcher";
    public static string WindowsTitle = "Frontera - Windows";
    private static string processesTitle = "Frontera - Processes";
    private static string systemTitle = "Frontera - Utilities";

		private System.Windows.Forms.MainMenu mm;
		private System.Windows.Forms.MenuItem frontera;
		private System.Windows.Forms.MenuItem reset;
		private System.Windows.Forms.MenuItem end;

    private ArrayList drives = new ArrayList();

    //private static FileInfo gpsOut;
    //private static SerialPort gps;
    //private static StreamWriter gpsWr;

    /// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

    public MainForm(AccessButton accessButton)
		{
      loadConfig();

      launchCERemote();

      launchStartList();

      //WriteDebug("GUID=" + OpenNETCF.WindowsCE.DeviceManagement.GetDeviceGuid());
      //WriteDebug("ID=" + OpenNETCF.WindowsCE.DeviceManagement.GetDeviceID());
      foreach (string drive in OpenNETCF.Environment2.GetLogicalDrives())
      {
        if (!drive.Equals("\\"))
        {
          drives.Add(drive);
        }
        //WriteDebug("Drive=" + drive);
      }
      //foreach (string port in System.IO.Ports.SerialPort.GetPortNames())
      //{
      //  WriteDebug("Port="+port.ToString());
      //}

      this.InitializeComponent();
			this.InitializeExtras();
      
			this.accessButton = accessButton;
			accessButton.TopMost = true;

      windows = GetAllWindows(false);
      processes = getAllProcesses();
      programs = getAllPrograms();
			// Default view is windows
      currentScreen = FronteraScreen.Windows;
      DoRefresh();

      Dsm = new DeviceStatusMonitor(Guid.Empty, true);
      Dsm.DeviceNotification += new DeviceNotificationEventHandler(dsm_DeviceNotification);
      Dsm.StartStatusMonitoring();

      hideCERemote();
    }

    private void loadConfig()
    {
      if (ConfigSettings == null)
      {
        ConfigSettings = new SettingsFile(HomeDirectory + "\\" + configFile, true);
      }
      SettingGroup frnt = ConfigSettings.Groups["Frontera"];
      debug = frnt.Settings["Debug"].Value.ToString().ToLower().Equals("true");
      useStartList = frnt.Settings["UseStartList"].Value.ToString().ToLower().Equals("true");
      Cycle = frnt.Settings["Cycle"].Value.ToString().ToLower().Equals("true");
      BlankTimeout = int.Parse(frnt.Settings["BlankTimeout"].Value.ToString());
      //gpsOut  = new FileInfo(HomeDirectory + "\\gps.txt");
      //StreamWriter gpsWr = gpsOut.CreateText();
      //gps = new SerialPort("COM7", 115200, Parity.None);
      //try
      //{
      //  gps.Open();
      //}
      //catch (IOException e)
      //{
      //  WriteDebug(e.Message + ", " + e.StackTrace);
      //}
      //gps.DataReceived += new SerialDataReceivedEventHandler(gps_DataReceived);
    }

    private void launchCERemote()
    {
      FileInfo cerdisp = new FileInfo(HomeDirectory + "\\cerdisp2.exe");
      if (cerdisp.Exists)
      {
        WriteDebug("cerdisp found");
        FileInfo wincer = new FileInfo("\\Windows\\cerdisp2.exe");
        if (!wincer.Exists)
        {
          WriteDebug("No win\\cerdisp");
          cerdisp.CopyTo(wincer.FullName);
          wincer = new FileInfo("\\Windows\\cerdisp2.exe");
        }
        if (debug && wincer.Exists)
        {
          WriteDebug("Checking processes");
          bool running = false;
          foreach (ProcessEntry proc in ProcessEntry.GetProcesses())
          {
            WriteDebug("proc: " + proc.ExeFile + ", wincer: " + wincer.Name);
            if (proc.ExeFile.Equals(wincer.Name))
            {
              WriteDebug("Already running");
              running = true;
            }
          }
          if (!running)
          {
            WriteDebug("Launching cerdisp: " + wincer.FullName);
            launch(wincer.FullName, "");
          }
        }
      }
    }

    private void hideCERemote()
    {
      IntPtr cerwin = CoreDll.FindWindow(null, "CE Remote Display");
      if (cerwin != IntPtr.Zero)
      {
        CoreDll.ShowWindow(cerwin, CoreDll.SW_HIDE);
      }
      cerwin = CoreDll.FindWindow(null, "About CERDisp");
      if (cerwin != IntPtr.Zero)
      {
        CoreDll.ShowWindow(cerwin, CoreDll.SW_HIDE);
      }
    }

    private void launchStartList()
    {
      if (useStartList)
      {
        SettingGroup startList = ConfigSettings.Groups["StartList"];
        foreach (Setting exe in startList.Settings)
        {
          if (exe.Name.ToLower().EndsWith(".executable"))
          {
            string arg = exe.Name.Substring(0,
              exe.Name.Length - ".executable".Length) + ".Arguments";
            //WriteDebug("exe=" + exe.Name + ", arg=" + arg);
            Setting args = startList.Settings[arg];
            launch(exe.Value.ToString(),
              args == null || args.Value == null ? "" : args.Value.ToString());
          }
        }
      }
    }

    //static void gps_DataReceived(object sender, SerialDataReceivedEventArgs e)
    //{
    //  WriteDebug("got gps data");
    //  gpsWr.Write(gps.ReadExisting());
    //  gpsWr.Flush();
    //}

    /// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
      Dsm.StopStatusMonitoring();
      ConfigSettings.Save();
      
      //gps.DataReceived -= new SerialDataReceivedEventHandler(gps_DataReceived);
      //WriteDebug("before close gps");
      //gpsWr.Close();
      //WriteDebug("after close gps");
      stopDebug();
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
			this.processButton = new System.Windows.Forms.Button();
			this.programButton = new System.Windows.Forms.Button();
			this.systemButton = new System.Windows.Forms.Button();
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
			this.Controls.Add(this.systemButton);
			this.Controls.Add(this.refreshButton);
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
			Stream ico = new StreamReader(
				System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(
						"Frontera.Resources.frontera.ico")).BaseStream;
			this.Icon = new Icon(ico, 16, 16);
			ico.Close();

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
			// systemButton
			//
			xpos += ButtonWidth + buttonSpacing;
			systemButton.Text = "Utils";
			//systemButton.Image = new Bitmap(homeDir + "\\system.gif");
			systemButton.Location = new Point(xpos, buttonYPos);
			systemButton.Size = new Size(ButtonWidth, ButtonHeight);
			//Hook up into click event
			systemButton.Click += new System.EventHandler(this.systemButton_Click);
			//
			// refreshButton
			//
			xpos += ButtonWidth + buttonSpacing;
			//refreshButton.Image = new Bitmap(homeDir + "\\refresh.gif");
      refreshButton.Text = "Refresh";
      refreshButton.Location = new Point(xpos, buttonYPos);
			refreshButton.Size = new Size(ButtonWidth, ButtonHeight);
			//Hook up into click event
			refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
			
			//this.listView.BackColor = System.Drawing.SystemColors.Desktop;
			//this.ClientSize = new System.Drawing.Size(638, 455);
			//this.ControlBox = false;
			//this.FormBorderStyle = FormBorderStyle.None;
		}

    private delegate void RefreshDelegate();

    void dsm_DeviceNotification(object sender, DeviceNotificationArgs e)
    {
      RefreshDelegate refreshDelegate = new RefreshDelegate(refreshLauncher);
      if (e.DeviceName != null && drives.Contains(e.DeviceName.Substring(1)) &&
        currentScreen == FronteraScreen.Programs)
      {
        Invoke(refreshDelegate);
      }
    }

    void reset_Click(object sender, EventArgs e)
    {
      OpenNETCF.WindowsCE.PowerManagement.SoftReset();
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
      currentScreen = FronteraScreen.Windows;
      DoRefresh();
    }

    private void processButton_Click(object sender, System.EventArgs e)
    {
      currentScreen = FronteraScreen.Processes;
      DoRefresh();
    }

    private void programButton_Click(object sender, System.EventArgs e)
    {
      currentScreen = FronteraScreen.Programs;
      DoRefresh();
    }

    private void refreshButton_Click(object sender, System.EventArgs e)
		{
      DoRefresh();
		}

    private void systemButton_Click(object sender, System.EventArgs e)
    {
      currentScreen = FronteraScreen.System;
      DoRefresh();
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
      switch (currentScreen)
      {
        case FronteraScreen.Windows:
          switchToSelectedWindows();
          break;
        case FronteraScreen.Processes:
          if (ConfirmSelection("End Selected Process?"))
          {
            endSelectedProcesses();
            DoRefresh();
          }
          else
          {
            this.Show();
          }
          break;
        case FronteraScreen.Programs:
          launchSelectedPrograms();
          break;
        case FronteraScreen.System:
          performSelectedUtils();
          break;
      }
    }

    private void placeAtBack()
    {
      this.Hide();
      if (accessButton.WinsTimer != null)
      {
        accessButton.WinsTimer.Enabled = true;
      }
      accessButton.Redraw();
    }

    public void DoRefresh()
    {
      listView.Items.Clear();
      updateButtonsAndTitle();
      switch (currentScreen)
      {
        case FronteraScreen.Windows:
          refreshWindows();
          break;
        case FronteraScreen.Processes:
          refreshProcesses();
          break;
        case FronteraScreen.Programs:
          refreshLauncher();
          break;
        case FronteraScreen.System:
          refreshSystem();
          break;
      }
    }

    private void updateButtonsAndTitle()
    {
      accessButton.Redraw();
      windowButton.Enabled = true;
      processButton.Enabled = true;
      programButton.Enabled = true;
      systemButton.Enabled = true;
      switch (currentScreen)
      {
        case FronteraScreen.Windows:
          this.Text = WindowsTitle;
          windowButton.Enabled = false;
          break;
        case FronteraScreen.Processes:
          this.Text = processesTitle;
          processButton.Enabled = false;
          break;
        case FronteraScreen.Programs:
          this.Text = LauncherTitle;
          programButton.Enabled = false;
          break;
        case FronteraScreen.System:
          this.Text = systemTitle;
          systemButton.Enabled = false;
          break;
      }
    }

    public static Stream GetResourceStream(string resname)
    {
      return new StreamReader(
        System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(
            "Frontera.Resources." + resname)).BaseStream;
    }

    private void refreshLauncher()
    {
      listView.View = View.LargeIcon;

      ImageList imageList = new ImageList();
      // Default icon
      Bitmap bm = new Bitmap(GetResourceStream(launchImage));
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
      listView.Columns.Add("Source",
        -2,
        HorizontalAlignment.Left);

      programs = getAllPrograms();

      listView.Items.Clear();
      // Add the rest from list file
      int i = 1;
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
          lvi.ImageIndex = 0;
        }
        if (parts.Length >= 4 && "Card".Equals(parts[3]))
        {
          lvi.ForeColor = System.Drawing.Color.Blue;
        }
        listView.Items.Add(lvi);
      }
      listView.LargeImageList = imageList;
      listView.SmallImageList = imageList;
    }

    private void refreshSystem()
    {
      listView.View = View.LargeIcon;
      ImageList imageList = new ImageList();

      // Add columns
      listView.Columns.Clear();
      listView.Columns.Add("Title",
        -2,
        HorizontalAlignment.Left);
      listView.Columns.Add("Program",
        -2,
        HorizontalAlignment.Left);

      // Reset icon
      Bitmap bm;
      bm = new Bitmap(GetResourceStream(moveImage));
      imageList.Images.Add(bm);
      bm = new Bitmap(GetResourceStream(hideImage));
      imageList.Images.Add(bm);
      bm = new Bitmap(GetResourceStream(exitImage));
      imageList.Images.Add(bm);
      bm = new Bitmap(GetResourceStream(muteImage));
      imageList.Images.Add(bm);
      bm = new Bitmap(GetResourceStream(sleepImage));
      imageList.Images.Add(bm);
      bm = new Bitmap(GetResourceStream(softresetImage));
      imageList.Images.Add(bm);
      bm = new Bitmap(GetResourceStream(hardresetImage));
      imageList.Images.Add(bm);

      // 7 = unmute
      bm = new Bitmap(GetResourceStream(unmuteImage));
      imageList.Images.Add(bm);

      listView.Items.Clear();
      int i = 0;
      // Add exit option first
      ListViewItem utilItem = new ListViewItem(new string[] { "Move Selector", "", "" });
      utilItem.ImageIndex = i++;
      listView.Items.Add(utilItem);
      utilItem = new ListViewItem(new string[] { "Hide Frontera", "", "" });
      utilItem.ImageIndex = i++;
      listView.Items.Add(utilItem);
      utilItem = new ListViewItem(new string[] { "Exit Frontera", "", "" });
      utilItem.ImageIndex = i++;
      listView.Items.Add(utilItem);
      if (OpenNETCF.Media.SystemSound.GetVolume() == 0 && initialVolume != 0)
      {
        utilItem = new ListViewItem(new string[] { "Unmute", "", "" });
        utilItem.ImageIndex = 7;
        i++;
      }
      else
      {
        utilItem = new ListViewItem(new string[] { "Mute", "", "" });
        utilItem.ImageIndex = i++;
      }
      listView.Items.Add(utilItem);
      utilItem = new ListViewItem(new string[] { "Sleep", "", "" });
      utilItem.ImageIndex = i++;
      listView.Items.Add(utilItem);
      utilItem = new ListViewItem(new string[] { "Soft Reset", "", "" });
      utilItem.ImageIndex = i++;
      listView.Items.Add(utilItem);
      utilItem = new ListViewItem(new string[] { "Hard Reset", "", "" });
      utilItem.ImageIndex = i++;
      listView.Items.Add(utilItem);
      listView.LargeImageList = imageList;
      listView.SmallImageList = imageList;
    }

    private ArrayList getProgramsFromConfig(SettingsFile sf, string source)
    {
      ArrayList programs = new ArrayList();
      SettingGroup progs = sf.Groups["Programs"];
      for (int i = 0; i < progs.Settings.Count; i++)
      {
        Setting title = progs.Settings[i];
        if (title.Name.EndsWith(".Title"))
        {
          string name = title.Name.Substring(0, title.Name.LastIndexOf(".Title"));
          Setting exe = progs.Settings[name + ".Executable"];
          Setting args = progs.Settings[name + ".Arguments"];
          programs.Add(title.Value + "," + (exe == null ? "" : exe.Value) + ", " + (args == null ? "" : args.Value) + "," + source);
        }
      }
      return programs;
    }

    private ArrayList getAllProcesses()
    {
      ArrayList processes = new ArrayList();
      ProcessEntry[] procs = ProcessEntry.GetProcesses();
      processes.AddRange(procs);

      return processes;
    }

    private ArrayList getAllPrograms()
    {
      ArrayList programs = getProgramsFromConfig(ConfigSettings, "Resident");
      // Get launch list from frontera.xml in HomeDirectory and under Card
      // Frontera directory.
      foreach (string drive in drives)
      {
        string cardlaunchDir = "\\" + drive + "\\Frontera";
        string cardlaunch = cardlaunchDir + "\\" + configFile;
        FileInfo cc = new FileInfo(cardlaunch);
        if (cc.Exists && !cardlaunchDir.ToUpper().Equals(HomeDirectory.ToUpper()))
        {
          SettingsFile cardConfig = new SettingsFile(cardlaunch, false);
          programs.AddRange(getProgramsFromConfig(cardConfig, "Card"));
        }
      }
      programs.Sort();
      return programs;
    }

    private void refreshProcesses()
    {
      listView.View = View.List;
      ImageList imageList = new ImageList();
      Bitmap bm = new Bitmap(GetResourceStream(processImage));
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

      processes = getAllProcesses();

      listView.Items.Clear();
      int i = 1;
      foreach (ProcessEntry proc in processes)
      {
        WriteDebug("Procs:" + proc.ToString() + "," + proc.ExeFile + "," + proc.ProcessID.ToString());
				string[] procDetails = new string[] { proc.ToString(), proc.ProcessID.ToString(), proc.BaseAddress.ToString(), proc.ThreadCount.ToString() };
        ListViewItem lvi = new ListViewItem(procDetails);
        lvi.Text = proc.ToString();

        Icon icon = CoreDll.extractIconFromExe(proc.ExeFile, false);
        if (icon == null)
        {
          string fullexe = findExeInPrograms(proc.ExeFile);
          if (fullexe != null)
          {
            icon = CoreDll.extractIconFromExe(fullexe, false);
          }
        }
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

    private string findExeInPrograms(string exe)
    {
      string fullexe = null;
      foreach (string prog in programs)
      {
        string[] parts = prog.Split(new char[] { ',' });
        if (parts.Length > 1 && parts[1].ToUpper().EndsWith(exe.ToUpper()))
        {
          if (fullexe == null)
          {
            fullexe = parts[1];
          }
          else
          {
            // More than one with this exe so we don't know which is the right one
            fullexe = null;
            break;
          }
        }
      }
      // Try \windows if not found
      if (fullexe == null)
      {
        FileInfo inwin = new FileInfo("\\Windows\\" + exe);
        if (inwin.Exists)
        {
          fullexe = inwin.FullName;
        }
      }
      return fullexe;
    }

		public void NextWindow()
		{
      accessButton.Redraw();
			ArrayList allwins = GetAllWindows(false);
			int i = 0;
			if (currWin != null)
			{
				WriteDebug("CurrWindow-> "+currWin.Caption);
				for (; i < allwins.Count; i++)
				{
					if (((Window)allwins[i]).Caption.Equals(currWin.Caption))
					{
						break;
					}
				}
				i++;
			}
			if (i >= allwins.Count)
			{
				i = 0;
			}
			if (i < allwins.Count)
			{
				currWin = (Window)allwins[i];
        if (i == 0)
        {
          DoRefresh();
        }
        CoreDll.ShowWindow(currWin.Handle, CoreDll.SW_SHOW);
        CoreDll.SetForegroundWindow(currWin.Handle);
      }
		}

    private void refreshWindows()
    {
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
      // Default window icon
      Bitmap bm = new Bitmap(GetResourceStream(windowImage));
      imageList.Images.Add(bm);

      windows = GetAllWindows(false);
      processes = getAllProcesses();

      listView.Items.Clear();
      int i = 1;
      foreach (Window win in windows)
      {
        ListViewItem lvi = new ListViewItem(new string[] { win.Caption, win.Handle.ToString() });
        // Try to find the matching process for this window and if found, use its icon
        uint pid = 0;
        ProcessEntry proc = null;
        Icon icon = null;
        CoreDll.GetWindowThreadProcessId(win.Handle, out pid);
        if (pid != 0)
        {
          foreach (ProcessEntry pr in processes)
          {
            if (pr.ProcessID == pid)
            {
              proc = pr;
              break;
            }
          }
        }
        if (proc != null)
        {
          icon = CoreDll.extractIconFromExe(proc.ExeFile, false);
          if (icon == null)
          {
            string fullexe = findExeInPrograms(proc.ExeFile);
            if (fullexe != null)
            {
              icon = CoreDll.extractIconFromExe(fullexe, false);
            }
          }
        }
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
      int startToolsIdx = i;
      while (listView.Items.Count < 10)
      {
        listView.Items.Add(new ListViewItem(new string[] {"", "0"}));
      }

      imageList.Images.Add(CoreDll.extractIconFromExe("\\Windows\\control.exe", false));
      ListViewItem lvi2 = new ListViewItem(new string[] { "Control Panel", "0" });
      lvi2.ImageIndex = imageList.Images.Count - 1;
      listView.Items.Add(lvi2);

      bm = new Bitmap(GetResourceStream(unmuteImage));
      imageList.Images.Add(bm);
      int vol = (SystemSound.GetVolume() / VOL_INCR);
      if (vol < 0)
      {
        vol = 0;
        SystemSound.SetVolume(0);
      }
      else if (vol > 5)
      {
        vol = 5 * VOL_INCR;
        SystemSound.SetVolume(vol);
      }
      lvi2 = new ListViewItem(
        new string[] { "Volume [" + vol / VOL_INCR + "]", "0" });
      lvi2.ImageIndex = imageList.Images.Count - 1;
      listView.Items.Add(lvi2);

      bm = new Bitmap(GetResourceStream(brightnessImage));
      imageList.Images.Add(bm);
      RegistryKey cp = Registry.CurrentUser.OpenSubKey("ControlPanel");
      foreach (string subkey in cp.GetSubKeyNames())
      {
        WriteDebug("subkey=" + subkey);
      }
      cp = cp.OpenSubKey("Backlight");
      //cp = cp.OpenSubKey("BacklightCurrentLevel");
      //RegistryValueKind rk = cp.GetValueKind("BacklightCurrentLevel");
      //Object bc = cp.GetValue("BacklightCurrentLevel");
      lvi2 = new ListViewItem(
        new string[] { "Display [" + cp.GetValue("BacklightCurrentLevel") + "]", "0" });
      lvi2.ImageIndex = imageList.Images.Count - 1;
      listView.Items.Add(lvi2);

      bm = new Bitmap(GetResourceStream(sleepImage));
      imageList.Images.Add(bm);
      lvi2 = new ListViewItem(new string[] { "Sleep", "0" });
      lvi2.ImageIndex = imageList.Images.Count - 1;
      listView.Items.Add(lvi2);

      bm = new Bitmap(GetResourceStream(exitImage));
      imageList.Images.Add(bm);
      lvi2 = new ListViewItem(new string[] { "Exit Frontera", "0" });
      lvi2.ImageIndex = imageList.Images.Count - 1;
      listView.Items.Add(lvi2);

      listView.LargeImageList = imageList;
      listView.SmallImageList = imageList;
    }

    public static bool ConfirmSelection(string msg)
    {
      ConfirmDialog cd = new ConfirmDialog(msg);
      cd.ShowDialog();
      return cd.isConfirmed();
    }

    private void launchSelectedPrograms()
    {
      for (int i = 0; i < listView.SelectedIndices.Count; i++)
      {
        int idx = listView.SelectedIndices[i];
        ListViewItem lvi = listView.Items[idx];
        launch(lvi.SubItems[1].Text, lvi.SubItems[2].Text);
      }
      placeAtBack();
    }

    private int initialVolume = OpenNETCF.Media.SystemSound.GetVolume();

    private void performSelectedUtils()
    {
      for (int i = 0; i < listView.SelectedIndices.Count; i++)
      {
        switch (listView.SelectedIndices[i])
        {
          case 0: // Move
            accessButton.StartDrag();
            break;
          case 1: // Hide
            this.Hide();
            break;
          case 2: // Exit
            Application.Exit();
            break;
          case 3: // Mute/Unmute
            ListViewItem mute = listView.Items[3];
            if (OpenNETCF.Media.SystemSound.GetVolume() != 0)
            {
              OpenNETCF.Media.SystemSound.SetVolume(0);
              mute.ImageIndex = 7;
              mute.Text = "Unmute";
            }
            else
            {
              OpenNETCF.Media.SystemSound.SetVolume(initialVolume);
              mute.ImageIndex = 3;
              mute.Text = "Mute";
            }
            break;
          case 4: // Sleep
            OpenNETCF.WindowsCE.PowerManagement.Suspend();
            break;
          case 5: // Soft Reset
            if (ConfirmSelection("Are you sure?"))
            {
              OpenNETCF.WindowsCE.PowerManagement.SoftReset();
            }
            else
            {
              this.Show();
            }
            break;
          case 6: // Hard Reset
            if (ConfirmSelection("Are you sure?"))
            {
              OpenNETCF.WindowsCE.PowerManagement.HardReset();
            }
            else
            {
              this.Show();
            }
            break;
        }
      }
    }

    private void endSelectedProcesses()
    {
      for (int i = 0; i < listView.SelectedIndices.Count; i++)
      {
        ProcessEntry proc = (ProcessEntry)processes[listView.SelectedIndices[i]];
        proc.Kill();
      }
    }

    public void ShowFrontera()
    {
      currentScreen = FronteraScreen.Windows;
      DoRefresh();
      this.Show();
      this.Activate();
    }
      
    /// <summary>
    /// Switch to the selected window(title)
    /// </summary>
		private void switchToSelectedWindows()
		{
      bool hideFrontera = true;
			for (int i = 0; i < listView.SelectedIndices.Count; i++)
			{
        ListViewItem lvi = listView.Items[listView.SelectedIndices[i]];
        WriteDebug("title=" + lvi.SubItems[0].Text + ",[1]=[" + lvi.SubItems[1].Text + "]");
        if (lvi.SubItems[0].Text.Length > 0 && !lvi.SubItems[1].Text.Equals("0"))
        {
          switchToSelectedWindow(lvi.SubItems[0].Text, lvi.SubItems[1].Text);
        }
        else
        {
          hideFrontera = false;
          if (lvi.SubItems[0].Text.Equals("Exit Frontera"))
          {
            Application.Exit();
          }
          else if (lvi.SubItems[0].Text.Equals("Sleep"))
          {
            OpenNETCF.WindowsCE.PowerManagement.Suspend();
          }
          else if (lvi.SubItems[0].Text.Equals("Control Panel"))
          {
            launch("\\Windows\\control.exe", "");
          }
          else if (lvi.SubItems[0].Text.StartsWith("Display"))
          {
            RegistryKey cp = Registry.CurrentUser.OpenSubKey("ControlPanel");
            cp = cp.OpenSubKey("Backlight", true);
            int bright = int.Parse(cp.GetValue("BacklightCurrentLevel").ToString());
            int newbright = bright + 1;
            if (newbright > 5)
            {
              newbright = 1;
            }
            cp.SetValue("BacklightCurrentLevel", newbright, RegistryValueKind.DWord);
            cp.Close();
            IntPtr hBackLightEvent = CoreDll.CreateEvent(IntPtr.Zero, false, true, "BackLightChangeEvent");
            WriteDebug("hBLCE=" + hBackLightEvent.ToString());
            if (hBackLightEvent != IntPtr.Zero)
            {
              WriteDebug("SetEvent=" + CoreDll.SetEvent(hBackLightEvent).ToString());
              WriteDebug("ResetEvent=" + CoreDll.ResetEvent(hBackLightEvent).ToString());
              CoreDll.CloseHandle(hBackLightEvent);
            }
            //hBackLightEvent = CoreDll.CreateEvent(IntPtr.Zero, false, true, "SDKBackLightChangeEvent");
            //WriteDebug("SDKhBLCE=" + hBackLightEvent.ToString());
            //if (hBackLightEvent != IntPtr.Zero)
            //{
            //  WriteDebug("SetEvent=" + CoreDll.SetEvent(hBackLightEvent).ToString());
            //  WriteDebug("ResetEvent=" + CoreDll.ResetEvent(hBackLightEvent).ToString());
            //  CoreDll.CloseHandle(hBackLightEvent);
            //}
            lvi.SubItems[0].Text = "Display [" + newbright + "]";
            
          }
          else if (lvi.SubItems[0].Text.StartsWith("Volume"))
          {
            int vol = OpenNETCF.Media.SystemSound.GetVolume() / VOL_INCR;
            int newvol = vol + 1;
            if (newvol > 5)
            {
              newvol = 0;
            }
            OpenNETCF.Media.SystemSound.SetVolume(newvol * VOL_INCR);
            lvi.SubItems[0].Text = "Volume [" + newvol + "]";
          }
        }
      }
      if (hideFrontera)
      {
        placeAtBack();
      }
    }

    public static void launch(string exe, string args)
    {
      CoreDll.ProcessInfo pi = new CoreDll.ProcessInfo();
      CoreDll.CreateProcess(exe, null, pi, false);
    }

		public ArrayList GetAllWindows(Boolean inclFrnt)
		{
      ArrayList wins = new ArrayList();
      // if Frontera should be included, add it to the start
      if (inclFrnt)
      {
        IntPtr hWnd = CoreDll.FindWindow(null, LauncherTitle);
        Window win = new Window();
        win.Handle = hWnd;
        win.Caption = LauncherTitle;
        if (hWnd == IntPtr.Zero)
        {
          hWnd = CoreDll.FindWindow(null, systemTitle);
          win.Handle = hWnd;
          win.Caption = systemTitle;
        }
        if (hWnd == IntPtr.Zero)
        {
          hWnd = CoreDll.FindWindow(null, WindowsTitle);
          win.Handle = hWnd;
          win.Caption = WindowsTitle;
        }
        if (hWnd == IntPtr.Zero)
        {
          hWnd = CoreDll.FindWindow(null, processesTitle);
          win.Handle = hWnd;
          win.Caption = processesTitle;
        }
        if (hWnd != IntPtr.Zero)
        {
          wins.Add(win);
        }
      }
      wins.AddRange(CoreDll.EnumerateTopWindows(ConfigSettings.Groups["IgnoreList"]));
      wins.Sort();
			return wins;
		}

		private void switchToSelectedWindow(string title, string id)
		{
			IntPtr newWin = (IntPtr)int.Parse(id);
			if (newWin != IntPtr.Zero)
			{
        CoreDll.SetWindowPos(newWin, (System.IntPtr)CoreDll.HWND_TOP, 0, 0, 0, 0,
                CoreDll.SWP_NOMOVE | CoreDll.SWP_NOSIZE);
        CoreDll.ShowWindow(newWin, CoreDll.SW_SHOW);
        CoreDll.SetForegroundWindow(newWin);
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
      if (!debug)
      {
        new FileInfo(HomeDirectory + "\\" + debugOut).Delete();
      }
    }

    public static void WriteDebug(string msg)
    {
      if (debug)
      {
        debugStream.WriteLine(DateTime.Now + ": " + msg);
        debugStream.Flush();
      }
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
