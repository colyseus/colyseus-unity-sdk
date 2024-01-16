
using System.Threading.Tasks;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

public class AuthTest
{
	class User
	{
		public string email;
		public string name;
		public int errorServerIsStringButClientIsInt;
		public int anonymousId;
		public bool anonymous;
	}

	private Colyseus.ColyseusClient client;

	[SetUp]
	public void Init()
	{
		// Initialize without a token on each test 
		client = new Colyseus.ColyseusClient("http://localhost:2567");
		client.Auth.Token = null;

		// Make sure auth token is not cached
		PlayerPrefs.DeleteAll();
	}

	[TearDown]
	public void Dispose()
	{
	}

	[Test]
	public async Task GetUserData()
	{
		var uniqueEmail = $"endel{Time.time.ToString().Replace(".", "")}@colyseus.io";

		string tokenFromCallback = "OnChange was not called";
		string emailFromCallback = "";
		string nameFromCallback = "";

		_ = client.Auth.OnChange((Colyseus.AuthData<User> authData) =>
		{
			tokenFromCallback = authData.token;
			if (authData.user != null)
			{
				emailFromCallback = authData.user.email;
				nameFromCallback = authData.user.name;
			}
		});

		//
		// Registering for the first time
		//
		Colyseus.IAuthData response = null;
		try
		{
			response = await client.Auth.RegisterWithEmailAndPassword(uniqueEmail, "123456");
		}
		catch (Colyseus.HttpException e)
		{
			Assert.Fail(e.Message + $"({e.StatusCode})");
		}

		Assert.AreEqual(tokenFromCallback, client.Auth.Token);

		var user = await client.Auth.GetUserData<User>();
		Assert.AreEqual(user.email, emailFromCallback);
		Assert.AreEqual(user.name, nameFromCallback);
	}

	[Test]
	public async Task RegisterWithEmailAndPassword()
	{
		var uniqueEmail = $"endel{Time.time.ToString().Replace(".", "")}@colyseus.io";

		string tokenFromCallback = "OnChange was not called";
		string emailFromCallback = "";
		string nameFromCallback = "";

		_ = client.Auth.OnChange((Colyseus.AuthData<User> authData) =>
		{
			tokenFromCallback = authData.token;
			if (authData.user != null)
			{
				emailFromCallback = authData.user.email;
				nameFromCallback = authData.user.name;
			}
		});

		//
		// Registering for the first time
		//
		Colyseus.IAuthData response = null;
		try
		{
			response = await client.Auth.RegisterWithEmailAndPassword(uniqueEmail, "123456");
		} catch (Colyseus.HttpException e)
		{
			Assert.Fail(e.Message + $"({e.StatusCode})");
		}
		Assert.True(response.Token.Length > 0);
		Assert.AreEqual(response.Token, tokenFromCallback);
		Assert.AreEqual(tokenFromCallback, client.Auth.Token);

		object responseEmail = "";
		object responseName = "";
		response.RawUser.TryGetValue("email", out responseEmail);
		response.RawUser.TryGetValue("name", out responseName);
		Assert.AreEqual(responseEmail, emailFromCallback);
		Assert.AreEqual(responseName, nameFromCallback);

		//
		// Trying to register a second time
		//
		try
		{
			await client.Auth.RegisterWithEmailAndPassword(uniqueEmail, "123456");
			Assert.Fail("Should have failed.");
		}
		catch (Colyseus.HttpException e)
		{
			Assert.AreEqual(e.Message, "email_already_in_use");
		}

		try
		{
			await client.Auth.SignInWithEmailAndPassword(uniqueEmail, "wrong");
			Assert.Fail("Signing in with wrong password should've failed!");
		}
		catch (Colyseus.HttpException e)
		{
			Assert.AreEqual(e.Message, "invalid_credentials");
		}

		var loginResponse = await client.Auth.SignInWithEmailAndPassword<User>(uniqueEmail, "123456");
		Assert.AreEqual(loginResponse.user.email, uniqueEmail);
		Assert.AreEqual(loginResponse.user.name, uniqueEmail.Split("@")[0]);
	}

	[Test]
	public async Task SignInAnonymously()
	{
		string tokenFromCallback = "OnChange was not called";
		bool anonymousFromCallback = false;
		int anonymousIdFromCallback = 0;

		_ = client.Auth.OnChange((Colyseus.AuthData<User> authData) =>
		{
			tokenFromCallback = authData.token;
			if (authData.user != null)
			{
				anonymousFromCallback = authData.user.anonymous;
				anonymousIdFromCallback = authData.user.anonymousId;
			}
		});

		//
		// Registering for the first time
		//
		Colyseus.IAuthData response = null;
		try
		{
			response = await client.Auth.SignInAnonymously();
		}
		catch (Colyseus.HttpException e)
		{
			Assert.Fail(e.Message + $"({e.StatusCode})");
		}
		Assert.True(response.Token.Length > 0);
		Assert.AreEqual(response.Token, tokenFromCallback);
		Assert.AreEqual(tokenFromCallback, client.Auth.Token);

		object responseAnonymous = false;
		object responseAnonymousId = -1;
		response.RawUser.TryGetValue("anonymous", out responseAnonymous);
		response.RawUser.TryGetValue("anonymousId", out responseAnonymousId);
		Assert.AreEqual(responseAnonymous, anonymousFromCallback);
		Assert.AreEqual(responseAnonymousId, anonymousIdFromCallback);
	}

	[Test]
	public async Task SignOut()
	{
		string tokenFromCallback = "OnChange was not called";
		bool anonymousFromCallback = false;
		int anonymousIdFromCallback = 0;
		int onChangeCallCount = 0;
		int onChangeCallWithNullUser = 0;

		_ = client.Auth.OnChange((Colyseus.AuthData<User> authData) =>
		{
			onChangeCallCount++;
			tokenFromCallback = authData.token;
			if (authData.user != null)
			{
				anonymousFromCallback = authData.user.anonymous;
				anonymousIdFromCallback = authData.user.anonymousId;
			}
			else
			{
				onChangeCallWithNullUser++;
			}
		});

		await client.Auth.SignInAnonymously();
		Assert.AreEqual(tokenFromCallback, client.Auth.Token);
		Assert.AreEqual(1, onChangeCallWithNullUser);
		Assert.AreEqual(2, onChangeCallCount);

		client.Auth.SignOut();
		Assert.AreEqual(null, client.Auth.Token);
		Assert.AreEqual(3, onChangeCallCount);
		Assert.AreEqual(2, onChangeCallWithNullUser);
	}

}