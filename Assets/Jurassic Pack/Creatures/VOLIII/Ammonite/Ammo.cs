using UnityEngine;

public class Ammo : Creature
{
	public Transform Root,Body,Tentacles,Right0,Right1,Right2,Right3,Right4,Right5,Right6,Right7,Right8,
	Left0,Left1,Left2,Left3,Left4,Left5,Left6,Left7,Left8;
  public AudioClip Waterflush,Hit_jaw,Hit_head,Hit_tail,Smallstep,Smallsplash,Ammo1,Ammo2,Ammo3;
	//*************************************************************************************************************************************************
	//Play sound
	void OnCollisionStay(Collision col)
	{
		int rndPainsnd=Random.Range(0, 3); AudioClip painSnd=null;
		switch (rndPainsnd) { case 0: painSnd=Ammo1; break; case 1: painSnd=Ammo2; break; case 2: painSnd=Ammo3; break; }
		ManageCollision(col, source, painSnd, Hit_jaw, Hit_head, Hit_tail);
	}
	void PlaySound(string name, int time)
	{
		if(time==currframe && lastframe!=currframe)
		{
			switch (name)
			{
			case "Swim": source[1].pitch=Random.Range(0.5f, 0.75f); 
				if(isOnWater && isOnGround) source[1].PlayOneShot(Smallsplash, 0.1f);
				else if(isOnGround && !isInWater) source[1].PlayOneShot(Smallstep, 0.1f);
				else if(isOnWater) source[1].PlayOneShot(Waterflush, 0.1f);
				lastframe=currframe; break;
			case "Atk":int rnd=Random.Range(0, 2); source[0].pitch=Random.Range(0.9f, 1.1f);
				if(rnd==0) source[0].PlayOneShot(Ammo1, 0.1f);
				else source[0].PlayOneShot(Ammo2, 0.1f);
				lastframe=currframe; break;
			case "Die": source[0].pitch=Random.Range(0.8f, 1.0f); source[0].PlayOneShot(Ammo3, 0.1f);
				lastframe=currframe; isDead=true; break;
			}
		}
	}

  //*************************************************************************************************************************************************
  // Add forces to the Rigidbody
  void FixedUpdate ()
	{
		StatusUpdate(); if(!isActive | animSpeed==0.0f) { body.Sleep(); return; }
		Vector3 dir=-Root.up.normalized; onJump=false; onAttack=false; isOnLevitation=false; isConstrained=false; onReset=false;

    if(useAI&&health!=0)// CPU
    {
      AICore(1, 2, 3, 0, 4, 0, 5); if(behavior.EndsWith("Hunt")|behavior.EndsWith("Food")|behavior.Equals("Battle")) onInvert=true; else onInvert=false;
    } else if(health!=0) { GetUserInputs(1, 2, 3, 0, 4, 0, 5); }// Human
    else { anm.SetBool("Attack", false); anm.SetInteger("Move", 0); anm.SetInteger("Idle", -1); } //Dead

    //Set Y position
    if(isInWater)
		{
      body.drag=1; body.angularDrag=1;
      if(health!=0&&!OnAnm.IsName("Ammo|ToHide")&&!OnAnm.IsName("Ammo|ToHide-"))
			{
        transform.rotation=Quaternion.Lerp(transform.rotation, normAng, ang_T);
			  pitch=Mathf.Lerp(pitch, anm.GetFloat("Pitch")*(onInvert?-90f:90f), ang_T);
			  if(anm.GetInteger("Move").Equals(-1)) Move(onInvert?dir:-dir, 25);
        else if(anm.GetInteger("Move").Equals(1)) Move(onInvert?-dir:dir, 25);
				else if(anm.GetInteger("Move").Equals(10)) Move(onInvert?-Head.right.normalized:Head.right.normalized, 25);
				else if(anm.GetInteger("Move").Equals(-10)) Move(onInvert?Head.right.normalized:-Head.right.normalized, 25);
				else if(!anm.GetInteger("Move").Equals(0)) Move(onInvert?-dir:dir, 50);
        else Move(Vector3.zero);
        isOnLevitation=true;
			}
      anm.SetBool("OnGround", false);
			if(isOnWater) ApplyGravity();
		}
		else if(isOnGround) { body.drag=4; body.angularDrag=4; anm.SetBool("OnGround", true); ApplyYPos(); }
    else
    {
      if(health!=0) { Move(Vector3.zero); pitch=Mathf.Lerp(pitch, anm.GetFloat("Pitch")*90f, ang_T); }
      anm.SetBool("OnGround", false); onJump=true; body.drag=0.5f; body.angularDrag=0.5f; ApplyGravity();
    }


		//Stopped
		if(OnAnm.IsName("Ammo|Die") | OnAnm.IsName("Ammo|DieGround"))
		{
			onReset=true;
      if(!isDead) PlaySound("Die", 2);
		}

		//Forward
		else if(OnAnm.IsName("Ammo|Swim"))
		{
			PlaySound("Swim", 5);
		}

		//Running
		else if(OnAnm.IsName("Ammo|SwimFast"))
		{
			PlaySound("Swim", 5); PlaySound("Swim", 10);
		}
		
		//Backward/Strafe
		else if(OnAnm.IsName("Ammo|Swim-"))
		{
			PlaySound("Swim", 5);
		}

		//Attack
		else if(OnAnm.IsName("Ammo|Atk"))
		{
			onAttack=true;
			PlaySound("Atk", 5); PlaySound("Swim", 10);
		}

		//Impulse
		else if(OnAnm.IsName("Ammo|IdleC"))
		{
      if(isInWater&&OnAnm.normalizedTime<0.4) { PlaySound("Flush", 2); Move(onInvert?-dir:dir, 60); }
			PlaySound("Atk", 5); PlaySound("Swim", 10);
		}

		//On Ground
		else if(OnAnm.IsName("Ammo|OnGround"))
		{
			onReset=true; Move(transform.forward, 40);
			PlaySound("Swim", 5); PlaySound("Swim", 10);
		}
		else if(OnAnm.IsName("Ammo|Eat")) { PlaySound("Atk", 1); }
		else if(OnAnm.IsName("Ammo|ToHide") | OnAnm.IsName("Ammo|ToHide-") ) onReset=true;
		else if(OnAnm.IsName("Ammo|Die-")) { PlaySound("Atk", 1);  isDead=false; }

    RotateBone(IkType.None, 60f, 30f, false); if(onInvert) spineY*=-1;
	}

  //*************************************************************************************************************************************************
	// Bone rotation
	void LateUpdate()
	{
    if(!isActive) return; headPos=Head.GetChild(0).GetChild(0).position;
    Root.RotateAround(transform.position, Vector3.up, reverse=Mathf.Lerp(reverse, onInvert?180f : 0.0f, ang_T) );
		Root.rotation*= Quaternion.Euler(Mathf.Clamp(-pitch, -90, 90), roll*2f, 0);
		Body.rotation*= Quaternion.Euler(0, 0, -spineX);
		Tentacles.rotation*= Quaternion.Euler(spineY*4.0f, 0, -spineX*2.0f);
		Right0.rotation*= Quaternion.Euler(0, -spineY, -spineX);
		Right1.rotation*= Quaternion.Euler(0, -spineY, -spineX);
		Right2.rotation*= Quaternion.Euler(0, -spineY, -spineX);
		Right3.rotation*= Quaternion.Euler(0, -spineY, -spineX);
		Right4.rotation*= Quaternion.Euler(0, -spineY, -spineX);
		Right5.rotation*= Quaternion.Euler(0, -spineY, -spineX);
		Right6.rotation*= Quaternion.Euler(0, -spineY, -spineX);
		Right7.rotation*= Quaternion.Euler(0, -spineY, -spineX);
		Right8.rotation*= Quaternion.Euler(0, -spineY, -spineX);
		Left0.rotation*= Quaternion.Euler(spineY, 0, -spineX);
		Left1.rotation*= Quaternion.Euler(spineY, 0, -spineX);
		Left2.rotation*= Quaternion.Euler(spineY, 0, -spineX);
		Left3.rotation*= Quaternion.Euler(spineY, 0, -spineX);
		Left4.rotation*= Quaternion.Euler(spineY, 0, -spineX);
		Left5.rotation*= Quaternion.Euler(spineY, 0, -spineX);
		Left6.rotation*= Quaternion.Euler(spineY, 0, -spineX);
		Left7.rotation*= Quaternion.Euler(spineY, 0, -spineX);
		Left8.rotation*= Quaternion.Euler(spineY, 0, -spineX);

		//Check for ground layer
		GetGroundPos(IkType.None);
	}
}










