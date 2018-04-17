using System;
using System.Collections;
using System.Windows.Forms;
using Frontera;

namespace Frontera
{
  public class Program
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main(string[] args)
    {
      AccessButton ab = new AccessButton();
      MainForm frontera = new MainForm(ab);
      ab.setFrontera(frontera);
      frontera.Show();
      Application.Run(ab);
    }
  }
}