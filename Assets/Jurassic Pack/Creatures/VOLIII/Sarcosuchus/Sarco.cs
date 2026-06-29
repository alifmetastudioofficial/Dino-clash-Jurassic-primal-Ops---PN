using UnityEngine;

public class Sarco : Creature
{
	public Transform Root,Spine0,Spine1,Spine2,Spine3,Neck0,Neck1,Neck2,Tail0,Tail1,Tail2,Tail3,Tail4,Tail5,Tail6,Tail7,Tail8, 
	Left_Arm0,Right_Arm0,Left_Arm1,Right_Arm1,Left_Hand,Right_Hand,Left_Hips,Right_Hips,Left_Leg,Right_Leg,Left_Foot,Right_Foot;
  public AudioClip Waterflush,Hit_jaw,Hit_head,Hit_tail,Medstep,Medsplash,Sniff2,Bite,Swallow,Largestep,Largesplash,Idlecarn,Sarco1,Sarco2,Sarco3;

	//*************************************************************************************************************************************************
	//Play sound
	void OnCollisionStay(Collision col)
	{
		int rndPainsnd=Random.Range(0, 3); AudioClip painSnd=null;
		switch (rndPainsnd) { case 0: painSnd=Sarco1; break; case 1: painSnd=Sarco2; break; case 2: painSnd=Sarco3; break; }
		ManageCollision(col, source, painSnd, Hit_jaw, Hit_head, Hit_tail);
	}
	void PlaySound(string name, int time)
	{
		if(time==currframe && lastframe!=currframe)
		{
			switch (name)
			{
			case "Step": source[1].pitch=Random.Range(0.75f, 1.25f); 
				if(isInWater) source[1].PlayOneShot(Waterflush, Random.Range(0.25f, 0.5f));
				else if(isOnWater) source[1].PlayOneShot(Medsplash, Random.Range(0.25f, 0.5f));
				else if(isOnGround) source[1].PlayOneShot(Medstep, Random.Range(0.25f, 0.5f));
				if (FootstepParticleManager != null)
					FootstepParticleManager.SpawnFootstep(transform.position, isOnWater, isInWater);
				lastframe=currframe; break;
      case "Swim": source[1].pitch=Random.Range(0.75f, 1.0f);
				if(isOnWater) source[1].PlayOneShot(Waterflush,  0.1f);
				lastframe=currframe; break;
			case "Bite": source[1].pitch=Random.Range(0.75f, 1.0f); source[1].PlayOneShot(Bite, 1.0f);
				lastframe=currframe; break;
			case "Die": source[1].pitch=Random.Range(1.0f, 1.25f); source[1].PlayOneShot(isOnWater|isInWater?Largesplash:Largestep, 1.0f);
				lastframe=currframe; isDead=true; break;
			case "Food": source[0].pitch=Random.Range(1.0f, 1.25f); source[0].PlayOneShot(Swallow, 0.75f);
				lastframe=currframe; break;
			case "Sniff": source[0].pitch=Random.Range(1.0f, 1.25f); source[0].PlayOneShot(Sniff2, 0.5f);
				lastframe=currframe; break;
			case "Atk": int rnd=Random.Range (0, 3); source[0].pitch=Random.Range(1.0f, 1.25f);
        if(rnd==0) source[0].PlayOneShot(Sarco1, 0.75f);
				else if(rnd==1) source[0].PlayOneShot(Sarco2, 0.75f);
				lastframe=currframe; break;
			case "Growl":  source[0].pitch=Random.Range(1.0f, 1.25f);
				source[0].PlayOneShot(Sarco3, 0.75f);
				lastframe=currframe; break;
			}
		}
	}

	//*************************************************************************************************************************************************
	// Add forces to the Rigidbody
	void FixedUpdate ()
	{
		StatusUpdate(); if(!isActive | animSpeed==0.0f) { body.Sleep(); return; }
   	Vector3 dir=Root.right; onAttack=false; isOnLevitation=false; isConstrained=false; onReset=false;

		if(useAI && health!=0) { AICore(1, 2, 3, 0, 4, 0, 0); }  // CPU
		else if(health!=0) { GetUserInputs(1, 2, 3, 0, 4, 0, 0); } // Human
		else { anm.SetBool("Attack", false); anm.SetInteger ("Move", 0); anm.SetInteger ("Idle", -1); } //Dead

   
    //Set Y position
		if(isInWater)
		{
            ApplyYPos();
            // ApplyYPos();//amcode
             body.drag = 4; body.angularDrag = 4;
			//body.drag=1; body.angularDrag=1;
			if (health != 0)
			{
				if (!OnAnm.IsName("Sarco|SwimEat")) pitch = Mathf.Lerp(pitch, anm.GetFloat("Pitch") * 90f, ang_T);
				if (anm.GetInteger("Move").Equals(-1)) Move(-dir, 20);
				else if (anm.GetInteger("Move").Equals(1)) Move(dir, 40);
				else if (anm.GetInteger("Move").Equals(10)) Move(-Head.up.normalized, 20);
				else if (anm.GetInteger("Move").Equals(-10)) Move(Head.up.normalized, 20);
				else if (!anm.GetInteger("Move").Equals(0)) Move(dir, 100);
				else Move(Vector3.zero);
				isOnLevitation = true;
			}
			anm.SetBool("OnGround", false); isOnLevitation = true;
			if (isOnWater) ApplyYPos();
		}
		else if(isOnGround)
    {
      body.drag=4; body.angularDrag=4; ApplyYPos();
      anm.SetBool("OnGround", true); pitch=Mathf.Lerp(pitch, 0.0f, ang_T);
    }
    else { body.drag=4; body.angularDrag=4; ApplyGravity(); }

		//Stopped
		if(OnAnm.IsName("Sarco|Idle1A") | OnAnm.IsName("Sarco|Idle2A") | OnAnm.IsName("Sarco|Die1")
      | OnAnm.IsName("Sarco|Die2") | OnAnm.IsName("Sarco|SwimDie"))
		{
      Move(Vector3.zero);
			if(OnAnm.IsName("Sarco|Die1") | OnAnm.IsName("Sarco|Die2") | OnAnm.IsName("Sarco|SwimDie"))
        { onReset=true; if(!isDead) { PlaySound("Atk", 1); PlaySound("Die", 12); } }
		}

		//Forward
		else if(OnAnm.IsName("Sarco|Walk") | OnAnm.IsName("Sarco|WalkGrowl") |
		        (OnAnm.IsName("Sarco|Step1") && OnAnm.normalizedTime < 0.7) | (OnAnm.IsName("Sarco|Step2") && OnAnm.normalizedTime < 0.7) |
            (OnAnm.IsName("Sarco|StepAtk1") && OnAnm.normalizedTime < 0.7) | (OnAnm.IsName("Sarco|StepAtk2") && OnAnm.normalizedTime < 0.7) |
            (OnAnm.IsName("Sarco|ToIdle1C") && OnAnm.normalizedTime < 0.7))
		{
			Move(transform.forward, 30);
			if(OnAnm.IsName("Sarco|WalkGrowl")) { PlaySound("Growl", 2); PlaySound("Step", 6); PlaySound("Step", 13); }
			else if(OnAnm.IsName("Sarco|Walk")) { PlaySound("Step", 6); PlaySound("Step", 13); }
			else if(OnAnm.IsName("Sarco|StepAtk1") |OnAnm.IsName("Sarco|StepAtk2"))
			{ onAttack=true; PlaySound("Atk", 2); PlaySound("Bite", 4); } else PlaySound("Step", 9);
		}

		//Running
		else if(OnAnm.IsName("Sarco|Run") | OnAnm.IsName("Sarco|RunGrowl") | OnAnm.IsName("Sarco|WalkAtk"))
		{
			Move(transform.forward, 75);
			if(OnAnm.IsName("Sarco|WalkAtk")) { onAttack=true; PlaySound("Atk", 2); PlaySound("Bite", 6); }
			else if(OnAnm.IsName("Sarco|RunGrowl")) { PlaySound("Growl", 2); PlaySound("Step", 6); PlaySound("Step", 13); }
			else if(OnAnm.IsName("Sarco|Run")) { PlaySound("Step", 6); PlaySound("Step", 13); }
			else PlaySound("Step", 8);
		}
		
		//Backward
		else if(OnAnm.IsName("Sarco|Step1-") | OnAnm.IsName("Sarco|Step2-") | OnAnm.IsName("Sarco|ToIdle2C") |
		         OnAnm.IsName("Sarco|ToEatA") | OnAnm.IsName("Sarco|ToEatC"))
		{
			Move(-transform.forward, 20);
			PlaySound("Step", 8);
		}

		//Strafe/Turn right
		else if(OnAnm.IsName("Sarco|Strafe1-") | OnAnm.IsName("Sarco|Strafe2+"))
		{
			Move(transform.right, 8);
			PlaySound("Step", 6); PlaySound("Step", 13);
		}

		//Strafe/Turn left
		else if(OnAnm.IsName("Sarco|Strafe1+") | OnAnm.IsName("Sarco|Strafe2-"))
		{
			Move(-transform.right, 8);
			PlaySound("Step", 6); PlaySound("Step", 13);
		}

		//Various
		else if(OnAnm.IsName("Sarco|EatA")) { onReset=true; isConstrained=true; PlaySound("Food", 4); }
		else if(OnAnm.IsName("Sarco|Idle1B")) PlaySound("Growl", 1);
		else if(OnAnm.IsName("Sarco|Idle1C")) PlaySound("Growl", 1);
		else if(OnAnm.IsName("Sarco|Idle2B")) PlaySound("Growl", 1);
		else if(OnAnm.IsName("Sarco|Idle2C")) { onReset=true; PlaySound("Sniff", 1); }
		else if(OnAnm.IsName("Sarco|Die1-")) { isConstrained=true; PlaySound("Growl", 3);  isDead=false;}
		else if(OnAnm.IsName("Sarco|Die2-")) { isConstrained=true; PlaySound("Growl", 3);  isDead=false; }
    

		//Forward swim
		else if(OnAnm.IsName("Sarco|Swim") | OnAnm.IsName("Sarco|SwimGlide"))
		{
			PlaySound("Swim", (int) currframe);
		}

		//Backward/Strafe swim
		else if(OnAnm.IsName("Sarco|Swim-"))
		{
			PlaySound("Swim", (int) currframe);
		}

		//Running swim
		else if(OnAnm.IsName("Sarco|SwimFast") | OnAnm.IsName("Sarco|SwimGrowl") | OnAnm.IsName("Sarco|SwimFastGrowl") )
		{
      if(OnAnm.IsName("Sarco|SwimFastGrowl")) PlaySound("Atk", 2);
      else if(OnAnm.IsName("Sarco|SwimGrowl"))  PlaySound("Growl", 2);
			PlaySound("Swim",  (int) currframe);
		}

		//Attack swim
		else if(OnAnm.IsName("Sarco|SwimFastAtk") | OnAnm.IsName("Sarco|SwimAtk"))
		{
			if(OnAnm.IsName("Sarco|SwimFastAtk")) { PlaySound("Atk", 2); PlaySound("Bite", 10);	 onAttack=true; }
			else { PlaySound("Atk", 2); PlaySound("Bite", 3);  PlaySound("Bite", 8); onAttack=true; }
			PlaySound("Swim",  (int) currframe);
		}

		//Various
		else if(OnAnm.IsName("Sarco|SwimEat")) { onReset=true; isConstrained=true; PlaySound("Food", 2); }
		else if(OnAnm.IsName("Sarco|Die1-") | OnAnm.IsName("Sarco|Die2-") | OnAnm.IsName("Sarco|SwimDie-")) { PlaySound("Atk", 2);  isDead=false; }
	
    RotateBone(IkType.Convex, 48f, 20f, isOnGround);
  }

  //*************************************************************************************************************************************************
	// Bone rotation
	void LateUpdate()
	{
		if(!isActive) return; headPos=Head.GetChild(0).GetChild(0).position;
    Root.rotation*= Quaternion.Euler( 0, Mathf.Clamp(pitch, -90, 90), 0);
		Neck0.rotation*= Quaternion.Euler(0, -headY, -headX);
		Neck1.rotation*= Quaternion.Euler(0, -headY, -headX);
		Neck2.rotation*= Quaternion.Euler(0, -headY, -headX);
		Head.rotation*= Quaternion.Euler(0, -headY, -headX);
    Spine0.rotation*= Quaternion.Euler(0, -spineY, -spineX);
		Spine1.rotation*= Quaternion.Euler(0, -spineY, -spineX);
		Spine2.rotation*= Quaternion.Euler(0, -spineY, -spineX);
		Spine3.rotation*= Quaternion.Euler(0, -spineY, -spineX);
		Tail0.rotation*= Quaternion.Euler(0, spineY, spineX);
		Tail1.rotation*= Quaternion.Euler(0, spineY, spineX);
		Tail2.rotation*= Quaternion.Euler(0, spineY, spineX);
		Tail3.rotation*= Quaternion.Euler(0, spineY, spineX);
		Tail4.rotation*= Quaternion.Euler(0, spineY, spineX);
		Tail5.rotation*= Quaternion.Euler(0, spineY, spineX);
		Tail6.rotation*= Quaternion.Euler(0, spineY, spineX);
		Tail7.rotation*= Quaternion.Euler(0, spineY, spineX);
		Tail8.rotation*= Quaternion.Euler(0, spineY, spineX);
    if(!isDead) Head.GetChild(0).transform.rotation*=Quaternion.Euler(0, lastHit, 0);
		//Check for ground layer
		GetGroundPos(IkType.Convex, Right_Hips, Right_Leg, Right_Foot, Left_Hips, Left_Leg, Left_Foot, Right_Arm0, Right_Arm1, Right_Hand, Left_Arm0, Left_Arm1, Left_Hand);
	}
}



