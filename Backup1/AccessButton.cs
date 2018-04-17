using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices;

namespace Frontera
{
	/// <summary>
	/// Summary description for AccessButton.
	/// </summary>
	public class AccessButton : System.Windows.Forms.Form
	{
		private static int NOWINS_DELAY = 2000;
    private static int POPUP_DELAY = 1000;
    private static int MAX_CONTEXT_MOVE = 20;

		public static string WindowTitle = "Frontera - AccessButton";
		private MainForm frontera;
		public Timer WinsTimer;
		private ContextMenu contextMenu;

		private Size AccessSize = new Size(35, 35);
		private Size ShrunkenSize = new Size(10, 10);
		private Point AccessLocation = new Point(160, 2);

		private static bool dragInProgress = false;
		int MouseDownX = 0;
		private System.Windows.Forms.PictureBox accessBox;
		int MouseDownY = 0;

		public AccessButton()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			accessBox.Image = new Bitmap(MainForm.HomeDirectory + "\\button.gif");
      
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
/*			MenuItem shrink = new MenuItem();
			shrink.Text = "Shrink";
			shrink.Click +=new EventHandler(shrink_Click);
			contextMenu.MenuItems.Add(shrink);
			sep = new MenuItem();
			sep.Text = "-";
			contextMenu.MenuItems.Add(sep);
			MenuItem expand = new MenuItem();
			expand.Text = "Expand";
			expand.Click +=new EventHandler(expand_Click);
			contextMenu.MenuItems.Add(expand);
			sep = new MenuItem();
			sep.Text = "-";
			contextMenu.MenuItems.Add(sep);
*/			MenuItem end = new MenuItem();
			end.Text = "Exit";
			end.Click +=new EventHandler(end_Click);
			contextMenu.MenuItems.Add(end);
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
			accessBox.Size = AccessSize;

			this.Location = AccessLocation;
			MouseDownX = this.Location.X;
			MouseDownY = this.Location.Y;
			this.Size = this.accessBox.Size;
			this.Text = WindowTitle;
			
			WinsTimer = new Timer();
			WinsTimer.Interval = NOWINS_DELAY;
			WinsTimer.Tick += new EventHandler(winsTimer_Tick);
			WinsTimer.Enabled = true;
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
      resetInterval();
      //OpenNETCF.Windows.Forms.SendKeys.Send("%{TAB}");
      showFrontera();
    }

		void reset_Click(object sender, EventArgs e)
		{
			ConfirmDialog cd = new ConfirmDialog("Are you sure?");
			cd.ShowDialog();
			if (cd.isConfirmed())
			{
				CoreDll.ResetUnit();
			}
		}

		public void setFrontera(MainForm frontera)
		{
			this.frontera = frontera;
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
			// 
			// accessBox
			// 
			this.accessBox.Location = new System.Drawing.Point(0, 0);
			this.accessBox.Size = new System.Drawing.Size(50, 50);
			// 
			// AccessButton
			// 
			this.ClientSize = new System.Drawing.Size(50, 50);
			this.ControlBox = false;
			this.Controls.Add(this.accessBox);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Text = "AccessButton";
		}

		public void showFrontera()
		{
			if (frontera != null)
			{
				frontera.DoRefresh();
				frontera.Show();
#if DOTNET2
        frontera.Activate();
#endif
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
			// "Button" held down for required interval, so show context menu.
			if (!dragInProgress)
			{
				if (e.Button == MouseButtons.Right ||
					e.Button == MouseButtons.Left && isDownIntervalElapsed())
        {
					contextMenu.Show(accessBox, new Point(0, 0));
				}
					// Normal click on "button".
				else if (!isDownIntervalElapsed() && e.Button == MouseButtons.Left)
				{
					accessBox_Click(sender, e);
				}
				else
				{
					resetInterval();
				}
			}
			else
			{
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
			ArrayList wins = frontera.GetAllWindows();
			if (wins.Count == 0)
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

		private void shrink_Click(object sender, EventArgs e)
		{
			accessBox.Size = ShrunkenSize;
			this.Size = accessBox.Size;
		}

		private void expand_Click(object sender, EventArgs e)
		{
			accessBox.Size = AccessSize;
			this.Size = accessBox.Size;
		}
	}
}
