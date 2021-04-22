using LucidSightTools;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Colyseus;

public class ExampleMoveController : ExampleNetworkedEntityView
{
	private CharacterController controller;
	private Vector3 playerVelocity;
	private bool groundedPlayer;
	private float playerSpeed = 2.0f;
	private float jumpHeight = 1.0f;
	private float gravityValue = -9.81f;

	protected override void Start()
	{
		autoInitEntity = false;
		base.Start();
		controller = gameObject.AddComponent<CharacterController>();

		StartCoroutine("WaitForConnect");
	}

	IEnumerator WaitForConnect()
    {
        if (ExampleManager.Instance.CurrentUser != null && !IsMine) yield break;

		while(!ExampleManager.Instance.IsInRoom)
		{
			yield return 0;
		}
		LSLog.LogImportant("HAS JOINED ROOM - CREATING ENTITY");
        ExampleManager.CreateNetworkedEntityWithTransform(new Vector3(0f, 0f, 0f), Quaternion.identity, new Dictionary<string, object>() { ["prefab"] = "VMEViewPrefab" }, this, (entity) => {
			LSLog.LogImportant($"Network Entity Ready {entity.id}");
		});
	}

	public override void OnEntityRemoved()
	{
		base.OnEntityRemoved();
		LSLog.LogImportant("REMOVING ENTITY", LSLog.LogColor.lime);
		Destroy(this.gameObject);
	}

	protected override void Update()
	{
		base.Update();

		if (!HasInit || !IsMine) return;

		groundedPlayer = controller.isGrounded;
		if (groundedPlayer && playerVelocity.y < 0)
		{
			playerVelocity.y = 0f;
		}

		Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		controller.Move(move * Time.deltaTime * playerSpeed);

		if (move != Vector3.zero)
		{
			gameObject.transform.forward = move;
		}

		// Changes the height position of the player..
		if (Input.GetButtonDown("Jump") && groundedPlayer)
		{
			playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
		}

		playerVelocity.y += gravityValue * Time.deltaTime;
		controller.Move(playerVelocity * Time.deltaTime);

		if (Input.GetKeyDown(KeyCode.F))
		{
			LSLog.Log("fire weapon key");
            ExampleManager.RFC(state.id, "FireWeaponRFC", new object[] { new ExampleVector3Obj(transform.forward), "aWeapon" });
		}

	}

	public void FireWeaponRFC(object v3, string weaponType)
	{
		ExampleVector3Obj aVec = ParseRFCObject<ExampleVector3Obj>(v3);

		GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		bullet.transform.position = new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z);
		bullet.transform.LookAt(new Vector3((float)aVec.x, (float)aVec.y, (float)aVec.z) * 100f);
		bullet.AddComponent<SphereCollider>();
		var rb = bullet.AddComponent<Rigidbody>();
		rb.AddForce(bullet.transform.forward * 15f, ForceMode.Impulse);
	}

}
