using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Frontera
{
  public class ConfirmDialog : Form
  {
    private bool confirmed = false;

    public ConfirmDialog(string msg)
    {
      InitializeComponent();
      this.Location = new Point(
        (MainForm.ScreenWidth - this.Width) / 2, 
        (MainForm.ScreenHeight - this.Height) / 2 - 35);
      //button1.Size = new Size(MainForm.ButtonWidth, MainForm.ButtonHeight);
      //button2.Size = new Size(MainForm.ButtonWidth, MainForm.ButtonHeight);
      
      labelMessage.Text = msg;
    }

    private void button1_Click(object sender, EventArgs e)
    {
      confirmed = true;
      this.Dispose();
    }

    public bool isConfirmed()
    {
      return confirmed;
    }

    private void button2_Click(object sender, EventArgs e)
    {
      this.Dispose();
    }

	  /// <summary>
	  /// Required designer variable.
	  /// </summary>
	  private System.ComponentModel.IContainer components = null;

	  /// <summary>
	  /// Clean up any resources being used.
	  /// </summary>
	  /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
	  protected override void Dispose(bool disposing)
	  {
		  if (disposing && (components != null))
		  {
			  components.Dispose();
		  }
		  base.Dispose(disposing);
	  }

	  /// <summary>
	  /// Required method for Designer support - do not modify
	  /// the contents of this method with the code editor.
	  /// </summary>
	  private void InitializeComponent()
	  {
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.labelMessage = new System.Windows.Forms.Label();
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(24, 64);
			this.button1.Size = new System.Drawing.Size(72, 30);
			this.button1.Text = "OK";
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(120, 64);
			this.button2.Size = new System.Drawing.Size(72, 30);
			this.button2.Text = "Cancel";
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// labelMessage
			// 
			this.labelMessage.Location = new System.Drawing.Point(25, 13);
			this.labelMessage.Size = new System.Drawing.Size(163, 23);
			this.labelMessage.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// ConfirmDialog
			// 
			this.ClientSize = new System.Drawing.Size(209, 105);
			this.Controls.Add(this.labelMessage);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Text = "Confirmation";

		}

	  private System.Windows.Forms.Button button1;
	  private System.Windows.Forms.Button button2;
	  private System.Windows.Forms.Label labelMessage;
  }
}