using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using OpenNETCF.WindowsCE;
using System.IO;
using OpenNETCF.AppSettings;
using System.IO.Ports;

namespace Frontera
{
	/// <summary>
	/// Summary description for AccessButton.
	/// </summary>
	public class AccessButton : Form
	{
    private static int POPUP_DELAY = 2000;

		public static string WindowTitle = "Frntra";
		private MainForm frontera = null;
		public Timer WinsTimer;
		private ContextMenu contextMenu;

		private Size DefaultSize = new Size(46,47);
		private Point DefaultLocation = new Point(160, 2);

    private static bool dragInProgress = false;
		int MouseDownX = 0;
		private System.Windows.Forms.PictureBox accessBox;
//    private TransparentLabel accessLabel;
    int MouseDownY = 0;

		public AccessButton()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

      MenuItem sep;
  		contextMenu = new ContextMenu();
			MenuItem move = new MenuItem();
			move.Text = "Move";
			move.Click += new EventHandler(move_Click);
			contextMenu.MenuItems.Add(move);
			sep = new MenuItem();
			sep.Text = "-";
			contextMenu.MenuItems.Add(sep);
			MenuItem show = new MenuItem();
			show.Text = "Show";
			show.Click += new EventHandler(show_Click);
			contextMenu.MenuItems.Add(show);
			sep = new MenuItem();
			sep.Text = "-";
			contextMenu.MenuItems.Add(sep);
			MenuItem hide = new MenuItem();
			hide.Text = "Hide";
			hide.Click +=new EventHandler(hide_Click);
			contextMenu.MenuItems.Add(hide);
			sep = new MenuItem();
			sep.Text = "-";
			contextMenu.MenuItems.Add(sep);
			MenuItem end = new MenuItem();
			end.Text = "Exit";
			end.Click +=new EventHandler(end_Click);
			contextMenu.MenuItems.Add(end);
      sep = new MenuItem();
      sep.Text = "-";
      contextMenu.MenuItems.Add(sep);
      sep = new MenuItem();
      sep.Text = "-";
      contextMenu.MenuItems.Add(sep);
      MenuItem sleep = new MenuItem();
      sleep.Text = "Sleep";
      sleep.Click += new EventHandler(sleep_Click);
      contextMenu.MenuItems.Add(sleep);
      sep = new MenuItem();
      sep.Text = "-";
      contextMenu.MenuItems.Add(sep);
      MenuItem reset = new MenuItem();
      reset.Text = "Reset";
      reset.Click += new EventHandler(reset_Click);
      contextMenu.MenuItems.Add(reset);
			
			accessBox.MouseDown += new MouseEventHandler(accessButton_MouseDown);
      accessBox.MouseUp += new MouseEventHandler(accessButton_MouseUp);
      accessBox.MouseMove += new MouseEventHandler(accessButton_MouseMove);
      //accessBox.Click += new EventHandler(accessBox_Click);

			MouseDownX = this.Location.X;
			MouseDownY = this.Location.Y;
			this.Text = WindowTitle;
		}

    private Rectangle srcRect = new Rectangle();
    private Rectangle destRect = new Rectangle();

    private void scaleBitmap(Bitmap dest, Bitmap src)
    {
      destRect.Width = dest.Width;
      destRect.Height = dest.Height;
      using (Graphics g = Graphics.FromImage(dest))
      {
        Brush b = new SolidBrush(Color.Black);
        g.FillRectangle(b, destRect);
        srcRect.Width = src.Width;
        srcRect.Height = src.Height;
        g.DrawImage(src, destRect, srcRect, System.Drawing.GraphicsUnit.Pixel);
      }
    }

    void sleep_Click(object sender, EventArgs e)
    {
      OpenNETCF.WindowsCE.PowerManagement.Suspend();
    }

    /// <summary>
    /// Single-click shows Frontera window
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void accessBox_Click(object sender, EventArgs e)
    {
      if (!dragInProgress)
      {
        if (frontera.Cycle && !isDownIntervalElapsed())
        {
          frontera.NextWindow();
        }
        else
        {
          resetInterval();
          Redraw();
          showFrontera();
        }
      }
    }

    public void Redraw()
    {
      Bitmap img = new Bitmap(MainForm.GetResourceStream("window.bmp"));
      Size imgSize = getAccessSize();
      accessBox.Size = imgSize;
      Bitmap resized = new Bitmap(imgSize.Width, imgSize.Height);
      scaleBitmap(resized, img);
      accessBox.Image = resized;
      this.Size = accessBox.Size;
      this.Location = getAccessLocation();
      this.BringToFront();
      this.Refresh();
    }

		void reset_Click(object sender, EventArgs e)
		{
			if (MainForm.ConfirmSelection("Are you sure?"))
			{
        OpenNETCF.WindowsCE.PowerManagement.SoftReset();
			}
		}

		public void setFrontera(MainForm frontera)
		{
			this.frontera = frontera;
      
      this.Redraw();

      if (frontera.BlankTimeout > 0)
      {
        WinsTimer = new Timer();
        WinsTimer.Interval = frontera.BlankTimeout;
        WinsTimer.Tick += new EventHandler(winsTimer_Tick);
      }
    }

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			base.Dispose(disposing);
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.accessBox = new System.Windows.Forms.PictureBox();
            this.SuspendLayout();
            // 
            // accessBox
            // 
            this.accessBox.Location = new System.Drawing.Point(0, 0);
            this.accessBox.Name = "accessBox";
            this.accessBox.Size = new System.Drawing.Size(50, 50);
            // 
            // AccessButton
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.ClientSize = new System.Drawing.Size(50, 50);
            this.ControlBox = false;
            //accessLabel = new TransparentLabel();
            //accessLabel.Text = "";
            //accessLabel.Bounds = new Rectangle(1, 1, 48, 48);
//            this.Controls.Add(accessLabel);
            this.Controls.Add(this.accessBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AccessButton";
            this.Text = "AccessButton";
            this.ResumeLayout(false);
		}

		public void showFrontera()
		{
			if (frontera != null)
			{
        frontera.ShowFrontera();
        if (WinsTimer != null)
        {
          WinsTimer.Enabled = false;
        }
			}
		}

		DateTime lastDown = DateTime.MinValue;

		void accessButton_MouseDown(object sender, MouseEventArgs e)
		{
      if (!dragInProgress)
      {
        lastDown = DateTime.Now;
      }
      else
      {
        MouseDownX = this.Location.X;
        MouseDownY = this.Location.Y;
      }
      return;
		}

		void accessButton_MouseUp(object sender, MouseEventArgs e)
		{
			// "Button" held down for required interval, so show frontera.
      if (!dragInProgress)
      {
        if (frontera.Cycle && !isDownIntervalElapsed())
        {
            frontera.NextWindow();
        }
        else
        {
            resetInterval();
            Redraw();
            showFrontera();
        }
      }
      else
      {
        // Save current location
        saveAccessLocation();
        resetInterval();
      }

			return;
		}

		private bool isDownIntervalElapsed()
		{
      return lastDown != DateTime.MinValue &&
         DateTime.Now - lastDown >= TimeSpan.FromMilliseconds(POPUP_DELAY);
		}

		void accessButton_MouseMove(object sender, MouseEventArgs e)
		{
      if (dragInProgress)
      {
        Point temp = new Point();
        temp.X = this.Location.X + e.X - this.Width / 2;
        temp.Y = this.Location.Y + e.Y - this.Height / 2;
        this.Location = temp;
      }
			return;
		}
		
		private void resetInterval()
		{
      dragInProgress = false;
      lastDown = DateTime.MinValue;
      MouseDownX = this.Location.X;
      MouseDownY = this.Location.Y;
		}

		private void winsTimer_Tick(object sender, EventArgs e)
		{
			ArrayList wins = frontera.GetAllWindows(false);
			if (wins.Count == 0 && frontera.BlankTimeout > 0)
			{
				if (!frontera.Visible)
				{
					showFrontera();
				}
				else
				{
					frontera.Show();
				}
			}
		}

		private void move_Click(object sender, EventArgs e)
		{
      StartDrag();
		}

    public void StartDrag()
    {
      if (!dragInProgress)
      {
        dragInProgress = true;
        this.MouseDownX = this.Location.X;
        this.MouseDownY = this.Location.Y;
      }
    }

		private void hide_Click(object sender, EventArgs e)
		{
			resetInterval();
			frontera.Hide();
      if (WinsTimer != null)
      {
        WinsTimer.Enabled = true;
      }
    }

		private void end_Click(object sender, EventArgs e)
		{
			resetInterval();
			Application.Exit();
		}

		private void show_Click(object sender, EventArgs e)
		{
			resetInterval();
			showFrontera();
		}

    private void saveAccessLocation()
    {
      if (frontera != null)
      {
        Setting x = frontera.ConfigSettings.GetSetting("SelectorLocation", "X");
        x.Value = this.Location.X;
        Setting y = frontera.ConfigSettings.GetSetting("SelectorLocation", "Y");
        y.Value = this.Location.Y;
        frontera.ConfigSettings.Save();
      }
    }

    private Point getAccessLocation()
    {
      if (frontera != null)
      {
        Setting x = frontera.ConfigSettings.GetSetting("SelectorLocation", "X");
        Setting y = frontera.ConfigSettings.GetSetting("SelectorLocation", "Y");
        return new Point(int.Parse(x.Value.ToString()), int.Parse(y.Value.ToString()));
      }
      else
      {
        return DefaultLocation;
      }
    }

    private Size getAccessSize()
    {
      if (frontera != null)
      {
        Setting x = frontera.ConfigSettings.GetSetting("SelectorSize", "X");
        Setting y = frontera.ConfigSettings.GetSetting("SelectorSize", "Y");
        MainForm.WriteDebug("Size=" + x.Value.ToString() + ", " + y.Value.ToString());
        return new Size(int.Parse(x.Value.ToString()), int.Parse(y.Value.ToString()));
      }
      else
      {
        return DefaultSize;
      }
    }
  }
}
