using System;
using Gtk;

using Colyseus;

public partial class MainWindow: Gtk.Window
{

	Client colyseus;
	Room room;
	
	public MainWindow () : base (Gtk.WindowType.Toplevel)
	{
		Console.WriteLine("Main window!");
		Build ();

		this.colyseus = new Client ("ws://localhost:2657");
		this.room = this.colyseus.Join ("chat");
		this.room.OnUpdate += Room_OnUpdate;
	}

	void Room_OnUpdate (object sender, RoomUpdateEventArgs e)
	{
//		Console.WriteLine (e.data);
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		this.colyseus.Close ();
		Application.Quit ();
		a.RetVal = true;
	}
}
