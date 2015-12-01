using System;
using Gtk;

using Colyseus;

public partial class MainWindow: Gtk.Window
{

	Client colyseus;
	Room room;
	
	public MainWindow () : base (Gtk.WindowType.Toplevel)
	{
		this.colyseus = new Client ("ws://localhost:2657");
		Console.WriteLine("Main window!");
		Build ();
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		this.colyseus.Close ();
		Application.Quit ();
		a.RetVal = true;
	}
}
