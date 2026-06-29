using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LudumAssets {
	public class DemoScript : MonoBehaviour{
		[SerializeField]
		private MeshRenderer renderSky;
		[SerializeField]
		private MeshRenderer renderLandscape;
		//
		[SerializeField]
		private Texture2D[] skies;
		//Tint Landscape based on sky color
		[SerializeField]
		private Color[] tintColors;
		//
		[SerializeField]
		private Texture2D[] landscapes;
		
		//Counters
		private int currentLand = 0;
		private int currentSky = 0;
		//
		private Material landMaterial;
		private Transform landTransform;
		private Material groundMaterial;
		
		void Start(){
			landMaterial = renderLandscape.material;
			groundMaterial = GameObject.Find("/GroundDemo").GetComponent<MeshRenderer>().material;
			//
			landTransform = GameObject.Find("/SkyAndLandscape/Landscape").transform;
			renderLandscape.material.mainTexture = landscapes[0];
			renderSky.material.mainTexture = skies[0];
		}
		
		
		public void TilingSlider(Slider slider){
			//Set Material Tiling Value
			landMaterial.SetFloat("_Xtiling", slider.value);
			//
			float newScaleY = 1/slider.value;
			landTransform.localScale = new Vector3 (1,newScaleY,1);
		}
		
		public void PositionSlider(Slider slider){
			//Set Landscape Position
			float newPos = slider.value * 2;
			landTransform.localPosition = new Vector2 (landTransform.position.x, -newPos);
		}
		
		
		public void NextLand(){
			if(currentLand< landscapes.Length-1){
				currentLand++;
				renderLandscape.material.mainTexture = landscapes[currentLand];
			}
		}
		public void PrevLand(){
			if(currentLand> 0){
				currentLand--;
				renderLandscape.material.mainTexture = landscapes[currentLand];
			}
		}
		//
		public void NextSky(){
			if(currentSky< skies.Length-1){
				currentSky++;
				renderSky.material.mainTexture = skies[currentSky];
				//Tint land and ground color
				landMaterial.color = tintColors[currentSky];
				//groundMaterial.color = tintColors[currentSky];
				groundMaterial.SetColor("_ColorTint", tintColors[currentSky]);
			}
		}
		public void PrevSky(){
			if(currentSky> 0){
				currentSky--;
				renderSky.material.mainTexture = skies[currentSky];
				//Tint land and ground color
				landMaterial.color = tintColors[currentSky];
				groundMaterial.SetColor("_ColorTint", tintColors[currentSky]);
			}
		}
		
	}
}
