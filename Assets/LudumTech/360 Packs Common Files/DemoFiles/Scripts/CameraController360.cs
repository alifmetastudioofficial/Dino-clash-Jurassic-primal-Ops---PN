using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LudumAssets {
	public class CameraController360 : MonoBehaviour{
		public float sensitivity = 0.05f; // camera movement sensitivity
		private Vector3 lastPosition; // mouse position in the previous frame
	
		void Update(){
			if (Input.GetMouseButtonDown(1)){
				lastPosition = Input.mousePosition; // record current mouse position
			}
	
			if (Input.GetMouseButton(1)){
				// Calculate camera rotation based on mouse movement
				float deltaX = Input.mousePosition.x - lastPosition.x;
				float deltaY = Input.mousePosition.y - lastPosition.y;
				Vector3 newRotation = transform.eulerAngles + new Vector3(-deltaY * sensitivity, deltaX * sensitivity, 0);
	
				// Apply rotation to camera
				transform.eulerAngles = newRotation;
	
				// Update mouse position in previous frame
				lastPosition = Input.mousePosition;
			}
		}
	}
}