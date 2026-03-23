using NUnit.Framework;

namespace Colyseus.Tests
{

	[TestFixture]
	public class HTTPTest
	{

		[Test]
		public void UnsecureRootPathWithPortTest()
		{
			var settings = Colyseus.Settings.Create();
			settings.colyseusServerAddress = "localhost";
			settings.colyseusServerPort = "2567";
			settings.useSecureProtocol = false;

			var request = new Colyseus.HTTP(settings);
			Assert.AreEqual("http://localhost:2567/", request.GetRequestURL("").ToString());
		}

		[Test]
		public void UnsecureChildPathWithPortTest()
		{
			var settings = Colyseus.Settings.Create();
			settings.colyseusServerAddress = "localhost/path";
			settings.colyseusServerPort = "2567";
			settings.useSecureProtocol = false;

			var request = new Colyseus.HTTP(settings);
			Assert.AreEqual("http://localhost:2567/path/", request.GetRequestURL("").ToString());
		}

		[Test]
		public void UnsecureChildPathNoPortTest()
		{
			var settings = Colyseus.Settings.Create();
			settings.colyseusServerAddress = "localhost/path";
			settings.colyseusServerPort = "80";
			settings.useSecureProtocol = false;

			var request = new Colyseus.HTTP(settings);
			Assert.AreEqual("http://localhost/path/", request.GetRequestURL("").ToString());
		}


		[Test]
		public void SecureChildPathNoPortTest()
		{
			var settings = Colyseus.Settings.Create();
			settings.colyseusServerAddress = "localhost/path";
			settings.colyseusServerPort = "443";
			settings.useSecureProtocol = true;

			var request = new Colyseus.HTTP(settings);
			Assert.AreEqual("https://localhost/path/", request.GetRequestURL("").ToString());
		}

		[Test]
		public void SecureChildPathWithPortTest()
		{
			var settings = Colyseus.Settings.Create();
			settings.colyseusServerAddress = "localhost";
			settings.colyseusServerPort = "8080";
			settings.useSecureProtocol = true;

			var request = new Colyseus.HTTP(settings);
			Assert.AreEqual("https://localhost:8080/", request.GetRequestURL("").ToString());
		}

	}

}
