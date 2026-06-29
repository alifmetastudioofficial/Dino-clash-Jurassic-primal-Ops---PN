using UnityEngine;

public class Arche : Creature
{
	public Transform Root,Neck0,Neck1,Neck2;
  public AudioClip Waterflush, Hit_jaw,Hit_head,Hit_tail,Slip,Bite,Swallow,Medsplash,Arche1,Arche2,Arche3;

	//*************************************************************************************************************************************************
	//Play sound
	void OnCollisionStay(Collision col)
	{
		int rndPainsnd=Random.Range(0, 3); AudioClip painSnd=null;
		switch (rndPainsnd) { case 0: painSnd=Arche1; break; case 1: painSnd=Arche2; break; case 2: painSnd=Arche3; break; }
		ManageCollision(col, source, painSnd, Hit_jaw, Hit_head, Hit_tail);
	}
	void PlaySound(string name, int time)
	{
		if(time==currframe && lastframe!=currframe)
		{
			switch (name)
			{
			case "Swim": source[1].pitch=Random.Range(0.75f, 1.0f);
				if(isOnWater && isOnGround) source[1].PlayOneShot(Medsplash, 0.25f);
				else if(isOnGround && !isInWater) source[1].PlayOneShot(Slip, 0.25f);
				else if(isOnWater) source[1].PlayOneShot(Waterflush, 0.1f);
				lastframe=currframe; break;
			case "Bite": source[1].pitch=Random.Range(0.25f, 0.5f); source[1].PlayOneShot(Bite, 0.1f);
				lastframe=currframe; break;
			case "Growl": int rnd=Random.Range(0, 2); source[0].pitch=Random.Range(1.0f, 1.5f);
				if(rnd==0) source[0].PlayOneShot(Arche1, 0.1f);
				else source[0].PlayOneShot(Arche2, 0.1f);
				lastframe=currframe; break;
			case "Food": source[0].pitch=Random.Range(1.0f, 1.5f); source[0].PlayOneShot(Swallow, 0.1f);
				lastframe=currframe; break;
			case "Die": source[0].pitch=Random.Range(1.0f, 1.5f);
				source[0].PlayOneShot(Arche3, 0.5f);
				lastframe=currframe; isDead=true; break;
			}
		}
	}

	//*************************************************************************************************************************************************
	// Add forces to the Rigidbody
	void FixedUpdate ()
	{
		StatusUpdate(); if(!isActive | animSpeed==0.0f) { body.Sleep(); return; }
		Vector3 dir=-Root.up; onJump=false; onAttack=false; isOnLevitation=false; isConstrained=false; onReset=false;

		if(useAI && health!=0) { AICore(1, 2, 4, 0, 3, 0, 4); }// CPU
		else if(health!=0) { GetUserInputs(1, 2, 4, 0, 3, 0, 4); }// Human
		else { anm.SetBool("Attack", false); anm.SetInteger("Move", 0); anm.SetInteger("Idle", -1); }//Dead

    //Set Y position
		if(isInWater)
		{
      body.drag=1; body.angularDrag=1; 
      if(health!=0&&!OnAnm.IsName("Arche|ToHide")&&!OnAnm.IsName("Arche|ToHide-"))
			{
        anm.SetBool("OnGround", false);
			  pitch=Mathf.Lerp(pitch, anm.GetFloat("Pitch")*90f, ang_T);
			  if(anm.GetInteger("Move").Equals(-1)) Move(-dir,25);
        else if(anm.GetInteger("Move").Equals(1)) Move(dir,25);
				else if(anm.GetInteger("Move").Equals(10)) Move(Head.right.normalized,25);
				else if(anm.GetInteger("Move").Equals(-10)) Move(-Head.right.normalized,25);
				else if(!anm.GetInteger("Move").Equals(0)) Move(dir,50);
        else Move(Vector3.zero);
        isOnLevitation=true;
			}
      if(isOnWater) ApplyGravity();
		}
		else if(isOnGround) { body.drag=4; body.angularDrag=4; anm.SetBool("OnGround", true); ApplyYPos(); }
    else
    {
      if(health!=0) { Move(Vector3.zero); pitch=Mathf.Lerp(pitch, anm.GetFloat("Pitch")*90f, ang_T); }
      onJump=true; body.drag=1f; body.angularDrag=1f; ApplyGravity();
    }

		//Stopped
		if(OnAnm.IsName("Arche|Die") | OnAnm.IsName("Arche|DieOnGround"))
		{
      onReset=true; if(!isDead) PlaySound("Die", 2);
		}

		//Forward
		else if(OnAnm.IsName("Arche|Swim") | OnAnm.IsName("Arche|SwimGrowl") | OnAnm.IsName("Arche|SwimGlide"))
		{
			if(OnAnm.IsName("Arche|SwimGrowl")) PlaySound("Growl", 2);
			PlaySound("Swim",  (int) currframe);
		}

		//Backward/Strafe
		else if(OnAnm.IsName("Arche|Swim-"))
		{
			PlaySound("Swim", (int) currframe);
		}

		//Running
		else if(OnAnm.IsName("Arche|SwimFast") | OnAnm.IsName("Arche|SwimFastGrowl") )
		{
			if(OnAnm.IsName("Arche|SwimFastGrowl")) PlaySound("Growl", 2);
			PlaySound("Swim",  (int) currframe);
		}

		//Attack
		else if(OnAnm.IsName("Arche|SwimFastAtk") | OnAnm.IsName("Arche|SwimAtk"))
		{
			if(OnAnm.IsName("Arche|SwimFastAtk"))  { PlaySound("Growl", 2); PlaySound("Bite", 10); onAttack=true; }
			else { PlaySound("Growl", 2); PlaySound("Bite", 3);  PlaySound("Bite", 8); onAttack=true; }
			PlaySound("Swim",  (int) currframe);
		}

		//On Ground
		else if(OnAnm.IsName("Arche|Walk") | OnAnm.IsName("Arche|AtkOnGround") | OnAnm.IsName("Arche|IdleOnGround"))
		{
			if(!OnAnm.IsName("Arche|IdleOnGround"))
			{
			  if(OnAnm.normalizedTime> 0.4f && OnAnm.normalizedTime< 0.9f)  Move(transform.forward, 32); else  Move(Vector3.zero);
				if(OnAnm.IsName("Arche|AtkOnGround"))  { PlaySound("Growl", 2); PlaySound("Bite", 10); onAttack=true; }
			  else PlaySound("Swim", 7); 
			} else  Move(Vector3.zero);
		}

		//Various
		else if(OnAnm.IsName("Arche|EatA")) { onReset=true; isConstrained=true; PlaySound("Food", 2); }
    else if(OnAnm.IsName("Arche|ToHide") | OnAnm.IsName("Arche|ToHide-")) onReset=true;
		else if(OnAnm.IsName("Arche|Die-")) { PlaySound("Growl", 2);  isDead=false; }

    RotateBone(IkType.None, 30f, 20f, false);
	}

  //*************************************************************************************************************************************************
  // Bone rotation
	void LateUpdate()
	{
		if(!isActive) return; headPos=Head.GetChild(0).GetChild(0).position;
    Root.rotation*= Quaternion.Euler(Mathf.Clamp(-pitch, -90f, 90f), roll*3.0f, 0);
		Neck0.rotation*= Quaternion.Euler(spineY, 0, spineX);
		Neck1.rotation*= Quaternion.Euler(spineY, 0, spineX);
		Neck2.rotation*= Quaternion.Euler(spineY, 0, spineX);
		Head.rotation*= Quaternion.Euler(spineY, 0, spineX*2);
    if(!isDead) Head.GetChild(0).transform.rotation*=Quaternion.Euler(-lastHit, 0, 0);
		//Check for ground layer
		GetGroundPos(IkType.None);
	}
}



