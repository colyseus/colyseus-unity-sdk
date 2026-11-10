using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Colyseus.Tests
{
	/// <summary>
	/// A failed matchmaking request carries the server's ErrorCode in its JSON body. With
	/// <c>matchMaker.controller.standardHttpStatus</c> the HTTP status is a standard one
	/// (404, 401, 500…) instead, so the code must come from the body (colyseus#999).
	/// </summary>
	[TestFixture]
	public class MatchMakeErrorCodeTest
	{
		private HttpListener listener;
		private int port;
		private int respondStatus;
		private string respondBody;
		private IHttpClient previousHttpClient;

		[OneTimeSetUp]
		public void StartServer()
		{
			var probe = new TcpListener(IPAddress.Loopback, 0);
			probe.Start();
			port = ((IPEndPoint)probe.LocalEndpoint).Port;
			probe.Stop();

			listener = new HttpListener();
			listener.Prefixes.Add($"http://127.0.0.1:{port}/");
			listener.Start();
			_ = Serve();
		}

		[OneTimeTearDown]
		public void StopServer()
		{
			listener.Close();
		}

		[SetUp]
		public void UseDefaultHttpClient()
		{
			previousHttpClient = ColyseusContext.HttpClient;
			ColyseusContext.HttpClient = new DefaultHttpClient();
		}

		[TearDown]
		public void RestoreHttpClient()
		{
			ColyseusContext.HttpClient = previousHttpClient;
		}

		[TestCase(404, "{\"code\":520,\"error\":\"provided room name \\\"nope\\\" not defined\"}", 520)]
		[TestCase(401, "{\"code\":525,\"error\":\"onAuth failed\"}", 525)]
		[TestCase(500, "{\"code\":4000,\"error\":\"banned\"}", 4000)]
		[TestCase(520, "{\"code\":520,\"error\":\"provided room name \\\"nope\\\" not defined\"}", 520)]
		[TestCase(502, "<html>Bad Gateway</html>", 502)]
		[TestCase(500, "{\"code\":\"4000\",\"error\":\"banned\"}", 500)]
		public void MatchMakeErrorReportsBodyCode(int status, string body, int expected)
		{
			Respond(status, body);
			var client = new Client($"ws://127.0.0.1:{port}");

			var e = Assert.ThrowsAsync<HttpException>(() => client.JoinOrCreate("nope"));

			Assert.AreEqual(expected, e.StatusCode);
			Assert.AreEqual(body, e.Body);
		}

		[Test]
		public void UnityShapedErrorReportsBodyCode()
		{
			// UnityHttpClient puts the body's "error" in the message and the raw body in Body
			const string body = "{\"code\":520,\"error\":\"provided room name \\\"nope\\\" not defined\"}";
			ColyseusContext.HttpClient = new ThrowingHttpClient(new HttpException(404, "provided room name \"nope\" not defined", body));
			var client = new Client($"ws://127.0.0.1:{port}");

			var e = Assert.ThrowsAsync<HttpException>(() => client.JoinOrCreate("nope"));

			Assert.AreEqual(520, e.StatusCode);
			Assert.AreEqual("provided room name \"nope\" not defined", e.Message);
		}

		[Test]
		public void GeneralHttpKeepsStatus()
		{
			const string body = "{\"code\":520,\"error\":\"not found\"}";
			Respond(404, body);
			var client = new Client($"ws://127.0.0.1:{port}");

			var e = Assert.ThrowsAsync<HttpException>(() => client.Http.Post("matchmake/joinOrCreate/nope"));

			Assert.AreEqual(404, e.StatusCode);
			Assert.AreEqual(body, e.Body);
		}

		private void Respond(int status, string body)
		{
			respondStatus = status;
			respondBody = body;
		}

		private async Task Serve()
		{
			while (listener.IsListening)
			{
				HttpListenerContext ctx;
				try { ctx = await listener.GetContextAsync(); }
				catch (Exception) { return; } // listener closed

				var bytes = Encoding.UTF8.GetBytes(respondBody);
				ctx.Response.StatusCode = respondStatus;
				ctx.Response.ContentType = "application/json";
				ctx.Response.ContentLength64 = bytes.Length;
				await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
				ctx.Response.Close();
			}
		}

		private class ThrowingHttpClient : IHttpClient
		{
			private readonly Exception error;

			public ThrowingHttpClient(Exception error)
			{
				this.error = error;
			}

			public Task<string> Request(string method, string url, byte[] body, Dictionary<string, string> headers)
			{
				return Task.FromException<string>(error);
			}
		}
	}
}
