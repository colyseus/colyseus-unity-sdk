using System;
using Gtk;

using Colyseus;
using Newtonsoft.Json.Linq;
using JsonDiffPatch;

public partial class MainWindow: Gtk.Window
{

	Client colyseus;
	Room room;

	public MainWindow () : base (Gtk.WindowType.Toplevel)
	{
		Console.WriteLine("Main window!");
		Build ();

//		this.colyseus = new Client ("ws://localhost:2657");
		this.colyseus = new Client("ws://colyseus-react-example.herokuapp.com");
		this.room = this.colyseus.Join ("chat");
		this.room.OnUpdate += Room_OnUpdate;
	}

	void Room_OnUpdate (object sender, RoomUpdateEventArgs e)
	{
		if (e.patches == null) {
			JArray messages = (JArray) e.state ["messages"];
			for (int i = 0; i < messages.Count; i++) {
				textview1.Buffer.Text += messages[i] + "\n";
			}
		} else {
			for (int i = 0; i < e.patches.Operations.Count; i++) {
				AddOperation operation = (AddOperation) e.patches.Operations [i];
				textview1.Buffer.Text += operation.Value + "\n";
			}
		}
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		this.colyseus.Close ();
		Application.Quit ();
		a.RetVal = true;
	}

	protected void Submit (object sender, EventArgs e)
	{
		this.room.Send (entry1.Text);
		entry1.Text = "";
	}

	protected void OnKeyPress (object o, KeyPressEventArgs args)
	{
		if (args.Event.Key.ToString() == "Return") {
			this.Submit (o, args);
		}
	}
}
